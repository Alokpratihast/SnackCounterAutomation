
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Services;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;

namespace SnackCounter.Tests;

public class LowStockRefillRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenStockIsLow_AssignsVolunteer()
    {
        await using var test = await CreateTestContextAsync();

        var result = await test.Service.CreateAsync(
            test.Office.Id, test.Product.Id);

        Assert.Equal(
            RefillRequestCreationStatus.CreatedAndAssigned,
            result.Status);

        Assert.Equal(test.Volunteer!.Id, result.AssignedVolunteerId);
        Assert.NotNull(result.RefillRequestId);
        Assert.Equal(1, test.Chat.NotificationCount);

        var savedRequest = await test.Db.RefillRequests.SingleAsync();

        Assert.Equal(test.Volunteer.Id, savedRequest.AssignedVolunteerId);
    }

    [Fact]
    public async Task CreateAsync_WhenNoVolunteerAvailable_CreatesPendingRequest()
    {
        await using var test = await CreateTestContextAsync(
            includeVolunteer: false);

        var result = await test.Service.CreateAsync(
            test.Office.Id, test.Product.Id);

        Assert.Equal(
            RefillRequestCreationStatus.CreatedPendingAssignment,
            result.Status);

        Assert.Null(result.AssignedVolunteerId);
        Assert.Equal(0, test.Chat.NotificationCount);

        var savedRequest = await test.Db.RefillRequests.SingleAsync();

        Assert.Equal(
            SnackCounter.Domain.Enums.RefillStatus.Pending,
            savedRequest.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveRequestAlreadyExists_ReturnsAlreadyExists()
    {
        await using var test = await CreateTestContextAsync();

        var firstResult = await test.Service.CreateAsync(
            test.Office.Id, test.Product.Id);

        var secondResult = await test.Service.CreateAsync(
            test.Office.Id, test.Product.Id);

        Assert.Equal(
            RefillRequestCreationStatus.CreatedAndAssigned,
            firstResult.Status);

        Assert.Equal(
            RefillRequestCreationStatus.AlreadyExists,
            secondResult.Status);

        Assert.Equal(1, await test.Db.RefillRequests.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenNotificationFails_KeepsRequestSaved()
    {
        await using var test = await CreateTestContextAsync();

        test.Chat.ShouldFail = true;

        var result = await test.Service.CreateAsync(
            test.Office.Id, test.Product.Id);

        Assert.Equal(
            RefillRequestCreationStatus.CreatedAndAssigned,
            result.Status);

        Assert.NotNull(result.Warning);

        Assert.Equal(1, await test.Db.RefillRequests.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_SameProductInDifferentOffices_AllowsSeparateRequests()
    {
        await using var test = await CreateTestContextAsync(
            includeVolunteer: false);

        var secondOffice = new Office("Bengaluru", "INR");

        test.Db.Offices.Add(secondOffice);

        test.Db.Inventories.Add(
            new Inventory(
                secondOffice.Id,
                test.Product.Id,
                currentQuantity: 1,
                minimumQuantity: 5));

        await test.Db.SaveChangesAsync();

        var firstResult = await test.Service.CreateAsync(
            test.Office.Id, test.Product.Id);

        var secondResult = await test.Service.CreateAsync(
            secondOffice.Id, test.Product.Id);

        Assert.Equal(
            RefillRequestCreationStatus.CreatedPendingAssignment,
            firstResult.Status);

        Assert.Equal(
            RefillRequestCreationStatus.CreatedPendingAssignment,
            secondResult.Status);

        Assert.Equal(2, await test.Db.RefillRequests.CountAsync());
    }

    private static async Task<TestContext> CreateTestContextAsync(
        bool includeVolunteer = true)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var office = new Office("Oslo", "NOK");
        var product = new Product("Coffee", "Beverages");

        db.Offices.Add(office);
        db.Products.Add(product);

        db.Inventories.Add(
            new Inventory(
                office.Id,
                product.Id,
                currentQuantity: 2,
                minimumQuantity: 5));

        Employee? volunteer = null;

        if (includeVolunteer)
        {
            volunteer = new Employee(
                "Olivia",
                "olivia@example.com",
                office.Id,
                isVolunteer: true);

            db.Employees.Add(volunteer);
        }

        await db.SaveChangesAsync();

        var factory = new TestDbContextFactory(options);
        var assignmentService = new VolunteerAssignmentService();
        var chat = new FakeChatNotificationService();

        var repository = new RefillRequestRepository(
            db,
            factory,
            assignmentService);

        var service = new LowStockRefillRequestService(
            repository,
            assignmentService,
            chat);

        return new TestContext(
            connection,
            db,
            office,
            product,
            volunteer,
            chat,
            service);
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(options);
        }

        public Task<AppDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class FakeChatNotificationService
        : IChatNotificationService
    {
        public int NotificationCount { get; private set; }

        public bool ShouldFail { get; set; }

        public Task SendAssignmentNotificationAsync(
            string employeeEmail,
            string productName,
            string officeName,
            Guid refillRequestId,
            CancellationToken cancellationToken = default)
        {
            NotificationCount++;

            if (ShouldFail)
            {
                throw new InvalidOperationException(
                    "Simulated chat notification failure.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestContext : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Db { get; }
        public Office Office { get; }
        public Product Product { get; }
        public Employee? Volunteer { get; }
        public FakeChatNotificationService Chat { get; }
        public LowStockRefillRequestService Service { get; }

        public TestContext(
            SqliteConnection connection,
            AppDbContext db,
            Office office,
            Product product,
            Employee? volunteer,
            FakeChatNotificationService chat,
            LowStockRefillRequestService service)
        {
            _connection = connection;
            Db = db;
            Office = office;
            Product = product;
            Volunteer = volunteer;
            Chat = chat;
            Service = service;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task Database_ShouldRejectDuplicateActiveRequests()
    {
        await using var test = await CreateTestContextAsync(
            includeVolunteer: false);

        var firstRequest = new RefillRequest(
            test.Office.Id,
            test.Product.Id);

        var secondRequest = new RefillRequest(
            test.Office.Id,
            test.Product.Id);

        test.Db.RefillRequests.Add(firstRequest);
        await test.Db.SaveChangesAsync();

        test.Db.RefillRequests.Add(secondRequest);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => test.Db.SaveChangesAsync());
    }
}
