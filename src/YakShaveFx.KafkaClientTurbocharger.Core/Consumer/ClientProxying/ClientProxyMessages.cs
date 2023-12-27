using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying;

internal static class ClientProxyMessages
{
    internal static class Commands
    {
        public sealed record Fetch
        {
            private Fetch()
            {
            }

            public static Fetch Instance { get; } = new();
        }
        
        public sealed record Commit(TopicPartitionOffset Offset);

        public sealed record ScheduledCommit
        {
            private ScheduledCommit()
            {
            }

            public static ScheduledCommit Instance { get; } = new();
        }
    }

    internal static class Events
    {
        internal sealed record InboundRecord(ConsumeResult<byte[], byte[]> Record);
    }
}