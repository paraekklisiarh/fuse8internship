using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Fuse8_ByteMinds.SummerSchool.Domain;

namespace Fuse8_ByteMinds.SummerSchool.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class AccountProcessorBenchmark
{
    private readonly BankAccount _bankAccount = new();

    [Benchmark(Description = "Calculate with boxing")]
    public decimal Calculate()
    {
        AccountProcessor processor = new();
        return processor.Calculate(_bankAccount);
    }

    [Benchmark(Description = "Calculate with ref")]
    public decimal CalculatePerformed()
    {
        AccountProcessor processor = new();
        return processor.CalculatePerformed(in _bankAccount);
    }
}