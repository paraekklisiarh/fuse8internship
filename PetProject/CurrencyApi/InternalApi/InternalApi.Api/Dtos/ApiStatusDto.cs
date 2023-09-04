using System.Text.Json.Serialization;

namespace InternalApi.Dtos;

/// <summary>
///     Статус внешнего API
/// </summary>
public class ApiStatusDto
{
    /// <summary>
    ///     ID аккаунта
    /// </summary>
    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }

    /// <summary>
    ///     Лимиты в текущем месяце
    /// </summary>
    [JsonPropertyName("quotas")]
    public required ApiQuotasDto Quotas { get; set; }
}

/// <summary>
///     Квоты API
/// </summary>
public class ApiQuotasDto
{
    /// <summary>
    ///     Квоты в данном месяце
    /// </summary>
    [JsonPropertyName("month")]
    public required ApiLimitsDto Month { get; set; }
}

/// <summary>
///     Лимиты внешнего API
/// </summary>
public class ApiLimitsDto
{
    /// <summary>
    ///     Доступно токенов всего
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    ///     Использовано токенов
    /// </summary>
    [JsonPropertyName("used")]
    public int Used { get; set; }

    /// <summary>
    ///     Осталось токенов
    /// </summary>
    [JsonPropertyName("remaining")]
    public int Remaining { get; set; }
}