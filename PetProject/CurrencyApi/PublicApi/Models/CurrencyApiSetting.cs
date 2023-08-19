using System.ComponentModel.DataAnnotations;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

/// <summary>
///     Настройки внешнего API
/// </summary>
public class CurrencyApiSetting
{
    /// <summary>
    ///     Идентификатор записи в базе данных
    /// </summary>
    [Key]
    public byte Id { get; set; }

    /// <summary>
    ///     Валюта по умолчанию
    /// </summary>
    [RegularExpression("[A-Z]{3}")]
    public string? DefaultCurrency { get; set; }

    /// <summary>
    ///     Знак после запятой, до которого следует округлять значение курса
    /// </summary>
    [MaxLength(27)]
    public int? CurrencyRoundCount { get; set; }
}

/// <summary>
///     Настройки внешнего API
/// </summary>
public class CurrencyApiSettings
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Устанавливает настройки внешнего API
    /// </summary>
    /// <param name="dbContext">Контекст базы данных, хранящий настройки</param>
    public CurrencyApiSettings(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private async Task<CurrencyApiSetting> LoadSettingsFromDatabaseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = _dbContext.CurrencyApiSettings.FirstOrDefault();
        if (settings == null)
        {
            settings = new CurrencyApiSetting();
            await _dbContext.CurrencyApiSettings.AddAsync(settings, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    /// <summary>
    /// Устанавливает валюту по умолчанию
    /// </summary>
    /// <param name="newCurrency">Валюта по умолчанию</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task SetDefaultCurrencyAsync(string newCurrency, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);
        settings.DefaultCurrency = newCurrency;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Устанавливает знак после запятой для курса валют
    /// </summary>
    /// <param name="newCount">Знак после запятой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public async Task SetCurrencyRoundCountAsync(int newCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);
        settings.CurrencyRoundCount = newCount;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    ///     Валюта по умолчанию
    /// </summary>
    public async Task<string?> GetDefaultCurrencyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);
        if (settings.DefaultCurrency == null)
            throw new ApiSettingsAreNotSet("Валюта по умолчанию не установлена. Используйте контроллер настроек");
        return settings.DefaultCurrency;
    }

    /// <summary>
    ///     Знак после запятой, до которого следует округлять значение курса
    /// </summary>
    public async Task<int> GetCurrencyRoundCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);

        if (settings.CurrencyRoundCount == null)
            throw new ApiSettingsAreNotSet("Знак после запятой не установлен. Используйте контроллер настроек");
        return (int)settings.CurrencyRoundCount;
    }
}

/// <summary>
///     Выбрасывается в случае, если в базе данных не найдены настройки Api
/// </summary>
public class ApiSettingsAreNotSet : Exception
{
    /// <inheritdoc />
    public ApiSettingsAreNotSet(string? message) : base(message)
    {
    }
}