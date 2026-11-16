using Microsoft.EntityFrameworkCore;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Application.Services;

public sealed class BudgetConfigurationService
{
    private readonly AppDbContext _db;
    public BudgetConfigurationService(AppDbContext db) => _db = db;

    public async Task<Budget> ConfigureAsync(Guid officeId, int year, int month,
        decimal limit, CancellationToken cancellationToken)
    {
        var office = await _db.Offices.SingleOrDefaultAsync(x => x.Id == officeId, cancellationToken)
            ?? throw new KeyNotFoundException("Office not found.");
        var budget = await _db.Budgets.SingleOrDefaultAsync(
            x => x.OfficeId == officeId && x.Year == year && x.Month == month, cancellationToken);
        if (budget is null)
        {
            budget = new Budget(officeId, month, year, limit, office.CurrencyCode);
            _db.Budgets.Add(budget);
        }
        else
        {
            budget.UpdateMonthlyLimit(limit);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return budget;
    }
}
