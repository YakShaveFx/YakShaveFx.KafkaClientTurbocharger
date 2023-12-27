using Akka.Actor;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement;
using YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;
using ClientProxyCommands =
    YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying.ClientProxyMessages.Commands;
using ClientProxyEvents =
    YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying.ClientProxyMessages.Events;
using OffsetControllerCommands =
    YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement.OffsetControllerMessages.Commands;
using OffsetControllerEvents =
    YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement.OffsetControllerMessages.Events;
using ParallelismCoordinatorCommands =
    YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running.ParallelismCoordinatorMessages.Commands;
using ParallelismCoordinatorEvents =
    YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running.ParallelismCoordinatorMessages.Events;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Orchestration;

internal sealed class ConsumerOrchestrator : ReceiveActor
{
    private readonly Props _clientProxyProps;
    private readonly Props _offsetControllerProps;
    private readonly Props _parallelismCoordinatorProps;
    private IActorRef _clientProxy = null!;
    private IActorRef _offsetController = null!;
    private IActorRef _parallelismCoordinator = null!;

    public ConsumerOrchestrator(
        Props clientProxyProps,
        Props offsetControllerProps,
        Props parallelismCoordinatorProps)
    {
        _clientProxyProps = clientProxyProps;
        _offsetControllerProps = offsetControllerProps;
        _parallelismCoordinatorProps = parallelismCoordinatorProps;
        
        Receive<ClientProxyEvents.InboundRecord>(Handle);
        Receive<ParallelismCoordinatorEvents.AvailableForRecord>(Handle);
        Receive<ParallelismCoordinatorEvents.RecordHandled>(Handle);
        Receive<OffsetControllerEvents.OffsetReadyForCommit>(Handle);
    }

    protected override void PreStart()
    {
        _clientProxy = Context.ActorOf(_clientProxyProps, nameof(ClientProxy));
        _offsetController = Context.ActorOf(_offsetControllerProps, nameof(OffsetController));
        _parallelismCoordinator = Context.ActorOf(_parallelismCoordinatorProps, nameof(ParallelismCoordinator));
    }

    protected override SupervisorStrategy SupervisorStrategy()
        => new OneForOneStrategy(_ => Directive.Escalate);

    private void Handle(ClientProxyEvents.InboundRecord inbound)
    {
        _offsetController.Tell(new OffsetControllerCommands.TrackOffset(inbound.Record.TopicPartitionOffset));
        _parallelismCoordinator.Tell(new ParallelismCoordinatorCommands.HandleRecord(inbound.Record));
    }

    private void Handle(ParallelismCoordinatorEvents.AvailableForRecord _)
        => _clientProxy.Tell(ClientProxyCommands.Fetch.Instance);

    private void Handle(ParallelismCoordinatorEvents.RecordHandled handled)
        => _offsetController.Tell(new OffsetControllerCommands.CompleteOffset(handled.Record.TopicPartitionOffset));

    private void Handle(OffsetControllerEvents.OffsetReadyForCommit readyForCommit)
        => _clientProxy.Tell(new ClientProxyCommands.Commit(readyForCommit.Offset));
}