using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement;

internal static class OffsetControllerMessages
{
    internal static class Commands
    {
        public sealed record TrackOffset(TopicPartitionOffset Offset);

        public sealed record CompleteOffset(TopicPartitionOffset Offset);
    }

    internal static class Events
    {
        public sealed record OffsetReadyForCommit(TopicPartitionOffset Offset);
    }
}