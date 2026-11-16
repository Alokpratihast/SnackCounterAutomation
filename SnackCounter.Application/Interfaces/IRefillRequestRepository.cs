
using SnackCounter.Domain.Entities;
using SnackCounter.Application.Models;

namespace SnackCounter.Application.Interfaces;

public interface IRefillRequestRepository
{
    Task<Inventory?> GetInventoryAsync(
        Guid officeId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<Product?> GetActiveProductAsync(
        Guid productId,
        CancellationToken cancellationToken);

    Task<Office?> GetOfficeAsync(
        Guid officeId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveRequestAsync(
        Guid officeId,
        Guid productId,
        CancellationToken cancellationToken);

    Task<List<Employee>> GetAvailableVolunteersAsync(
        Guid officeId,
        CancellationToken cancellationToken);

    Task<Guid?> GetLastAssignedVolunteerIdAsync(
        Guid officeId,
        CancellationToken cancellationToken);

    Task AddAsync(
        RefillRequest request,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    Task<RefillRequest?> GetByIdAsync(
    Guid requestId,
    CancellationToken cancellationToken);

    Task<List<RefillRequest>> GetAssignedRequestsAsync(
    CancellationToken cancellationToken);

    Task UpdateOfficeRotationAsync(
    Guid officeId,
    Guid volunteerId,
    CancellationToken cancellationToken);

    Task<List<Employee>> GetAllVolunteersAsync(
    Guid officeId,
    CancellationToken cancellationToken);


    Task<AtomicVolunteerAssignmentResult> TryReassignExpiredRequestAsync(
    Guid requestId,
    long expectedConcurrencyVersion,
    CancellationToken cancellationToken);
}
