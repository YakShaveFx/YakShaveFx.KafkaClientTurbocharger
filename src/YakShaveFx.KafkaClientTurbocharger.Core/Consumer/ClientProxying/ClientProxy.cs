using Akka.Actor;
using Akka.Event;
using Confluent.Kafka;
using static YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying.ClientProxyMessages;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying;

internal sealed class ClientProxy : ReceiveActor, IWithTimers, ILogReceive
{
    private const string CommitOffsetsTimerKey = "CommitOffsets";

    private static readonly TimeSpan ScheduledCommitInterval = TimeSpan.FromSeconds(5);

    /*
     * TODO: better consider the poll timeout
     * this is mostly relevant for being responsive to commit messages and shutdown
     * I don't think it's very relevant for anything else,
     * as librdkafka internally does its magic to poll messages and keep them in an internal queue
     */
    private static readonly TimeSpan ConsumeTimeout = TimeSpan.FromSeconds(1);

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Func<IConsumer<byte[], byte[]>> _consumerFactory;
    private readonly IEnumerable<string> _topics;
    private readonly ToCommitQueue _offsetsToCommit;
    private IConsumer<byte[], byte[]> _consumer = null!;

    public ClientProxy(
        Func<IConsumer<byte[], byte[]>> consumerFactory,
        IEnumerable<string> topics,
        TimeProvider timeProvider)
    {
        _consumerFactory = consumerFactory;
        _topics = topics;
        _offsetsToCommit = new ToCommitQueue(timeProvider);

        Receive<Commands.Fetch>(Handle);
        Receive<Commands.Commit>(Handle);
        Receive<Commands.ScheduledCommit>(Handle);
    }

    public ITimerScheduler Timers { get; set; } = null!; // set automatically by Akka.NET

    protected override void PreStart()
    {
        _consumer = _consumerFactory();
        _consumer.Subscribe(_topics);
        Timers.StartSingleTimer(
            CommitOffsetsTimerKey,
            Commands.ScheduledCommit.Instance,
            ScheduledCommitInterval);
    }

    protected override void PostStop()
    {
        try
        {
            Commit(ignoreConditionsAndCommit: true);
            _consumer.Unsubscribe();
        }
        catch (Exception)
        {
            // trying to commit on shutdown is a best effort
            _log.Debug("Failed to commit offsets on shutdown");
        }

        _consumer.Dispose();
    }

    private void Handle(Commands.Fetch fetch)
    {
        var record = _consumer.Consume(ConsumeTimeout);
        if (record is not null)
        {
            Context.Parent.Tell(new Events.InboundRecord(record));
        }
        else
        {
            // if we didn't get a record (e.g. because of a timeout),
            // we still have available workers (as we had a fetch request)
            // in which case we want to try to fetch a record again
            Self.Tell(fetch);
        }
    }

    private void Handle(Commands.Commit commit)
    {
        _offsetsToCommit.Enqueue(commit.Offset);
        Commit();
    }

    private void Handle(Commands.ScheduledCommit scheduledCommit) => Commit();

    private void Commit(bool ignoreConditionsAndCommit = false)
    {
        if (_offsetsToCommit.IsEmpty() || !MeetsCommitConditions(ignoreConditionsAndCommit))
        {
            return;
        }

        var toCommit = _offsetsToCommit.PopToCommit();
        _consumer.Commit(toCommit);
    }

    private bool MeetsCommitConditions(bool ignoreConditionsAndCommit)
        => ignoreConditionsAndCommit || OffsetsToCommitSizeThresholdExceeded() || OffsetsToCommitTimeoutExceeded();

    private bool OffsetsToCommitSizeThresholdExceeded() => _offsetsToCommit.IsAboveThreshold();

    private bool OffsetsToCommitTimeoutExceeded() => _offsetsToCommit.IsTimeoutExceeded();
}