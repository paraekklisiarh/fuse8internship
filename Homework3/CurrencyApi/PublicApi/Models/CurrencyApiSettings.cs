namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

public class CurrencyApiSettings
{
    public string baseUrl { get; init; }
    public string defaultCurrency { get; init; }
    public string baseCurrency { get; init; }
    public int currencyRoundCount { get; init; }
    
    public string ApiKey { get; init; }
    
}