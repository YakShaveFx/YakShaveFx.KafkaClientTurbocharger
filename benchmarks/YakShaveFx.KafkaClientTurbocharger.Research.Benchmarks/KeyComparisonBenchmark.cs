using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.ObjectPool;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Benchmarks;

/*
 * As we're using a byte[] as a key from Kafka, the goal of this benchmark is to find a good way to compare byte[] keys,
 * as we need this for the per key parallelism strategy.
 * Approaches tested:
 * - direct byte[] as key, using a custom IEqualityComparer<byte[]>
 * - hash the byte[] and use the hash as key, still as byte[], using the same custom IEqualityComparer<byte[]>
 *      - main difference between this and the first one, is that the hashed byte[] is typically smaller than the original
 * - hash the byte[] and use the hash as key, but as string, using the default string comparer
 * - (added after recommendation on Mastodon) hash the byte[] and use the hash as key, but as UInt128, using XxHash128, then relying on the default UInt128 comparer
 *
 * For each of these approaches, tweaks were made to try to improve performance.
 *
 * Takeaways so far:
 * - For a moment, even though allocating more, it seemed the hash as string was faster
 * - But after some tweaks, the direct byte[] with custom comparer, not only allocates a lot less, but is also much faster
 * - Update - using XxHash128 to get a hash as UInt128, allocates a bit more than the direct byte[] with custom comparer, but is faster
 */

[RankColumn, MinColumn, MaxColumn]
[MemoryDiagnoser]
public class KeyComparisonBenchmark
{
    private readonly DefaultObjectPool<byte[]> _byteArrayPool;
    private readonly IEqualityComparer<byte[]> _comparerWithLinq;
    private readonly IEqualityComparer<byte[]> _comparerWithoutLinqInHashCode;
    private readonly IEqualityComparer<byte[]> _comparerAlternative;
    private readonly IEqualityComparer<byte[]> _comparerAlternativeWithSpan;
    private readonly SHA256 _hashAlgorithm;

    private readonly byte[][] _possibleKeys;

    [Params(100, 1_000)] public int TryAddCount { get; set; }

    [Params(100, 1_000)] public int PossibleKeysCount { get; set; }

    public KeyComparisonBenchmark()
    {
        _possibleKeys = Enumerable.Range(0, 1_000)
            .Select(_ => Encoding.UTF8.GetBytes(SampleKey.Random().ToString()))
            .ToArray();
        _byteArrayPool = new(new ByteArrayPoolPolicy());
        _comparerWithLinq = new ByteArrayEqualityComparerWithLinq();
        _comparerWithoutLinqInHashCode = new ByteArrayEqualityComparerWithoutLinqInHashCode();
        _comparerAlternative = new ByteArrayEqualityComparerAlternative();
        _comparerAlternativeWithSpan = new ByteArrayEqualityComparerAlternativeWithSpan();
        _hashAlgorithm = SHA256.Create();
    }

