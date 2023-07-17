using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Fuse8_ByteMinds.SummerSchool.Domain;

namespace Fuse8_ByteMinds.SummerSchool.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class AccountProcessorBenchmark
{
    private readonly List<BankAccount> _bankAccount = new();

    /// <summary>
    ///     Пытаюсь создать достаточную нагрузку, чтобы увидеть преимущество передачи структур по ссылке.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        for (var i = 0; i < 100; i++)
        {
            var random = new Random();
            _bankAccount.Add(new BankAccount
            {
                TotalAmount = random.Next(),
                LastOperation = new BankOperation
                {
                    TotalAmount = random.Next(), OperationInfo0 = random.NextInt64(),
                    OperationInfo1 = random.NextInt64(),
                    OperationInfo2 = random.NextInt64()
                },
                PreviousOperation = new BankOperation
                {
                    TotalAmount = random.NextInt64(), OperationInfo0 = random.NextInt64(),
                    OperationInfo1 = random.NextInt64(),
                    OperationInfo2 = random.Next()
                    
                }
            });
        }
    }

    [Benchmark(Description = "Calculate with boxing")]
    public List<decimal> Calculate()
    {
        AccountProcessor processor = new();
        return _bankAccount.Select(bankAccount => processor.Calculate(bankAccount)).ToList();
    }

    [Benchmark(Description = "Calculate with ref")]
    public List<decimal> CalculatePerformed()
    {
        AccountProcessor processor = new();
        return _bankAccount.Select(bankAccount => processor.CalculatePerformed(bankAccount)).ToList();
    }
}