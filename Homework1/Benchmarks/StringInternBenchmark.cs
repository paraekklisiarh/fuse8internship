using System.Text;
using BenchmarkDotNet.Attributes;

namespace Fuse8_ByteMinds.SummerSchool.Benchmarks;

/// <summary>
///     Применение интернирования даёт статистически значимый прирост производительности в случае неинтернированных строк
///     (SDR ~0.10),
///     который однако недостаточен, чтобы превысить недостаток подхода - забивание памяти множеством неочищаемых строк.
///     Интернированее менее выгодно, если используются короткие словари, с ростом словаря преимущество увеличивается.
///     With Intern одинаково быстрее with Equals, если строка отсутствует в словаре вне зависимости от её
///     интернированности.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class StringInternBenchmark
{
    private readonly List<string> _words = new();

    public StringInternBenchmark()
    {
        foreach (string word in File.ReadLines(@"./SpellingDictionaries/ru_RU.dic"))
            _words.Add(string.Intern(word));
    }

    [Benchmark(Baseline = true, Description = "IsExist with Equals")]
    [ArgumentsSource(nameof(SampleData))]
    public bool WordIsExists(string word)
    {

    [Benchmark(Description = "IsExist with Intern")]
    [ArgumentsSource(nameof(SampleData))]
    public bool WordIsExistsIntern(string word)
    {
        string internedWord = string.Intern(word);
        return _words.Any(item => ReferenceEquals(internedWord, item));
    }

    /// <summary>
    ///     Выбрать слова из начала, середины и конца файла ru_RU.dic, а так же слова, которых нет в словаре
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> SampleData()
    {
        //строки, собранные с помощью StringBuilder-а
        yield return new StringBuilder().Append("Чжэн").Append("чжоу").ToString();
        yield return new StringBuilder().Append("полим").Append("етрия/H").ToString();
        yield return new StringBuilder().Append("ёкаю").Append("щий/A").ToString();

        // Константные строки
        yield return _words[6]; // Слово из первой трети файла
        yield return _words[_words.Count / 2 + 1]; // Слово из средней трети
        yield return _words[^1]; // Слово из последней трети файла

        yield return new StringBuilder().Append("абырвал").Append("Г").ToString(); // Строка, которой нет в файле
        yield return "уъуъуъ"; // Константная строка, которой нет в файле
    }
}