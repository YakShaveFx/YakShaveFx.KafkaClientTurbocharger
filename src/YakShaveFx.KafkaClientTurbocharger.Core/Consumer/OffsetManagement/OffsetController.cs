using Akka.Actor;
using static YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement.OffsetControllerMessages;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement;

internal sealed class OffsetController : ReceiveActor
{
    private readonly OffsetTracker _tracker;

    public OffsetController()
    {
        _tracker = new OffsetTracker();
        Receive<Commands.TrackOffset>(Handle);
        Receive<Commands.CompleteOffset>(Handle);
    }

    private void Handle(Commands.TrackOffset trackOffset) => _tracker.Track(trackOffset.Offset);

    private void Handle(Commands.CompleteOffset completeOffset)
    {
        if (_tracker.Complete(completeOffset.Offset) is { } committableOffset)
        {
            Context.Parent.Tell(new Events.OffsetReadyForCommit(committableOffset));
        }
    }
}