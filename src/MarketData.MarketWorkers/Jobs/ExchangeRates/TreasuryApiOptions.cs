using System.ComponentModel.DataAnnotations;

namespace MarketData.ServiceWorkers.Jobs;

/// <summary>
/// Options for the U.S. Treasury FiscalData "rates of exchange" client. Bound from the
/// <c>TreasuryApi</c> configuration section.
/// </summary>
public sealed class TreasuryApiOptions
{
    public const string SectionName = "TreasuryApi";

    /// <summary>Base address of the FiscalData service.</summary>
    [Required]
    public string BaseUrl { get; set; } = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/";

    /// <summary>Lower bound (inclusive) for the <c>record_date</c> filter (yyyy-MM-dd).</summary>
    public string FromDate { get; set; } = "2015-01-01";

    /// <summary>Per-request timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
