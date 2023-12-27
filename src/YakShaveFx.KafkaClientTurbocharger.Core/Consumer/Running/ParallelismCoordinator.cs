using Akka.Actor;
using static YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running.ParallelismCoordinatorMessages;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;

internal sealed class ParallelismCoordinator : ReceiveActor
{
    private readonly uint _maximumDegreeOfParallelism;
    private readonly uint _maximumTracked;
    private readonly IHandlingTracker _handlingTracker;
    private readonly Queue<IActorRef> _availableRunners;
    private readonly Props _runnerProps;

    private uint _running = 0;
    private uint _totalTracked = 0;
    private uint _availableEventsSentStillWaitingReply = 0;

    public ParallelismCoordinator(ushort maximumDegreeOfParallelism, ParallelismStrategy strategy, Props runnerProps)
    {
        _maximumDegreeOfParallelism = maximumDegreeOfParallelism;
        _maximumTracked = maximumDegreeOfParallelism * (uint)2;
        _handlingTracker = strategy switch
        {
            ParallelismStrategy.PerPartition => new PerPartitionHandlingTracker(),
            ParallelismStrategy.PerKey => new PerKeyHandlingTracker(),
            ParallelismStrategy.LeeroyJenkins => new LeeroyJenkinsHandlingTracker(),
            _ => throw new NotImplementedException()
        };
        _availableRunners = new(maximumDegreeOfParallelism);
        _runnerProps = runnerProps;

        Receive<Commands.HandleRecord>(Handle);
        Receive<RunnerMessages.Events.RecordHandled>(Handle);
    }

    // to get things started
    protected override void PreStart()
    {
        NotifyIfAvailable();
        for (var i = 0; i < _maximumDegreeOfParallelism; ++i)
        {
            _availableRunners.Enqueue(Context.ActorOf(_runnerProps, $"{nameof(Runner)}{i}"));
        }
    }
    
    protected override SupervisorStrategy SupervisorStrategy()
        => new OneForOneStrategy(_ => Directive.Escalate);

    private void Handle(Commands.HandleRecord handle)
    {
        if (_running == _maximumDegreeOfParallelism)
        {
            throw new InvalidOperationException("Cannot handle more records in parallel");
        }

        --_availableEventsSentStillWaitingReply;
        ++_totalTracked;

        var ticket = _handlingTracker.CreateTicket(handle.Record);
        if (_handlingTracker.TryGetTracked(ticket, out var trackingInfo))
        {
            _handlingTracker.EnqueueTrack(ticket);
            trackingInfo.Runner.Tell(new RunnerMessages.Commands.HandleRecord(ticket, handle.Record));
        }
        else
        {
            ++_running;
            var runner = AllocateRunner();
            _handlingTracker.TrackNew(ticket, runner);
            runner.Tell(new RunnerMessages.Commands.HandleRecord(ticket, handle.Record));
        }

        NotifyIfAvailable();
    }

    private void Handle(RunnerMessages.Events.RecordHandled handled)
    {
        Context.Parent.Tell(new Events.RecordHandled(handled.Record));
        --_totalTracked;
        var trackingInformation = _handlingTracker.CompleteTrackingOne(handled.Ticket);
        if (trackingInformation.Enqueued == 0)
        {
            --_running;
            ReturnRunner(trackingInformation.Runner);
        }

        NotifyIfAvailable();
    }

    private void NotifyIfAvailable()
    {
        if (_running + _availableEventsSentStillWaitingReply < _maximumDegreeOfParallelism
            && _totalTracked + _availableEventsSentStillWaitingReply < _maximumTracked)
        {
            ++_availableEventsSentStillWaitingReply;
            Context.Parent.Tell(Events.AvailableForRecord.Instance);
        }
    }

    private IActorRef AllocateRunner() => _availableRunners.Dequeue();

    private void ReturnRunner(IActorRef runner) => _availableRunners.Enqueue(runner);
}