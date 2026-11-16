namespace SnackCounter.Application.Models;

public sealed record CurrencySpend(string CurrencyCode, decimal Amount);
public sealed record MonthlySpendReport(
    Guid OfficeId, string OfficeName, int Year, int Month,
    IReadOnlyList<CurrencySpend> OriginalCurrencyTotals,
    string? ReportingCurrency, decimal? ConvertedTotal,
    string? ConversionNote);
