using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace YakShaveFx.KafkaClientTurbocharger.Core.Benchmarks;

[RankColumn, MinColumn, MaxColumn]
[MemoryDiagnoser]
public class ListFindPredicateBenchmark
{
    private List<int> _list = null!;
    [Params(10, 100, 1_000, 10_000)] public int ListSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _list = Enumerable.Range(0, ListSize).ToList();
    }

    [Benchmark(Baseline = true)]
    public int FindWithLambda()
    {
        var value = ListSize - 1;
        return _list.Find(x => x == value);
    }

    [Benchmark]
    public int FindWithWrapperStructWithMethod()
    {
        var value = ListSize - 1;
        return _list.Find(new StructPredicate(value).IsMatch);
    }

    [Benchmark]
    public int FindWithWrapperClassWithMethod()
    {
        var value = ListSize - 1;
        return _list.Find(new ClassPredicate(value).IsMatch);
    }

    [Benchmark]
    public int FindWithForeach()
    {
        var value = ListSize - 1;
        foreach (var item in _list)
        {
            if (item == value)
            {
                return item;
            }
        }

        throw new Exception("Not found");
    }

    [Benchmark]
    public int FindWithFor()
    {
        var value = ListSize - 1;
        for (var i = 0; i < _list.Count; i++)
        {
            var item = _list[i];
            if (item == value)
            {
                return item;
            }
        }

        throw new Exception("Not found");
    }
    
    [Benchmark]
    public int FindWithForeachOnSpan()
    {
        var value = ListSize - 1;
        var span = CollectionsMarshal.AsSpan(_list);
        foreach (var item in span)
        {
            if (item == value)
            {
                return item;
            }
        }

        throw new Exception("Not found");
    }
    
    [Benchmark]
    public int FindWithForOnSpan()
    {
        var value = ListSize - 1;
        var span = CollectionsMarshal.AsSpan(_list);
        for (var i = 0; i < span.Length; i++)
        {
            var item = span[i];
            if (item == value)
            {
                return item;
            }
        }

        throw new Exception("Not found");
    }

    private readonly struct StructPredicate(int value)
    {
        public bool IsMatch(int x) => x == value;
    }

    private sealed class ClassPredicate(int value)
    {
        public bool IsMatch(int x) => x == value;
    }
}