using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer;

public sealed class TurbochargedKafkaConsumerFactory(TimeProvider timeProvider)
{
    /// <summary>
    /// Allows for the creation of a <see cref="ITurbochargedKafkaConsumer"/>, to consume records from Kafka.
    /// </summary>
    /// <param name="options">
    /// Simple set of options to configure the consumer.
    /// </param>
    /// <param name="handler">
    /// <para>Record handling function, invoked for each record consumed.</para>
    /// </param>
    /// <param name="configure">Allows for further configuring the <see cref="ConsumerConfig"/>, for options that are not exposed by the turbocharged consumer abstraction.</param>
    /// <param name="configureBuilder">Allows for further configuring the <see cref="ConsumerBuilder{TKey,TValue}"/>, for options that are not exposed by the turbocharged consumer abstraction.</param>
    /// <returns>An instance of <see cref="ITurbochargedKafkaConsumer"/> ready to start consuming records from Kafka.</returns>
    /// <remarks>
    /// <para>
    /// If an exception is uncaught by the <paramref name="handler"/>, the consumer will restart to avoid potential record loss.
    /// It is recommended to catch any exceptions and handle them, not letting them bubble to the consumer.
    /// </para>
    /// <para>
    /// Be mindful when using <paramref name="configure"/> and <paramref name="configureBuilder"/>.
    /// They exist to allow for the configuration of things that are not exposed by the turbocharged consumer abstraction,
    /// but misusing them can lead to unexpected behavior.
    /// </para>
    /// </remarks>
    public ITurbochargedKafkaConsumer Create(
        TurbochargedConsumerOptions options,
        Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> handler,
        Action<ConsumerConfig>? configure = null,
        Action<ConsumerBuilder<byte[], byte[]>>? configureBuilder = null)
        => new TurbochargedKafkaConsumer(options.EnsureValidity(), handler, timeProvider, configure, configureBuilder);
}