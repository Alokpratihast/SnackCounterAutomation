using SnackCounter.Domain.Entities;

namespace SnackCounter.Application.Interfaces;

public interface IReimbursementRepository
{
    Task<(Reimbursement? Claim, Purchase? Purchase, Employee? Employee, Product? Product)> GetClaimContextAsync(
        Guid purchaseId, CancellationToken cancellationToken);
    Task MarkSubmittedAsync(Guid purchaseId, string externalClaimId, CancellationToken cancellationToken);
    Task RecordSubmissionFailureAsync(Guid purchaseId, string reason, CancellationToken cancellationToken);
    Task MarkReimbursedAsync(Guid purchaseId, CancellationToken cancellationToken);
    Task MarkRejectedAsync(Guid purchaseId, string reason, CancellationToken cancellationToken);
}
