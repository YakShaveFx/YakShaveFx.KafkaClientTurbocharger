using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;

internal static class RunnerMessages
{
    internal static class Commands
    {
        public sealed record HandleRecord(ITrackingTicket Ticket, ConsumeResult<byte[], byte[]> Record);
    }

    internal static class Events
    {
        public sealed record RecordHandled(ITrackingTicket Ticket, ConsumeResult<byte[], byte[]> Record);
    }
}