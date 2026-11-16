
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Exceptions;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;

namespace SnackCounter.Infrastructure.Data.Repositories;

public class RefillRequestRepository : IRefillRequestRepository
{
    private readonly AppDbContext _dbContext;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IVolunteerAssignmentService _assignmentService;

    private const int MaxRetryAttempts = 3;

    public RefillRequestRepository(
        AppDbContext dbContext,
        IDbContextFactory<AppDbContext> dbContextFactory,
        IVolunteerAssignmentService assignmentService)
    {
        _dbContext = dbContext;
        _dbContextFactory = dbContextFactory;
        _assignmentService = assignmentService;
    }

    public Task<Inventory?> GetInventoryAsync(
        Guid officeId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Inventories.FirstOrDefaultAsync(
            x => x.OfficeId == officeId &&
                 x.ProductId == productId,
            cancellationToken);
    }

    public Task<Product?> GetActiveProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Products.FirstOrDefaultAsync(
            x => x.Id == productId && x.IsActive,
            cancellationToken);
    }

    public Task<Office?> GetOfficeAsync(
        Guid officeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Offices.FirstOrDefaultAsync(
            x => x.Id == officeId,
            cancellationToken);
    }

    public Task<bool> HasActiveRequestAsync(
        Guid officeId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.RefillRequests.AnyAsync(
            x => x.OfficeId == officeId &&
                 x.ProductId == productId &&
                 (x.Status == RefillStatus.Pending ||
                  x.Status == RefillStatus.Assigned ||
                  x.Status == RefillStatus.Accepted),
            cancellationToken);
    }

    public Task<List<Employee>> GetAvailableVolunteersAsync(
        Guid officeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Employees
            .Where(x => x.OfficeId == officeId &&
                        x.IsVolunteer &&
                        x.IsAvailable)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid?> GetLastAssignedVolunteerIdAsync(
        Guid officeId,
        CancellationToken cancellationToken)
    {
        var office = await _dbContext.Offices.FirstOrDefaultAsync(
            x => x.Id == officeId,
            cancellationToken);

        return office?.LastAssignedVolunteerId;
    }

    public async Task AddAsync(
        RefillRequest request,
        CancellationToken cancellationToken)
    {
        await _dbContext.RefillRequests.AddAsync(
            request,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            var sqliteException = ex.GetBaseException() as SqliteException;

            var isDuplicateActiveRequest =
                sqliteException?.SqliteErrorCode == 19 &&
                sqliteException.Message.Contains(
                    "RefillRequests.OfficeId, RefillRequests.ProductId",
                    StringComparison.OrdinalIgnoreCase);

            if (isDuplicateActiveRequest)
                throw new DuplicateActiveRefillRequestException();

            throw;
        }
    }

    public Task<RefillRequest?> GetByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        return _dbContext.RefillRequests.FirstOrDefaultAsync(
            x => x.Id == requestId,
            cancellationToken);
    }

    public Task<List<RefillRequest>> GetAssignedRequestsAsync(
        CancellationToken cancellationToken)
    {
        return _dbContext.RefillRequests
            .Where(x => x.Status == RefillStatus.Assigned)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateOfficeRotationAsync(
        Guid officeId,
        Guid volunteerId,
        CancellationToken cancellationToken)
    {
        var office = await _dbContext.Offices.FirstOrDefaultAsync(
            x => x.Id == officeId,
            cancellationToken);

        if (office is null)
            throw new InvalidOperationException("Office not found.");

        office.UpdateVolunteerRotation(volunteerId);
    }

    public Task<List<Employee>> GetAllVolunteersAsync(
        Guid officeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Employees
            .Where(x => x.OfficeId == officeId && x.IsVolunteer)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<AtomicVolunteerAssignmentResult>
        TryReassignExpiredRequestAsync(
            Guid requestId,
            long expectedConcurrencyVersion,
            CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var dbContext =
                    await _dbContextFactory.CreateDbContextAsync(
                        cancellationToken);

                                var connection = (SqliteConnection)
                    dbContext.Database.GetDbConnection();

                await connection.OpenAsync(cancellationToken);

                // Acquire SQLite write lock before reading the request.
                // This serializes competing writers.
                await using var transaction =
                    connection.BeginTransaction(deferred: false);

                await dbContext.Database.UseTransactionAsync(
                    transaction,
                    cancellationToken);



                var request = await dbContext.RefillRequests
                    .FirstOrDefaultAsync(
                        x => x.Id == requestId,
                        cancellationToken);

                if (request is null)
                {
                    return new AtomicVolunteerAssignmentResult(
                        AtomicAssignmentStatus.RequestNotFound);
                }

                if (request.Status != RefillStatus.Assigned ||
                    request.ConcurrencyVersion != expectedConcurrencyVersion)
                {
                    return new AtomicVolunteerAssignmentResult(
                        AtomicAssignmentStatus.RequestNoLongerAssigned);
                }

                var office = await dbContext.Offices.FirstOrDefaultAsync(
                    x => x.Id == request.OfficeId,
                    cancellationToken);

                if (office is null)
                {
                    throw new InvalidOperationException(
                        "Office not found for refill request.");
                }

                var volunteers = await dbContext.Employees
                    .Where(x =>
                        x.OfficeId == request.OfficeId &&
                        x.IsVolunteer)
                    .OrderBy(x => x.Name)
                    .ThenBy(x => x.Id)
                    .ToListAsync(cancellationToken);

                var nextVolunteer = _assignmentService.SelectNextVolunteer(
                    request.OfficeId,
                    volunteers,
                    office.LastAssignedVolunteerId);

                request.ExpireAssignment();

                if (nextVolunteer is null)
                {
                    request.ReturnToPending();

                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return new AtomicVolunteerAssignmentResult(
                        AtomicAssignmentStatus.NoVolunteerAvailable);
                }

                request.AssignVolunteer(nextVolunteer.Id);
                office.UpdateVolunteerRotation(nextVolunteer.Id);

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new AtomicVolunteerAssignmentResult(
                    AtomicAssignmentStatus.Assigned,
                    nextVolunteer.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == MaxRetryAttempts)
                {
                    return new AtomicVolunteerAssignmentResult(
                        AtomicAssignmentStatus.RetryLimitExceeded,
                        Error: "Concurrency retry limit exceeded.");
                }
            }
            catch (Exception ex) when (IsSqliteBusyOrLocked(ex))
            {
                if (attempt == MaxRetryAttempts)
                {
                    return new AtomicVolunteerAssignmentResult(
                        AtomicAssignmentStatus.RetryLimitExceeded,
                        Error: "SQLite database remained busy after retries.");
                }
            }

            // Small bounded backoff before retrying with a fresh DbContext.
            await Task.Delay(
                TimeSpan.FromMilliseconds(50 * attempt),
                cancellationToken);
        }

        return new AtomicVolunteerAssignmentResult(
            AtomicAssignmentStatus.RetryLimitExceeded);
    }

    private static bool IsSqliteBusyOrLocked(Exception exception)
    {
        var current = exception;

        while (current is not null)
        {
            if (current is SqliteException sqliteException &&
                (sqliteException.SqliteErrorCode == 5 ||
                 sqliteException.SqliteErrorCode == 6))
            {
                return true;
            }

            current = current.InnerException!;
        }

        return false;
    }
}
