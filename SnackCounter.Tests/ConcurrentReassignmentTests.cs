
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

public class ConcurrentReassignmentTests
{
    [Fact]
    public async Task TwoWorkersProcessingSameExpiredRequest_ShouldAllowOnlyOneReassignment()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"snack-concurrency-{Guid.NewGuid():N}.db");

        var connectionString =
            $"Data Source={databasePath};Default Timeout=10";

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;

            Guid officeId;
            Guid productId;
            Guid volunteer1Id;
            Guid volunteer2Id;
            Guid requestId;
            long expectedVersion;

            await using (var setupDb = new AppDbContext(options))
            {
                await setupDb.Database.EnsureCreatedAsync();

                var office = new Office("Oslo", "NOK");
                var product = new Product("Coffee", "Beverages");

                var volunteer1 = new Employee(
                    "Amit", "amit@test.com", office.Id, true);

                var volunteer2 = new Employee(
                    "Neha", "neha@test.com", office.Id, true);

                setupDb.AddRange(
                    office,
                    product,
                    volunteer1,
                    volunteer2);

                await setupDb.SaveChangesAsync();

                var request = new RefillRequest(
                    office.Id,
                    product.Id);

                request.AssignVolunteer(volunteer1.Id);
                office.UpdateVolunteerRotation(volunteer1.Id);

                setupDb.RefillRequests.Add(request);
                await setupDb.SaveChangesAsync();

                officeId = office.Id;
                productId = product.Id;
                volunteer1Id = volunteer1.Id;
                volunteer2Id = volunteer2.Id;
                requestId = request.Id;
                expectedVersion = request.ConcurrencyVersion;
            }

            var worker1 = RunWorkerAsync(
                options,
                requestId,
                expectedVersion);

            var worker2 = RunWorkerAsync(
                options,
                requestId,
                expectedVersion);

            var results = await Task.WhenAll(worker1, worker2);

            var successfulAssignments = results.Count(
                x => x.Status == AtomicAssignmentStatus.Assigned);

            var rejectedAssignments = results.Count(
                x => x.Status ==
                     AtomicAssignmentStatus.RequestNoLongerAssigned);

            Assert.Equal(1, successfulAssignments);
            Assert.Equal(1, rejectedAssignments);

            await using var verificationDb =
                new AppDbContext(options);

            var savedRequest = await verificationDb.RefillRequests
                .SingleAsync(x => x.Id == requestId);

            var savedOffice = await verificationDb.Offices
                .SingleAsync(x => x.Id == officeId);

            Assert.Equal(RefillStatus.Assigned, savedRequest.Status);

            Assert.Contains(
                savedRequest.AssignedVolunteerId,
                new Guid?[] { volunteer1Id, volunteer2Id });

            Assert.Equal(
                savedRequest.AssignedVolunteerId,
                savedOffice.LastAssignedVolunteerId);

            Assert.Equal(expectedVersion + 2,
                savedRequest.ConcurrencyVersion);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static async Task<AtomicVolunteerAssignmentResult>
        RunWorkerAsync(
            DbContextOptions<AppDbContext> options,
            Guid requestId,
            long expectedVersion)
    {
        await using var db = new AppDbContext(options);

        var factory = new TestDbContextFactory(options);

        var assignmentService = new VolunteerAssignmentService();

        var repository = new RefillRequestRepository(
            db,
            factory,
            assignmentService);

        return await repository.TryReassignExpiredRequestAsync(
            requestId,
            expectedVersion,
            CancellationToken.None);
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
}
