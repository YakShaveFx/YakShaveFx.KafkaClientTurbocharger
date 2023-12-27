using System.IO.Hashing;
using Akka.Actor;
using Confluent.Kafka;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Consumer.Running;

internal readonly record struct TrackingInformation(IActorRef Runner, uint Enqueued);

internal class Tracked(IActorRef runner)
{
    public IActorRef Runner { get; } = runner;
    public uint Enqueued { get; private set; } = 1;

    public void Enqueue() => ++Enqueued;
    public void Dequeue() => --Enqueued;
}

internal interface ITrackingTicket;

internal interface IHandlingTracker
{
    ITrackingTicket CreateTicket(ConsumeResult<byte[], byte[]> record);

    bool TryGetTracked(ITrackingTicket ticket, out TrackingInformation trackingInfo);

    TrackingInformation CompleteTrackingOne(ITrackingTicket ticket);

    void TrackNew(ITrackingTicket ticket, IActorRef runner);

    void EnqueueTrack(ITrackingTicket ticket);
}

internal class PerPartitionHandlingTracker : IHandlingTracker
{
    private readonly Dictionary<TrackingTicket, Tracked> _tracked = new();

    public ITrackingTicket CreateTicket(ConsumeResult<byte[], byte[]> record)
        => new TrackingTicket(record.Topic, record.Partition);

    public bool TryGetTracked(ITrackingTicket ticket, out TrackingInformation trackingInfo)
    {
        if (_tracked.TryGetValue((TrackingTicket)ticket, out var tracked))
        {
            trackingInfo = new TrackingInformation(tracked.Runner, tracked.Enqueued);
            return true;
        }

        trackingInfo = default;
        return false;
    }

    public TrackingInformation CompleteTrackingOne(ITrackingTicket ticket)
    {
        var tracked = _tracked[(TrackingTicket)ticket];
        tracked.Dequeue();
        if (tracked.Enqueued == 0)
        {
            _tracked.Remove((TrackingTicket)ticket);
        }

        return new TrackingInformation(tracked.Runner, tracked.Enqueued);
    }

    public void TrackNew(ITrackingTicket ticket, IActorRef runner)
        => _tracked.Add((TrackingTicket)ticket, new Tracked(runner));

    public void EnqueueTrack(ITrackingTicket ticket)
        => _tracked[(TrackingTicket)ticket].Enqueue();

    private sealed record TrackingTicket(string Topic, int Partition) : ITrackingTicket;
}

internal class PerKeyHandlingTracker : IHandlingTracker
{
    private readonly Dictionary<TrackingTicket, Tracked> _tracked = new();

    public ITrackingTicket CreateTicket(ConsumeResult<byte[], byte[]> record)
        => new TrackingTicket(record.Topic, HashKeyAsUInt128(record.Message.Key));

    public bool TryGetTracked(ITrackingTicket ticket, out TrackingInformation trackingInfo)
    {
        if (_tracked.TryGetValue((TrackingTicket)ticket, out var tracked))
        {
            trackingInfo = new TrackingInformation(tracked.Runner, tracked.Enqueued);
            return true;
        }

        trackingInfo = default;
        return false;
    }

    public TrackingInformation CompleteTrackingOne(ITrackingTicket ticket)
    {
        var tracked = _tracked[(TrackingTicket)ticket];
        tracked.Dequeue();
        if (tracked.Enqueued == 0)
        {
            _tracked.Remove((TrackingTicket)ticket);
        }

        return new TrackingInformation(tracked.Runner, tracked.Enqueued);
    }

    public void TrackNew(ITrackingTicket ticket, IActorRef runner)
        => _tracked.Add((TrackingTicket)ticket, new Tracked(runner));

    public void EnqueueTrack(ITrackingTicket ticket)
        => _tracked[(TrackingTicket)ticket].Enqueue();

    private static UInt128 HashKeyAsUInt128(byte[] key) => XxHash128.HashToUInt128(key);

    private sealed record TrackingTicket(string Topic, UInt128 KeyHash) : ITrackingTicket;
}

internal class LeeroyJenkinsHandlingTracker : IHandlingTracker
{
    private readonly Dictionary<TrackingTicket, IActorRef> _tracked = new();

    public ITrackingTicket CreateTicket(ConsumeResult<byte[], byte[]> record)
        => new TrackingTicket();

    public bool TryGetTracked(ITrackingTicket ticket, out TrackingInformation trackingInfo)
    {
        if (_tracked.TryGetValue((TrackingTicket)ticket, out var tracked))
        {
            trackingInfo = new TrackingInformation(tracked, 1);
            return true;
        }

        trackingInfo = default;
        return false;
    }

    public TrackingInformation CompleteTrackingOne(ITrackingTicket ticket)
    {
        if (!_tracked.Remove((TrackingTicket)ticket, out var tracked))
        {
            throw new InvalidOperationException("Cannot complete tracking for a record that is not being tracked");
        }

        return new TrackingInformation(tracked, 0);
    }

    public void TrackNew(ITrackingTicket ticket, IActorRef runner)
        => _tracked.Add((TrackingTicket)ticket, runner);

    public void EnqueueTrack(ITrackingTicket ticket)
        => throw new InvalidOperationException("Cannot enqueue a record for tracking with this strategy");

    private sealed record TrackingTicket : ITrackingTicket
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}