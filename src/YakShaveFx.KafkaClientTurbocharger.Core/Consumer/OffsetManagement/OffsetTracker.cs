using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.OffsetManagement;

internal sealed class OffsetTracker
{
    // this was kinda randomly chosen, just to minimize a bit list resizing
    // it could probably be calculated from some configuration related to parallelism
    // it's not super relevant, as it should rather quickly stabilize on a size
    private const int DefaultPartitionTrackerSize = 10;

    private readonly Dictionary<TrackedPartitionKey, List<TrackedOffset>> _tracker = new();

    private int _totalTrackedCount;

    public int TotalTrackedCount => _totalTrackedCount;

    public void Track(TopicPartitionOffset topicPartitionOffset)
    {
        var key = new TrackedPartitionKey(topicPartitionOffset.Topic, topicPartitionOffset.Partition.Value);
        var partitionTracker = GetOrCreateTrackerForPartition(key, _tracker);
        ThrowIfOutOfOrderOrDuplicatedOffset(topicPartitionOffset, partitionTracker);
        partitionTracker.Add(new(topicPartitionOffset));
        ++_totalTrackedCount;

        static List<TrackedOffset> GetOrCreateTrackerForPartition(
            TrackedPartitionKey key,
            Dictionary<TrackedPartitionKey, List<TrackedOffset>> tracker)
        {
            if (tracker.TryGetValue(key, out var partitionTracker))
            {
                return partitionTracker;
            }

            partitionTracker = new List<TrackedOffset>(DefaultPartitionTrackerSize);
            tracker.Add(key, partitionTracker);
            return partitionTracker;
        }

        static void ThrowIfOutOfOrderOrDuplicatedOffset(TopicPartitionOffset offset, List<TrackedOffset> trackedOffsets)
        {
            if (trackedOffsets.Count > 0 && trackedOffsets[^1].Offset.Offset + 1 != offset.Offset)
            {
                throw new InvalidOperationException("Got out of order or duplicated offset");
            }
        }
    }

    public TopicPartitionOffset? Complete(TopicPartitionOffset topicPartitionOffset)
    {
        var key = new TrackedPartitionKey(topicPartitionOffset.Topic, topicPartitionOffset.Partition.Value);
        var partitionTracker = GetPartitionTracker(key, _tracker);
        CompleteInPartition(topicPartitionOffset, partitionTracker);

        if (GetCommittableOffset(partitionTracker) is not ({ } offset, var index)) return null;

        RemoveCompletedOffsetsFromTracker(partitionTracker, index);
        _totalTrackedCount -= index + 1;
        return offset.Offset;

        static List<TrackedOffset> GetPartitionTracker(
            TrackedPartitionKey key,
            Dictionary<TrackedPartitionKey, List<TrackedOffset>> tracker)
            => tracker.TryGetValue(key, out var partitionTracker)
                ? partitionTracker
                : throw new InvalidOperationException("Offset not tracked");

        static void CompleteInPartition(TopicPartitionOffset offset, List<TrackedOffset> partitionTracker)
        {
            if (!TryFindOffset(partitionTracker, offset, out var trackedOffset))
            {
                throw new InvalidOperationException("Offset not tracked");
            }

            trackedOffset.Complete();
        }

        static CommittableOffset? GetCommittableOffset(List<TrackedOffset> partitionTracker)
        {
            var firstNotCommittableIndex = partitionTracker.FindIndex(static tracked => !tracked.IsCompleted);

            return firstNotCommittableIndex switch
            {
                > 0 => new(partitionTracker[firstNotCommittableIndex - 1], firstNotCommittableIndex - 1),
                -1 when partitionTracker[^1].IsCompleted => new(partitionTracker[^1], partitionTracker.Count - 1),
                _ => null
            };
        }

        static void RemoveCompletedOffsetsFromTracker(List<TrackedOffset> partitionTracker, int committableOffsetIndex)
            => partitionTracker.RemoveRange(0, committableOffsetIndex + 1);

        static bool TryFindOffset(
            List<TrackedOffset> partitionTracker,
            TopicPartitionOffset offset,
            [MaybeNullWhen(false)] out TrackedOffset foundOffset)
            => partitionTracker.TryFind(
                static (tracked, offset) => tracked.Offset == offset,
                offset,
                out foundOffset,
                out _);
    }

    private readonly record struct TrackedPartitionKey(string Topic, int Partition);

    private readonly record struct CommittableOffset(TrackedOffset Offset, int Index);

    private sealed class TrackedOffset(TopicPartitionOffset offset)
    {
        public TopicPartitionOffset Offset { get; } = offset;

        public bool IsCompleted { get; private set; }

        public void Complete() => IsCompleted = true;
    }
}