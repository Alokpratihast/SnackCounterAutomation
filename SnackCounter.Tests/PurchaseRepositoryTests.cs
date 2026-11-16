using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Models;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;

namespace SnackCounter.Tests;

public sealed class PurchaseRepositoryTests
{
    [Fact]
    public async Task RecordPurchaseAsync_CommitsPurchaseBudgetInventoryAndRequestTogether()
    {
        await using var fixture = await Fixture.CreateAsync(1000m);
        var result = await fixture.Repository.RecordPurchaseAsync(
            new(fixture.RequestId, fixture.VolunteerId, 5, 20m, "NOK", "receipt://test/1"),
            CancellationToken.None);

        Assert.Equal(RecordPurchaseStatus.Purchased, result.Status);
        Assert.Equal(100m, result.TotalAmount);
        Assert.Equal(900m, result.RemainingBudget);
        Assert.Equal(5, await fixture.Db.Inventories.Select(x => x.CurrentQuantity).SingleAsync());
        Assert.Equal(100m, await fixture.Db.Budgets.Select(x => x.ActualSpent).SingleAsync());
        Assert.Equal(0m, await fixture.Db.Budgets.Select(x => x.ReservedAmount).SingleAsync());
        Assert.Equal(1, await fixture.Db.Purchases.CountAsync());
        Assert.Equal("Pending", await fixture.Db.Reimbursements.Select(x => x.Status).SingleAsync());
    }

    [Fact]
    public async Task RecordPurchaseAsync_BlocksPurchaseAboveBudgetWithoutChangingStock()
    {
        await using var fixture = await Fixture.CreateAsync(50m);
        var result = await fixture.Repository.RecordPurchaseAsync(
            new(fixture.RequestId, fixture.VolunteerId, 5, 20m, "NOK", "receipt://test/2"),
            CancellationToken.None);

        Assert.Equal(RecordPurchaseStatus.BudgetExceeded, result.Status);
        Assert.Equal(0, await fixture.Db.Purchases.CountAsync());
        Assert.Equal(0, await fixture.Db.Inventories.Select(x => x.CurrentQuantity).SingleAsync());
        Assert.Equal(0m, await fixture.Db.Budgets.Select(x => x.ActualSpent).SingleAsync());
    }

    [Fact]
    public async Task RecordPurchaseAsync_RejectsWrongVolunteer()
    {
        await using var fixture = await Fixture.CreateAsync(1000m);
        var result = await fixture.Repository.RecordPurchaseAsync(
            new(fixture.RequestId, Guid.NewGuid(), 1, 10m, "NOK", "receipt://test/3"),
            CancellationToken.None);

        Assert.Equal(RecordPurchaseStatus.WrongVolunteer, result.Status);
        Assert.Empty(await fixture.Db.Purchases.ToListAsync());
    }

    [Fact]
    public async Task RecordPurchaseAsync_PreventsSecondPurchaseForSameRequest()
    {
        await using var fixture = await Fixture.CreateAsync(1000m);
        var command = new RecordPurchaseCommand(fixture.RequestId, fixture.VolunteerId,
            2, 10m, "NOK", "receipt://test/4");

        Assert.Equal(RecordPurchaseStatus.Purchased,
            (await fixture.Repository.RecordPurchaseAsync(command, CancellationToken.None)).Status);
        Assert.Equal(RecordPurchaseStatus.RequestNotAccepted,
            (await fixture.Repository.RecordPurchaseAsync(command, CancellationToken.None)).Status);
        Assert.Single(await fixture.Db.Purchases.ToListAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }
        public PurchaseRepository Repository { get; }
        public Guid RequestId { get; private set; }
        public Guid VolunteerId { get; private set; }

        private Fixture(SqliteConnection connection, AppDbContext db, PurchaseRepository repository)
        {
            _connection = connection; Db = db; Repository = repository;
        }

        public static async Task<Fixture> CreateAsync(decimal limit)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var office = new Office("Oslo", "NOK");
            var product = new Product("Coffee", "Beverages");
            var volunteer = new Employee("Volunteer", "v@example.test", office.Id, true);
            var inventory = new Inventory(office.Id, product.Id, 0, 2);
            var now = DateTime.UtcNow;
            var budget = new Budget(office.Id, now.Month, now.Year, limit, "NOK");
            var request = new RefillRequest(office.Id, product.Id);
            request.AssignVolunteer(volunteer.Id);
            request.AcceptAssignment(volunteer.Id);

            db.AddRange(office, product, volunteer, inventory, budget, request);
            await db.SaveChangesAsync();

            var factory = new SharedConnectionFactory(options);
            return new Fixture(connection, db, new PurchaseRepository(factory))
            {
                RequestId = request.Id,
                VolunteerId = volunteer.Id
            };
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class SharedConnectionFactory(DbContextOptions<AppDbContext> options)
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
