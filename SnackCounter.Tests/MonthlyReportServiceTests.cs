using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Reporting;

namespace SnackCounter.Tests;

public sealed class MonthlyReportServiceTests
{
    [Fact]
    public async Task GetAsync_PreservesOriginalCurrencyTotalsAndConvertsWhenRateProvided()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var office = new Office("Oslo", "NOK");
        var product = new Product("Coffee", "Beverages");
        var volunteer = new Employee("Volunteer", "test@example.test", office.Id, true);
        db.AddRange(office, product, volunteer);
        await db.SaveChangesAsync();

        var requestNok = CreatePurchasedRequest(office.Id, product.Id, volunteer.Id);
        var requestUsd = CreatePurchasedRequest(office.Id, product.Id, volunteer.Id);
        db.RefillRequests.AddRange(requestNok, requestUsd);
        db.Purchases.AddRange(
            new Purchase(requestNok.Id, volunteer.Id, 2, 50m, "NOK", "receipt://nok"),
            new Purchase(requestUsd.Id, volunteer.Id, 1, 10m, "USD", "receipt://usd"));
        await db.SaveChangesAsync();

        var service = new MonthlyReportService(db);
        var now = DateTime.UtcNow;
        var report = await service.GetAsync(office.Id, now.Year, now.Month, "INR",
            new Dictionary<string, decimal> { ["NOK"] = 8m, ["USD"] = 83m },
            CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(2, report.OriginalCurrencyTotals.Count);
        Assert.Contains(report.OriginalCurrencyTotals, x => x.CurrencyCode == "NOK" && x.Amount == 100m);
        Assert.Contains(report.OriginalCurrencyTotals, x => x.CurrencyCode == "USD" && x.Amount == 10m);
        Assert.Equal(1630m, report.ConvertedTotal);
    }

    [Fact]
    public async Task GetAsync_OmitsConvertedTotalWhenAnFxRateIsMissing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var office = new Office("Bengaluru", "INR");
        var product = new Product("Tea", "Beverages");
        var volunteer = new Employee("Volunteer", "test@example.test", office.Id, true);
        var request = CreatePurchasedRequest(office.Id, product.Id, volunteer.Id);
        db.AddRange(office, product, volunteer, request);
        db.Purchases.Add(new Purchase(request.Id, volunteer.Id, 1, 100m, "INR", "receipt://inr"));
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var report = await new MonthlyReportService(db).GetAsync(
            office.Id, now.Year, now.Month, "USD",
            new Dictionary<string, decimal>(), CancellationToken.None);

        Assert.NotNull(report);
        Assert.Null(report.ConvertedTotal);
        Assert.Contains("Missing positive FX rate", report.ConversionNote);
    }

    private static RefillRequest CreatePurchasedRequest(Guid officeId, Guid productId, Guid volunteerId)
    {
        var request = new RefillRequest(officeId, productId);
        request.AssignVolunteer(volunteerId);
        request.AcceptAssignment(volunteerId);
        request.MarkPurchased();
        return request;
    }
}
