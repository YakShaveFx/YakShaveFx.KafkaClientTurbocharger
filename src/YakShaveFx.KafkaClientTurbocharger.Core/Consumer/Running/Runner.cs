using Akka.Actor;
using Confluent.Kafka;
using static YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running.RunnerMessages;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;

internal sealed class Runner : ReceiveActor
{
    private readonly Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> _recordHandler;
    private readonly CancellationToken _cancellationToken;

    public Runner(
        Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> recordHandler,
        CancellationToken cancellationToken)
    {
        _recordHandler = recordHandler;
        _cancellationToken = cancellationToken;
        ReceiveAsync<Commands.HandleRecord>(HandleAsync);
    }

    private async Task HandleAsync(Commands.HandleRecord handle)
    {
        await _recordHandler(handle.Record, _cancellationToken);
        Context.Parent.Tell(new Events.RecordHandled(handle.Ticket, handle.Record));
    }
}