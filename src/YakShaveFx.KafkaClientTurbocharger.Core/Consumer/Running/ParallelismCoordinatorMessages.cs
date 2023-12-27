using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;

internal static class ParallelismCoordinatorMessages
{
    internal static class Commands
    {
        public sealed record HandleRecord(ConsumeResult<byte[], byte[]> Record);
    }
    
    internal static class Events
    {
        public sealed record AvailableForRecord
        {
            private AvailableForRecord()
            {
            }

            public static AvailableForRecord Instance { get; } = new();
        }

        public sealed record RecordHandled(ConsumeResult<byte[], byte[]> Record);
    }
}