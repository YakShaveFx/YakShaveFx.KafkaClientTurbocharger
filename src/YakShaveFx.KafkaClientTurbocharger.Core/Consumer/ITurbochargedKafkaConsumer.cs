namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

/// <summary>
/// Interface representing a turbocharged Kafka consumer.
/// </summary>
public interface ITurbochargedKafkaConsumer
{
    /// <summary>
    /// Runs the consumer. The returned <see cref="Task"/> will complete only when the consumer is stopped.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to signal the consumer it should stop execution.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing consumer execution.</returns>
    Task RunAsync(CancellationToken ct);
}