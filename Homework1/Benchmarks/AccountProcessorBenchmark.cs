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

/*
|                  Method |     Mean |     Error |    StdDev | Rank |   Gen0 | Allocated |
|------------------------ |---------:|----------:|----------:|-----:|-------:|----------:|
| 'Calculate with boxing' | 1.376 us | 0.0239 us | 0.0328 us |    1 | 3.2234 |    6744 B |
|    'Calculate with ref' | 1.663 us | 0.0317 us | 0.0325 us |    2 | 0.0114 |      24 B |
*/