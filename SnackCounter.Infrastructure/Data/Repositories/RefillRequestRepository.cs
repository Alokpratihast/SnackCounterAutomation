
using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Interfaces;
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Exceptions;

namespace SnackCounter.Infrastructure.Data.Repositories;

public class RefillRequestRepository : IRefillRequestRepository
{
    private readonly AppDbContext _dbContext;

    public RefillRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Inventory?> GetInventoryAsync(
        Guid officeId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Inventories
            .FirstOrDefaultAsync(
                x => x.OfficeId == officeId &&
                     x.ProductId == productId,
                cancellationToken);
    }

    public Task<Product?> GetActiveProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(
                x => x.Id == productId && x.IsActive,
                cancellationToken);
    }

    public Task<Office?> GetOfficeAsync(
        Guid officeId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Offices
            .FirstOrDefaultAsync(
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
        return await _dbContext.RefillRequests
            .Where(x => x.OfficeId == officeId &&
                        x.AssignedVolunteerId != null)
            .OrderByDescending(x => x.AssignedAtUtc)
            .Select(x => x.AssignedVolunteerId)
            .FirstOrDefaultAsync(cancellationToken);
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
    catch (DbUpdateException ex)
    {
        var sqliteException = ex.GetBaseException() as SqliteException;

        var isDuplicateActiveRequest =
            sqliteException?.SqliteErrorCode == 19 &&
            sqliteException.Message.Contains(
                "RefillRequests.OfficeId, RefillRequests.ProductId",
                StringComparison.OrdinalIgnoreCase);

        if (isDuplicateActiveRequest)
        {
            throw new DuplicateActiveRefillRequestException();
        }

        throw;
    }
}
}
