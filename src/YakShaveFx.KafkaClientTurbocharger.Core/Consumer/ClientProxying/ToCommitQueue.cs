using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.ClientProxying;

public sealed class ToCommitQueue(TimeProvider timeProvider)
{
    private static readonly TimeSpan CommitTimeout = TimeSpan.FromSeconds(5); // randomly chosen =p 
    private const int OffsetsToCommitThreshold = 50; // randomly chosen =p 

    private readonly Queue<TopicPartitionOffset> _toCommit = new();

    private long _lastCommitTimestamp;

    public void Enqueue(TopicPartitionOffset offset)
    {
        /*
         * from the Kafka client docs for void Commit(ConsumeResult<TKey, TValue> result):
         *
         * "A consumer at position N has consumed messages with offsets up to N-1 and will next receive the message with offset N.
         * Hence, this method commits an offset of result.Offset + 1."
         */

        _toCommit.Enqueue(new TopicPartitionOffset(
            offset.TopicPartition,
            offset.Offset + 1,
            offset.LeaderEpoch));
    }

    public IEnumerable<TopicPartitionOffset> PopToCommit()
    {
        // get only the most recent per topic/partition
        var result = _toCommit
            .GroupBy(o => (o.Topic, o.Partition.Value))
            .Select(g => g.OrderByDescending(x => x.Offset.Value).First())
            .ToArray();

        _toCommit.Clear();

        _lastCommitTimestamp = timeProvider.GetTimestamp();

        return result;
    }

    public bool IsAboveThreshold() => _toCommit.Count > OffsetsToCommitThreshold;


    public bool IsTimeoutExceeded() => timeProvider.GetElapsedTime(_lastCommitTimestamp) > CommitTimeout;

    public int Count() => _toCommit.Count;

    public bool IsEmpty() => _toCommit.Count == 0;
}