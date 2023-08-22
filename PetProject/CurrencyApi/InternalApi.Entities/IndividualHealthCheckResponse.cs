namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
///     Результат хелсчека
/// </summary>
public class IndividualHealthCheckResponse
{
    /// <summary>
    ///     Статус здоровья компонента
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    ///     Название проверяемого компонента
    /// </summary>
    public required string Component { get; set; }

    /// <summary>
    ///     Описание проверяемого компонента
    /// </summary>
    public string? Description { get; set; }
}