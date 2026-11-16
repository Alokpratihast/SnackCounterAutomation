using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure.Reporting;

public sealed class MonthlyReportService : IMonthlyReportService
{
    private readonly AppDbContext _db;
    public MonthlyReportService(AppDbContext db) => _db = db;

    public async Task<MonthlySpendReport?> GetAsync(Guid officeId, int year, int month,
        string? reportingCurrency, IReadOnlyDictionary<string, decimal>? rates,
        CancellationToken cancellationToken)
    {
        if (year < 2020 || month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        var office = await _db.Offices.SingleOrDefaultAsync(x => x.Id == officeId, cancellationToken);
        if (office is null) return null;

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var rows = await _db.Purchases.Where(x => x.PurchasedAtUtc >= start && x.PurchasedAtUtc < end)
            .Join(_db.RefillRequests, p => p.RefillRequestId, r => r.Id, (p, r) => new { p, r })
            .Where(x => x.r.OfficeId == officeId)
            .GroupBy(x => x.p.CurrencyCode)
            .Select(g => new CurrencySpend(g.Key, g.Sum(x => x.p.TotalAmount)))
            .ToListAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(reportingCurrency))
            return new(office.Id, office.Name, year, month, rows, null, null,
                "Original currency totals are shown separately; no FX conversion was requested.");

        var target = reportingCurrency.Trim().ToUpperInvariant();
        decimal converted = 0;
        foreach (var row in rows)
        {
            if (row.CurrencyCode == target) converted += row.Amount;
            else if (rates is not null && rates.TryGetValue(row.CurrencyCode, out var rate) && rate > 0)
                converted += row.Amount * rate;
            else
                return new(office.Id, office.Name, year, month, rows, target, null,
                    $"Missing positive FX rate from {row.CurrencyCode} to {target}; converted total omitted.");
        }

        return new(office.Id, office.Name, year, month, rows, target,
            decimal.Round(converted, 2, MidpointRounding.AwayFromZero),
            "Rates are caller-supplied; record the rate source and effective date for accounting use.");
    }
}
