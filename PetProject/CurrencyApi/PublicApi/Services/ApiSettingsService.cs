using CurrencyApi;
using Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Services;

/// <summary>
/// Настройки текущего Api
/// </summary>
public interface IApiSettingsService
{
    /// <summary>
    /// Установка валюты по умолчанию
    /// </summary>
    /// <param name="defaultCurrency">Новая валюта по умолчанию</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task DefaultCurrencySetAsync(string defaultCurrency, CancellationToken cancellationToken);

    /// <summary>
    /// Установка знака после запятой до которого будет округлён курс валют.
    /// </summary>
    /// <param name="roundCount">Новый знак после запятой</param>
    /// <param name="cancellationToken">Токен отмены</param>
    public Task RoundCountSetAsync(int roundCount, CancellationToken cancellationToken);

    /// <summary>
    /// Получение знака после запятой, до которого следует округлять значение курса
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Знак после запятой</returns>
    /// <exception cref="ApiSettingsAreNotSetException">Выбрасывает, если значение не установлено</exception>
    public Task<int> GetCurrencyRoundCountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получение валюты по умолчанию
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Валюта по умолчанию</returns>
    /// <exception cref="ApiSettingsAreNotSetException">Выбрасывает, если значение не установлено</exception>
    public Task<string?> GetDefaultCurrencyAsync(CancellationToken cancellationToken);

}

/// <summary>
/// Настройки текущего Api
/// </summary>
public class ApiSettingsService : IApiSettingsService
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Сервис настройки текущего Api
    /// </summary>
    /// <param name="dbContext">Контекст базы данных, хранящий настройки</param>
    public ApiSettingsService(AppDbContext dbContext)
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

    /// <inheritdoc />
    /// <exception cref="CurrencyCodeValidationException">Выбрасывает, если указанный код валюты не валиден.</exception>
    public async Task DefaultCurrencySetAsync(string defaultCurrency, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!Enum.TryParse(defaultCurrency, true, out CurrencyTypeDTO _))
            throw new CurrencyCodeValidationException("Указанный код валюты неверен или неподдерживается. Доступные коды: " +
                                                      string.Join(',', Enum.GetValues(typeof(CurrencyTypeDTO))));

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);
        settings.DefaultCurrency = defaultCurrency;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RoundCountSetAsync(int roundCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);
        settings.CurrencyRoundCount = roundCount;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetDefaultCurrencyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);
        if (settings.DefaultCurrency == null)
            throw new ApiSettingsAreNotSetException("Валюта по умолчанию не установлена. Используйте контроллер настроек");
        return settings.DefaultCurrency;
    }

    /// <inheritdoc />
    public async Task<int> GetCurrencyRoundCountAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var settings = await LoadSettingsFromDatabaseAsync(cancellationToken);

        if (settings.CurrencyRoundCount == null)
            throw new ApiSettingsAreNotSetException("Знак после запятой не установлен. Используйте контроллер настроек");
        return (int)settings.CurrencyRoundCount;
    }
}

/// <summary>
/// Ошибка, возникающая при валидации типа валюты
/// </summary>
public class CurrencyCodeValidationException : Exception
{
    /// <inheritdoc />
    public CurrencyCodeValidationException(string? message) : base(message)
    {
    }
}

/// <summary>
///     Выбрасывается в случае, если в базе данных не найдены настройки Api
/// </summary>
public class ApiSettingsAreNotSetException : Exception
{
    /// <inheritdoc />
    public ApiSettingsAreNotSetException(string? message) : base(message)
    {
    }
}