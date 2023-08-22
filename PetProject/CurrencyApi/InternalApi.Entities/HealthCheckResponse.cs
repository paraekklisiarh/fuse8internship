namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
///     Ответ хелсчека
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    ///     Статус здоровья API
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    ///     Проверяемые компоненты
    /// </summary>
    public IEnumerable<IndividualHealthCheckResponse> HealthChecks { get; set; } = null!;

    /// <summary>
    ///     Длительность выполнения проверки здоровья API
    /// </summary>
    public TimeSpan HealthCheckDuration { get; set; }
}