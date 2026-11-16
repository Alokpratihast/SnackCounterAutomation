using Microsoft.EntityFrameworkCore;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure.Services;

public sealed class DemoSeedService
{
    private readonly AppDbContext _db;
    public DemoSeedService(AppDbContext db) => _db = db;

    public async Task<object> SeedAsync(CancellationToken cancellationToken)
    {
        var oslo = await _db.Offices.SingleOrDefaultAsync(x => x.Name == "Oslo", cancellationToken);
        var bengaluru = await _db.Offices.SingleOrDefaultAsync(x => x.Name == "Bengaluru", cancellationToken);
        if (oslo is null) { oslo = new Office("Oslo", "NOK"); _db.Offices.Add(oslo); }
        if (bengaluru is null) { bengaluru = new Office("Bengaluru", "INR"); _db.Offices.Add(bengaluru); }
        await _db.SaveChangesAsync(cancellationToken);

        await SeedOfficeAsync(oslo, 15, 6000m, cancellationToken);
        await SeedOfficeAsync(bengaluru, 20, 15000m, cancellationToken);
        return new { Offices = new[] { new { oslo.Id, oslo.Name, oslo.CurrencyCode }, new { bengaluru.Id, bengaluru.Name, bengaluru.CurrencyCode } },
            OsloVolunteerCount = 15, BengaluruVolunteerCount = 20,
            OsloMonthlyBudget = 6000m, BengaluruMonthlyBudget = 15000m };
    }

    private async Task SeedOfficeAsync(Office office, int volunteerCount, decimal monthlyLimit, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (!await _db.Budgets.AnyAsync(x => x.OfficeId == office.Id && x.Year == now.Year && x.Month == now.Month, cancellationToken))
            _db.Budgets.Add(new Budget(office.Id, now.Month, now.Year, monthlyLimit, office.CurrencyCode));

        for (var i = 1; i <= volunteerCount; i++)
        {
            var email = $"{office.Name.ToLowerInvariant()}.volunteer{i:00}@example.test";
            if (!await _db.Employees.AnyAsync(x => x.Email == email, cancellationToken))
                _db.Employees.Add(new Employee($"{office.Name} Volunteer {i:00}", email, office.Id, true));
        }

        var productSpecs = new[] { ("Coffee", "Beverages"), ("Tea", "Beverages"), ("Biscuits", "Snacks") };
        foreach (var (name, category) in productSpecs)
        {
            var product = await _db.Products.SingleOrDefaultAsync(x => x.Name == $"{office.Name} {name}", cancellationToken);
            if (product is null)
            {
                product = new Product($"{office.Name} {name}", category);
                _db.Products.Add(product);
                await _db.SaveChangesAsync(cancellationToken);
            }
            if (!await _db.Inventories.AnyAsync(x => x.OfficeId == office.Id && x.ProductId == product.Id, cancellationToken))
                _db.Inventories.Add(new Inventory(office.Id, product.Id, name == "Coffee" ? 1 : 12, 2));
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
