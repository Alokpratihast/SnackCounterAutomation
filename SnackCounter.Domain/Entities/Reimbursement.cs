
namespace SnackCounter.Domain.Entities;

public class Reimbursement
{
    public Guid Id { get; private set; }

    public Guid PurchaseId { get; private set; }

    public string ExternalClaimId { get; private set; } = string.Empty;

    public string Status { get; private set; } = "Pending";

    public DateTime SubmittedAtUtc { get; private set; }

    public DateTime? ReimbursedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    private Reimbursement() { } // EF Core

    public Reimbursement(Guid purchaseId)
    {
        if (purchaseId == Guid.Empty)
            throw new ArgumentException("Purchase is required.");

        Id = Guid.NewGuid();
        PurchaseId = purchaseId;
        Status = "Pending";
    }

    public void MarkSubmitted(string externalClaimId)
    {
        if (string.IsNullOrWhiteSpace(externalClaimId))
            throw new ArgumentException("External claim ID is required.");

        ExternalClaimId = externalClaimId;
        SubmittedAtUtc = DateTime.UtcNow;
        Status = "Submitted";
        FailureReason = null;
    }

    public void MarkReimbursed()
    {
        if (Status != "Submitted")
            throw new InvalidOperationException(
                "Only submitted claims can be reimbursed.");

        Status = "Reimbursed";
        ReimbursedAtUtc = DateTime.UtcNow;
    }

    public void MarkRejected(string reason)
    {
        if (Status != "Submitted")
            throw new InvalidOperationException(
                "Only submitted claims can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        Status = "Rejected";
        FailureReason = reason;
    }
}
