using System.Text;
using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Labs.Consumer.Pipelines;

// TODO: better names!

public interface IPipelineBuilderStart
{
    IPipelineBuilderMiddle<TOut> WithInitialStep<TOut>(
        Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task<TOut>> initialStep);
}

public interface IPipelineBuilderMiddle<TIn>
{
    IPipelineBuilderMiddle<TOut> WithStep<TOut>(
        Func<TIn, CancellationToken, Task<TOut>> middleStep);

    Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> WithFinalStep(
        Func<TIn, CancellationToken, Task> finalStep);
}

public static class PipelineBuilder
{
    public static IPipelineBuilderStart Create() => new PipelineBuilderStart();

    private sealed class PipelineBuilderStart : IPipelineBuilderStart
    {
        public IPipelineBuilderMiddle<TOut> WithInitialStep<TOut>(
            Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task<TOut>> initialStep)
            => new PipelineBuilderMiddle<TOut>(initialStep);
    }

    private sealed class PipelineBuilderMiddle<TIn>(
        Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task<TIn>> previousStep)
        : IPipelineBuilderMiddle<TIn>
    {
        public IPipelineBuilderMiddle<TOut> WithStep<TOut>(Func<TIn, CancellationToken, Task<TOut>> middleStep)
            => new PipelineBuilderMiddle<TOut>(async (record, token) =>
            {
                var previousResult = await previousStep(record, token);
                return await middleStep(previousResult, token);
            });

        public Func<ConsumeResult<byte[], byte[]>, CancellationToken, Task> WithFinalStep(
            Func<TIn, CancellationToken, Task> finalStep)
            => async (record, token) =>
            {
                var previousResult = await previousStep(record, token);
                await finalStep(previousResult, token);
            };
    }
}