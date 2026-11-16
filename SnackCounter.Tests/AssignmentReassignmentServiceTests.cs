
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;
using SnackCounter.Application.Services;
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;

namespace SnackCounter.Tests;

public class AssignmentReassignmentServiceTests
{
    [Fact]
    public async Task ProcessExpiredAssignments_ShouldReassignToNextVolunteer()
    {
        await using var test = await CreateTestContextAsync();

        var request = test.CreateAssignedRequest(test.Volunteer1.Id);

        test.Clock.SetUtcNow(
            request.AssignedAtUtc!.Value.AddHours(1).AddSeconds(1));

        var result = await test.Service.ProcessExpiredAssignmentsAsync(
            CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(1, result);
        Assert.Equal(RefillStatus.Assigned, request.Status);
        Assert.Equal(test.Volunteer2.Id, request.AssignedVolunteerId);
        Assert.Single(test.Notifications.Notifications);
    }

    [Fact]
    public async Task ProcessExpiredAssignments_ShouldReturnPending_WhenNoVolunteerAvailable()
    {
        await using var test = await CreateTestContextAsync(
            secondVolunteerAvailable: false);

        var request = test.CreateAssignedRequest(test.Volunteer1.Id);

        test.Volunteer1.UpdateAvailability(false);
        await test.Db.SaveChangesAsync();

        test.Clock.SetUtcNow(
            request.AssignedAtUtc!.Value.AddHours(1).AddSeconds(1));

        var result = await test.Service.ProcessExpiredAssignmentsAsync(
            CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(0, result);
        Assert.Equal(RefillStatus.Pending, request.Status);
        Assert.Null(request.AssignedVolunteerId);
    }

    [Fact]
    public async Task ProcessExpiredAssignments_ShouldNotReassign_WhenNotExpired()
    {
        await using var test = await CreateTestContextAsync();

        var request = test.CreateAssignedRequest(test.Volunteer1.Id);

        test.Clock.SetUtcNow(
            request.AssignedAtUtc!.Value.AddMinutes(30));

        var result = await test.Service.ProcessExpiredAssignmentsAsync(
            CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(0, result);
        Assert.Equal(RefillStatus.Assigned, request.Status);
        Assert.Equal(test.Volunteer1.Id, request.AssignedVolunteerId);
        Assert.Empty(test.Notifications.Notifications);
    }

    [Fact]
    public async Task ProcessExpiredAssignments_ShouldKeepAssignment_WhenNotificationFails()
    {
        await using var test = await CreateTestContextAsync(
            notificationFails: true);

        var request = test.CreateAssignedRequest(test.Volunteer1.Id);

        test.Clock.SetUtcNow(
            request.AssignedAtUtc!.Value.AddHours(1).AddSeconds(1));

        var result = await test.Service.ProcessExpiredAssignmentsAsync(
            CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(1, result);
        Assert.Equal(RefillStatus.Assigned, request.Status);
        Assert.Equal(test.Volunteer2.Id, request.AssignedVolunteerId);
    }

    [Fact]
    public async Task TryReassignExpiredRequest_ShouldRejectSecondAttemptWithStaleVersion()
    {
        await using var test = await CreateTestContextAsync();

        var request = test.CreateAssignedRequest(test.Volunteer1.Id);
        var expectedVersion = request.ConcurrencyVersion;

        var firstResult =
            await test.Repository.TryReassignExpiredRequestAsync(
                request.Id,
                expectedVersion,
                CancellationToken.None);

        var secondResult =
            await test.Repository.TryReassignExpiredRequestAsync(
                request.Id,
                expectedVersion,
                CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(
            AtomicAssignmentStatus.Assigned,
            firstResult.Status);

        Assert.Equal(
            AtomicAssignmentStatus.RequestNoLongerAssigned,
            secondResult.Status);

        Assert.Equal(RefillStatus.Assigned, request.Status);

        Assert.Equal(
            test.Volunteer2.Id,
            request.AssignedVolunteerId);
    }

    private static async Task<TestContext> CreateTestContextAsync(
        bool secondVolunteerAvailable = true,
        bool notificationFails = false)
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

        var volunteer1 = new Employee(
            "Amit", "amit@test.com", office.Id, true);

        var volunteer2 = new Employee(
            "Neha", "neha@test.com", office.Id, true);

        if (!secondVolunteerAvailable)
            volunteer2.UpdateAvailability(false);

        db.Offices.Add(office);
        db.Products.Add(product);
        db.Employees.AddRange(volunteer1, volunteer2);

        await db.SaveChangesAsync();

        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);

        var factory = new TestDbContextFactory(options);
        var assignmentService = new VolunteerAssignmentService();

        var repository = new RefillRequestRepository(
            db,
            factory,
            assignmentService);

        var timeoutService = new AssignmentTimeoutService(clock);

        var notifications = new FakeChatNotificationService(
            notificationFails);

        var service = new AssignmentReassignmentService(
            repository,
            timeoutService,
            notifications);

        return new TestContext(
            connection,
            db,
            office.Id,
            product.Id,
            volunteer1,
            volunteer2,
            clock,
            notifications,
            repository,
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

    private sealed class TestContext(
        SqliteConnection connection,
        AppDbContext db,
        Guid officeId,
        Guid productId,
        Employee volunteer1,
        Employee volunteer2,
        TestTimeProvider clock,
        FakeChatNotificationService notifications,
        RefillRequestRepository repository,
        AssignmentReassignmentService service) : IAsyncDisposable
    {
        public AppDbContext Db { get; } = db;

        public Employee Volunteer1 { get; } = volunteer1;

        public Employee Volunteer2 { get; } = volunteer2;

        public TestTimeProvider Clock { get; } = clock;

        public FakeChatNotificationService Notifications { get; } =
            notifications;

        public RefillRequestRepository Repository { get; } = repository;

        public AssignmentReassignmentService Service { get; } = service;

        public RefillRequest CreateAssignedRequest(Guid volunteerId)
        {
            var request = new RefillRequest(officeId, productId);

            request.AssignVolunteer(volunteerId);

            var office = Db.Offices.First(x => x.Id == officeId);
            office.UpdateVolunteerRotation(volunteerId);

            Db.RefillRequests.Add(request);
            Db.SaveChanges();

            return request;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class TestTimeProvider(DateTimeOffset initialTime)
        : TimeProvider
    {
        private DateTimeOffset _currentTime = initialTime;

        public void SetUtcNow(DateTimeOffset time)
        {
            _currentTime = time;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _currentTime;
        }
    }

    private sealed class FakeChatNotificationService(bool shouldFail)
        : IChatNotificationService
    {
        public List<Guid> Notifications { get; } = [];

        public Task SendAssignmentNotificationAsync(
            string email,
            string productName,
            string officeName,
            Guid requestId,
            CancellationToken cancellationToken)
        {
            if (shouldFail)
                throw new InvalidOperationException(
                    "Simulated notification failure.");

            Notifications.Add(requestId);

            return Task.CompletedTask;
        }
    }
}