    [Benchmark(Baseline = true)]
    public void ByteArray()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerWithLinq);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(key, key);
        }
    }

    [Benchmark]
    public void ByteArrayWithoutLinqInHashCode()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerWithoutLinqInHashCode);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(key, key);
        }
    }

    [Benchmark]
    public void ByteArrayAlternative()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerAlternative);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(key, key);
        }
    }

    [Benchmark]
    public void ByteArrayAlternativeWithSpan()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerAlternativeWithSpan);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(key, key);
        }
    }

    [Benchmark]
    public void Hash()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerWithLinq);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKey(key), key);
        }
    }

    [Benchmark]
    public void HashWithoutLinqInHashCode()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerWithoutLinqInHashCode);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKey(key), key);
        }
    }

    [Benchmark]
    public void HashAlternative()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerAlternative);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKey(key), key);
        }
    }

    [Benchmark]
    public void HashAlternativeWithSpan()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<byte[], object>(_comparerAlternativeWithSpan);
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKey(key), key);
        }
    }

    [Benchmark]
    public void HashAsString()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<string, object>();
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKeyAsString(key), key);
        }
    }

    [Benchmark]
    public void HashAsStringWithSpanAndStackAlloc()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<string, object>();
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKeyAsStringWithSpanAndStackAlloc(key), key);
        }
    }

    [Benchmark]
    public void HashAsStringWithSpanAndPool()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<string, object>();
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKeyAsStringWithSpanAndPool(key), key);
        }
    }

    [Benchmark]
    public void HashAsUInt128()
    {
        var possibleKeys = _possibleKeys.AsSpan(0, PossibleKeysCount);
        var map = new Dictionary<UInt128, object>();
        for (var i = 0; i < TryAddCount; ++i)
        {
            var key = possibleKeys[i % possibleKeys.Length];
            _ = map.TryAdd(HashKeyAsUInt128(key), key);
        }
    }

    private sealed class ByteArrayEqualityComparerWithLinq : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
            => x is not null && y is not null
                ? x.SequenceEqual(y)
                : x is null && y is null;

        public int GetHashCode(byte[] obj) 
            => obj.Aggregate(0, static (acc, value) => HashCode.Combine(acc, value));
    }

    private sealed class ByteArrayEqualityComparerWithoutLinqInHashCode : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
            => x is not null && y is not null
                ? x.SequenceEqual(y)
                : x is null && y is null;

        public int GetHashCode(byte[] obj)
        {
            var hash = 0;
            for (var i = 0; i < obj.Length; ++i)
            {
                hash = HashCode.Combine(hash, obj[i]);
            }

            return hash;
        }
    }

    private sealed class ByteArrayEqualityComparerAlternative : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
            => x is not null && y is not null
                ? x.SequenceEqual(y)
                : x is null && y is null;

        public int GetHashCode(byte[] obj)
        {
            var hashCode = new HashCode();
            hashCode.AddBytes(obj);
            return hashCode.ToHashCode();
        }
    }

    private sealed class ByteArrayEqualityComparerAlternativeWithSpan : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
            => x is not null && y is not null
                ? MemoryExtensions.SequenceEqual((ReadOnlySpan<byte>)x, (ReadOnlySpan<byte>)y)
                : x is null && y is null;

        public int GetHashCode(byte[] obj)
        {
            var hashCode = new HashCode();
            hashCode.AddBytes(obj);
            return hashCode.ToHashCode();
        }
    }

    private sealed record SampleKey(Guid SomeGuid, Guid AnotherGuid, Guid YetAnotherGuid)
    {
        public static SampleKey Random() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        public override string ToString() => $"{SomeGuid}.{AnotherGuid}.{YetAnotherGuid}";
    }

    private byte[] HashKey(byte[] key) => _hashAlgorithm.ComputeHash(key);

    private string HashKeyAsString(byte[] key)
        => Convert.ToBase64String(_hashAlgorithm.ComputeHash(key));

    private string HashKeyAsStringWithSpanAndStackAlloc(byte[] key)
    {
        Span<byte> destination = stackalloc byte[SHA256.HashSizeInBytes];
        return _hashAlgorithm.TryComputeHash(key, destination, out _)
            ? Convert.ToBase64String(destination)
            : Throw();

        static string Throw() => throw new InvalidOperationException();
    }

    private string HashKeyAsStringWithSpanAndPool(byte[] key)
    {
        var destination = _byteArrayPool.Get();
        try
        {
            return _hashAlgorithm.TryComputeHash(key, destination, out _)
                ? Convert.ToBase64String(destination)
                : Throw();
        }
        finally
        {
            _byteArrayPool.Return(destination);
        }

        static string Throw() => throw new InvalidOperationException();
    }

    private static UInt128 HashKeyAsUInt128(byte[] key) => XxHash128.HashToUInt128(key);

    private sealed class ByteArrayPoolPolicy : PooledObjectPolicy<byte[]>
    {
        public override byte[] Create() => new byte[SHA256.HashSizeInBytes];

        // not worth clearing the array
        public override bool Return(byte[] obj) => true;
    }
}