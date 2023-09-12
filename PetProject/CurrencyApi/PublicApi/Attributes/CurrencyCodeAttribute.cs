using System.ComponentModel.DataAnnotations;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Attributes;

/// <summary>
///     Атрибут-регулярное выражение, предварительно проверяющий валидность кода валюты
/// </summary>
public class CurrencyCodeAttribute : RegularExpressionAttribute
{
    /// <summary>
    ///     Инициализирует атрибут, проверяющий, что строка состоиз из 3 прописных букв английского алфавита A-Z
    /// </summary>
    public CurrencyCodeAttribute() : base("[A-Z]{3}")
    {
    }
}