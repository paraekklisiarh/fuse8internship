using System.ComponentModel.DataAnnotations;

namespace InternalApi.Entities;

/// <summary>
///     Курс валюты
/// </summary>
public record Currency
{
    public Currency()
    {
    }

    public Currency(CurrencyType code, decimal value, DateTime rateDate)
    {
        Code = code;
        Value = value;
        RateDate = rateDate;
    }

    /// <summary>
    ///     Id курса валюты
    /// </summary>
    [Required]
    public long Id { get; set; }

    /// <summary>
    ///     Код валюты
    /// </summary>
    public required CurrencyType Code { get; set; }

    /// <summary>
    ///     Значение курса валюты, относительно базовой валюты
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Value must be greater than zero.")]
    public required decimal Value { get; set; }
    
    /// <summary>
    ///     Дата курса валюты
    /// </summary>
    public required DateTimeOffset RateDate { get; set; }
}

public enum CurrencyType
{
    USD	= 0,
    RUB = 1,
    
    AED	,
    AFN	,
    ALL	,
    AMD	,
    ANG	,
    AOA	,
    ARS	,
    AUD	,
    AWG	,
    AZN	,
    BAM	,
    BBD	,
    BDT	,
    BGN	,
    BHD	,
    BIF	,
    BMD	,
    BND	,
    BOB	,
    BRL	,
    BSD	,
    BTN	,
    BWP	,
    BYN	,
    BYR	,
    BZD	,
    CAD	,
    CDF	,
    CHF	,
    CLF	,
    CLP	,
    CNY	,
    COP	,
    CRC	,
    CUC	,
    CUP	,
    CVE	,
    CZK	,
    DJF	,
    DKK	,
    DOP	,
    DZD	,
    EGP	,
    ERN	,
    ETB	,
    EUR	,
    FJD	,
    FKP	,
    GBP	,
    GEL	,
    GGP	,
    GHS	,
    GIP	,
    GMD	,
    GNF	,
    GTQ	,
    GYD	,
    HKD	,
    HNL	,
    HRK	,
    HTG	,
    HUF	,
    IDR	,
    ILS	,
    IMP	,
    INR	,
    IQD	,
    IRR	,
    ISK	,
    JEP	,
    JMD	,
    JOD	,
    JPY	,
    KES	,
    KGS	,
    KHR	,
    KMF	,
    KPW	,
    KRW	,
    KWD	,
    KYD	,
    KZT	,
    LAK	,
    LBP	,
    LKR	,
    LRD	,
    LSL	,
    LTL	,
    LVL	,
    LYD	,
    MAD	,
    MDL	,
    MGA	,
    MKD	,
    MMK	,
    MNT	,
    MOP	,
    MRO	,
    MUR	,
    MVR	,
    MWK	,
    MXN	,
    MYR	,
    MZN	,
    NAD	,
    NGN	,
    NIO	,
    NOK	,
    NPR	,
    NZD	,
    OMR	,
    PAB	,
    PEN	,
    PGK	,
    PHP	,
    PKR	,
    PLN	,
    PYG	,
    QAR	,
    RON	,
    RSD	,
    RWF	,
    SAR	,
    SBD	,
    SCR	,
    SDG	,
    SEK	,
    SGD	,
    SHP	,
    SLL	,
    SOS	,
    SRD	,
    STD	,
    SVC	,
    SYP	,
    SZL	,
    THB	,
    TJS	,
    TMT	,
    TND	,
    TOP	,
    TRY	,
    TTD	,
    TWD	,
    TZS	,
    UAH	,
    UGX	,
    
    UYU	,
    UZS	,
    VEF	,
    VND	,
    VUV	,
    WST	,
    XAF	,
    XAG	,
    XAU	,
    XCD	,
    XDR	,
    XOF	,
    XPF	,
    YER	,
    ZAR	,
    ZMK	,
    ZMW	,
    ZWL	,
    XPT	,
    XPD	,
    BTC	,
    ETH	,
    BNB	,
    XRP	,
    SOL	,
    DOT	,
    AVAX	,
    MATIC	,
    LTC	,
    ADA	,

}