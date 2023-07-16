using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Fuse8_ByteMinds.SummerSchool.Benchmarks;

[MemoryDiagnoser(displayGenColumns: true)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class StringInternBenchmark
{
    private readonly List<string> _words = new();
    public StringInternBenchmark()
    {
        foreach (var word in File.ReadLines(@"./SpellingDictionaries/ru_RU.dic"))
            _words.Add(string.Intern(word));
    }

    [Benchmark(Baseline = true, Description = "IsExist with Equals")]
    [ArgumentsSource(nameof(SampleData))]
    public bool WordIsExists(string word)
        => _words.Any(item => word.Equals(item, StringComparison.Ordinal));

    [Benchmark(Description = "IsExist with Intern")]
    [ArgumentsSource(nameof(SampleData))]
    public bool WordIsExistsIntern(string word)
    {
        var internedWord = string.Intern(word);
        return _words.Any(item => ReferenceEquals(internedWord, item));
    }

    public IEnumerable<string> SampleData()
    {
        yield return "sos";
        //yield return new StringBuilder().ToString();
    }
}