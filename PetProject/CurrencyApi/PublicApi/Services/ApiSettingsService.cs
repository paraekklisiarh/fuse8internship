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
}

/// <summary>
/// Настройки текущего Api
/// </summary>
public class ApiSettingsService : IApiSettingsService
{
    private readonly CurrencyApiSettings _settings;

    /// <summary>
    /// Сервис настройки текущего Api
    /// </summary>
    /// <param name="settings">База данных</param>
    public ApiSettingsService(CurrencyApiSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    /// <exception cref="CurrencyCodeValidationException">Выбрасывает, если указанный код валюты не валиден.</exception>
    public async Task DefaultCurrencySetAsync(string defaultCurrency, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(defaultCurrency, true, out CurrencyTypeDTO _))
            throw new CurrencyCodeValidationException("Указанный код валюты неверен или неподдерживается. Доступные коды: " +
                                                      string.Join(',', Enum.GetValues(typeof(CurrencyTypeDTO))));

        await _settings.SetDefaultCurrencyAsync(defaultCurrency, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RoundCountSetAsync(int roundCount, CancellationToken cancellationToken) =>
        await _settings.SetCurrencyRoundCountAsync(roundCount, cancellationToken);
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