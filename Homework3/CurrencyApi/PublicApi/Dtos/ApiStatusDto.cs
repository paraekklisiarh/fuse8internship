using System.Text.Json.Serialization;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Dtos;

/// <summary>
///     Статус внешнего API
/// </summary>
public class ApiStatusDto
{
    /// <summary>
    /// ID аккаунта
    /// </summary>
    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }

    /// <summary>
    /// Лимиты в текущем месяце
    /// </summary>
    [JsonPropertyName("quotas")]
    public ApiQuotasDto Quotas { get; set; }
}

public class ApiQuotasDto
{
    [JsonPropertyName("month")]
    public ApiLimitsDto Month { get; set; }
}

/// <summary>
/// Лимиты внешнего API
/// </summary>
public class ApiLimitsDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("used")]
    public int Used { get; set; }
    
    [JsonPropertyName("remaining")]
    public int Remaining { get; set; }
}