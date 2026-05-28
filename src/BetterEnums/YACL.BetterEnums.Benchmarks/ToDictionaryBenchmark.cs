using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace BetterEnums.YACL.Benchmarks;

[MemoryDiagnoser]
public class ToDictionaryBenchmark
{
    [Benchmark(Baseline = true, Description = "Original Enum.GetValues")]
    public Dictionary<int, string> Clasic()
    {
        var dic = new Dictionary<int, string>();

        foreach (var item in Enum.GetValues<MyENUM>())
        {
            dic.Add((int)item, item.ToString());
        }

        return dic;
    }

    [Benchmark(Description = "New <enum>.ToDictionary")]
    public FrozenDictionary<int, string> NewVersion()
    {
        return MyENUM.ToDictionary();
    }
}
