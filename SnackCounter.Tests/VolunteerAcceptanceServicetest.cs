
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Services;
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;

namespace SnackCounter.Tests;

public class VolunteerAcceptanceServiceTests
{
    [Fact]
    public async Task AcceptAsync_ShouldAcceptAssignedVolunteer()
    {
        await using var test = await CreateTestContextAsync();

        var volunteerId = test.VolunteerId;

        var request = new RefillRequest(
            test.OfficeId,
            test.ProductId);

        request.AssignVolunteer(volunteerId);

        test.Db.RefillRequests.Add(request);
        await test.Db.SaveChangesAsync();

        var result = await test.Service.AcceptAsync(
            request.Id,
            volunteerId,
            CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(
            VolunteerAcceptanceStatus.Accepted,
            result.Status);

        Assert.Equal(RefillStatus.Accepted, request.Status);
        Assert.NotNull(request.AcceptedAtUtc);
    }

    [Fact]
    public async Task AcceptAsync_ShouldRejectWrongVolunteer()
    {
        await using var test = await CreateTestContextAsync();

        var request = new RefillRequest(
            test.OfficeId,
            test.ProductId);

        request.AssignVolunteer(test.VolunteerId);

        test.Db.RefillRequests.Add(request);
        await test.Db.SaveChangesAsync();

        var wrongVolunteerId = Guid.NewGuid();

        var result = await test.Service.AcceptAsync(
            request.Id,
            wrongVolunteerId,
            CancellationToken.None);

        await test.Db.Entry(request).ReloadAsync();

        Assert.Equal(
            VolunteerAcceptanceStatus.WrongVolunteer,
            result.Status);

        Assert.Equal(RefillStatus.Assigned, request.Status);
    }

    [Fact]
    public async Task AcceptAsync_ShouldReturnNotFoundForMissingRequest()
    {
        await using var test = await CreateTestContextAsync();

        var result = await test.Service.AcceptAsync(
            Guid.NewGuid(),
            test.VolunteerId,
            CancellationToken.None);

        Assert.Equal(
            VolunteerAcceptanceStatus.RequestNotFound,
            result.Status);
    }

    [Fact]
    public async Task AcceptAsync_ShouldRejectAlreadyAcceptedRequest()
    {
        await using var test = await CreateTestContextAsync();

        var volunteerId = test.VolunteerId;

        var request = new RefillRequest(
            test.OfficeId,
            test.ProductId);

        request.AssignVolunteer(volunteerId);
        request.AcceptAssignment(volunteerId);

        test.Db.RefillRequests.Add(request);
        await test.Db.SaveChangesAsync();

        var result = await test.Service.AcceptAsync(
            request.Id,
            volunteerId,
            CancellationToken.None);

        Assert.Equal(
            VolunteerAcceptanceStatus.NotAssigned,
            result.Status);
    }

    private static async Task<TestContext> CreateTestContextAsync()
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

        var volunteer = new Employee(
            "Test Volunteer",
            "volunteer@test.com",
            office.Id,
            true);

        db.Offices.Add(office);
        db.Products.Add(product);
        db.Employees.Add(volunteer);

        await db.SaveChangesAsync();

        var factory = new TestDbContextFactory(options);
        var assignmentService = new VolunteerAssignmentService();

        var repository = new RefillRequestRepository(
            db,
            factory,
            assignmentService);

        var timeoutService = new AssignmentTimeoutService(
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var service = new VolunteerAcceptanceService(
            repository,
            timeoutService);

        return new TestContext(
            connection,
            db,
            office.Id,
            product.Id,
            volunteer.Id,
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

    private sealed record TestContext(
        SqliteConnection Connection,
        AppDbContext Db,
        Guid OfficeId,
        Guid ProductId,
        Guid VolunteerId,
        VolunteerAcceptanceService Service) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
