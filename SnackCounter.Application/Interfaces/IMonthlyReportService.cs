using SnackCounter.Application.Models;
namespace SnackCounter.Application.Interfaces;

public interface IMonthlyReportService
{
    Task<MonthlySpendReport?> GetAsync(Guid officeId, int year, int month,
        string? reportingCurrency, IReadOnlyDictionary<string, decimal>? rates,
        CancellationToken cancellationToken);
}
