﻿namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

public class SettingsDto
{
    /// текущий курс валют по умолчанию из конфигурации
    public string defaultCurrency { get; set; }

    /// базовая валюта, относительно которой считается курс
    public string baseCurrency { get; set; }

    /// общее количество доступных запросов, полученное от внешнего API
    public string requestLimit { get; set; }

    ///  количество использованных запросов, полученное от внешнего API
    public string requestCount { get; set; }

    /// Количество знаков после запятой, до которого следует округлять значение курса валют
    public string currencyRoundCount { get; set; }
}