namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
///     Валюта
/// </summary>
public class Currency
{
    /// <summary>
    ///     Код валюты
    /// </summary>
    public string code { get; set; }

    /// <summary>
    ///     Курс валюты
    /// </summary>
    public decimal value { get; set; }
}