using BenchmarkDotNet.Attributes;
using System;

namespace BetterEnums.YACL.Benchmarks;

[MemoryDiagnoser]
public class ToEnumBenchmark
{
    [Benchmark(Baseline = true, Description = "Original Enum.TryParse")]
    [Arguments("Opt1")]
    [Arguments("opt2")]
    [Arguments("OPT3")]
    [Arguments("Opt4")]
    [Arguments("opt5")]
    [Arguments("OPT6")]
    [Arguments("invalidValue")]
    public MyOther Clasic(string value)
    {
        if (Enum.TryParse<MyOther>(value, out MyOther r))
            return r;

        throw new Exception();
    }

    [Benchmark(Description = "New <enum>.FromString(string)")]
    [Arguments("Opt1")]
    [Arguments("opt2")]
    [Arguments("OPT3")]
    [Arguments("Opt4")]
    [Arguments("opt5")]
    [Arguments("OPT6")]
    [Arguments("invalidValue")]
    public MyOther NewMethod(string value)
    {
        return MyOther.FromString(value);
    }

    [Benchmark(Description = "New \"someText\".To<enum>()")]
    [Arguments("Opt1")]
    [Arguments("opt2")]
    [Arguments("OPT3")]
    [Arguments("Opt4")]
    [Arguments("opt5")]
    [Arguments("OPT6")]
    [Arguments("invalidValue")]
    public MyOther NewMethod2(string value)
    {
        return value.ToMyOther();
    }

}


