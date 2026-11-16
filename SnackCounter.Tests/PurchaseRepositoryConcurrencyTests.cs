using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Models;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;

namespace SnackCounter.Tests;

public sealed class PurchaseRepositoryConcurrencyTests
{
    [Fact]
    public async Task ConcurrentAttemptsForSameRefill_OnlyOnePurchaseIsCommitted()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"snack-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=30";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connectionString).Options;
        try
        {
            Guid requestId;
            Guid volunteerId;
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                var office = new Office("Oslo", "NOK");
                var product = new Product("Coffee", "Beverages");
                var volunteer = new Employee("Volunteer", "race@example.test", office.Id, true);
                var inventory = new Inventory(office.Id, product.Id, 0, 1);
                var now = DateTime.UtcNow;
                var budget = new Budget(office.Id, now.Month, now.Year, 1000m, "NOK");
                var request = new RefillRequest(office.Id, product.Id);
                request.AssignVolunteer(volunteer.Id);
                request.AcceptAssignment(volunteer.Id);
                setup.AddRange(office, product, volunteer, inventory, budget, request);
                await setup.SaveChangesAsync();
                requestId = request.Id;
                volunteerId = volunteer.Id;
            }

            var factory = new FileDbContextFactory(options);
            var repository = new PurchaseRepository(factory);
            var command = new RecordPurchaseCommand(requestId, volunteerId, 2, 10m, "NOK", "receipt://race");
            var attempts = await Task.WhenAll(
                repository.RecordPurchaseAsync(command, CancellationToken.None),
                repository.RecordPurchaseAsync(command, CancellationToken.None));

            Assert.Single(attempts, x => x.Status == RecordPurchaseStatus.Purchased);
            Assert.Single(attempts, x => x.Status == RecordPurchaseStatus.DuplicatePurchase);

            await using var verify = new AppDbContext(options);
            Assert.Equal(1, await verify.Purchases.CountAsync());
            Assert.Equal(2, await verify.Inventories.Select(x => x.CurrentQuantity).SingleAsync());
            Assert.Equal(20m, await verify.Budgets.Select(x => x.ActualSpent).SingleAsync());
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    private sealed class FileDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }
}
