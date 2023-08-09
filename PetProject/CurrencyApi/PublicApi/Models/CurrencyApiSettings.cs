namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Models;

public class CurrencyApiSettings
{
    public required string BaseUrl { get; init; }
    public required string DefaultCurrency { get; init; }
    public string? BaseCurrency { get; init; }
    public int CurrencyRoundCount { get; init; }
    
    public required string ApiKey { get; init; }
    
}