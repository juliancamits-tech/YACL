using BenchmarkDotNet.Running;

namespace BetterEnums.YACL.Benchmarks;

internal class Program
{
    static void Main()
    {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, config: new GlobalBenchmarkConfig());
    }
}
