using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
/// Получаемый из внешнего API объект
/// </summary>
public class CurrencyApiDto
{
    public MetaData meta { get; set; }
    public Dictionary<string, Currency> data { get; set; }
}

/// <summary>
/// Метадата получаемого из внешнего API объекта <see cref="CurrencyApiDto"/>
/// </summary>
public class MetaData
{
    public DateTime last_updated_at { get; set; }
}