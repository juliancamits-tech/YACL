using BenchmarkDotNet.Attributes;

namespace BetterEnums.YACL.Benchmarks;

[MemoryDiagnoser]
public class ToStringBenchmark
{
    [Benchmark(Baseline = true, Description = "Traditional ToString() to a variable that is a enum")]
    [Arguments(MyENUM.Opt1)]
    [Arguments(MyENUM.Opt2)]
    [Arguments(MyENUM.Opt3)]
    [Arguments(MyENUM.Opt4)]
    [Arguments(MyENUM.Opt5)]
    [Arguments(MyENUM.Opt6)]
    public string EnumToString(MyENUM value)
    {
        return value.ToString();
    }

    [Benchmark(Description = "New ToStringV2() version to a variable that is a enum")]
    [Arguments(MyENUM.Opt1)]
    [Arguments(MyENUM.Opt2)]
    [Arguments(MyENUM.Opt3)]
    [Arguments(MyENUM.Opt4)]
    [Arguments(MyENUM.Opt5)]
    [Arguments(MyENUM.Opt6)]
    public string EnumToStringV2(MyENUM value)
    {
        return value.ToStringV2();
    }
}


