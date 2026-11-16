namespace SnackCounter.API.Contracts;
public sealed record MonthlyReportRequest(int Year, int Month, string ReportingCurrency, Dictionary<string, decimal> RatesToReportingCurrency);
