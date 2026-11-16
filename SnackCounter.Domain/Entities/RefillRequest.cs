
using SnackCounter.Domain.Enums;

namespace SnackCounter.Domain.Entities;

public class RefillRequest
{
    public Guid Id { get; private set; }

    public Guid OfficeId { get; private set; }

    public Guid ProductId { get; private set; }

    public RefillStatus Status { get; private set; }

    public long ConcurrencyVersion { get; private set; }

    public Guid? AssignedVolunteerId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? AssignedAtUtc { get; private set; }

    public DateTime? AcceptedAtUtc { get; private set; }

    public DateTime? PurchasedAtUtc { get; private set; }

    public DateTime? ReimbursedAtUtc { get; private set; }

    private RefillRequest() { } // EF Core

    public RefillRequest(Guid officeId, Guid productId)
    {
        if (officeId == Guid.Empty)
            throw new ArgumentException("Office is required.");

        if (productId == Guid.Empty)
            throw new ArgumentException("Product is required.");

        Id = Guid.NewGuid();
        OfficeId = officeId;
        ProductId = productId;
        Status = RefillStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void AssignVolunteer(Guid volunteerId)
    {
        if (volunteerId == Guid.Empty)
            throw new ArgumentException("Volunteer is required.");

        if (Status != RefillStatus.Pending &&
            Status != RefillStatus.Expired)
            throw new InvalidOperationException(
                "Refill request cannot be assigned in its current status.");

        AssignedVolunteerId = volunteerId;
        AssignedAtUtc = DateTime.UtcNow;
        AcceptedAtUtc = null;
        Status = RefillStatus.Assigned;

        ConcurrencyVersion++;
    }

    public void AcceptAssignment(Guid volunteerId)
    {
        if (Status != RefillStatus.Assigned)
            throw new InvalidOperationException("No active assignment.");

        if (AssignedVolunteerId != volunteerId)
            throw new InvalidOperationException(
                "This assignment belongs to another volunteer.");

        AcceptedAtUtc = DateTime.UtcNow;
        Status = RefillStatus.Accepted;

        ConcurrencyVersion++;
    }

    public void MarkPurchased()
    {
        if (Status != RefillStatus.Accepted)
            throw new InvalidOperationException(
                "Assignment must be accepted before purchase.");

        PurchasedAtUtc = DateTime.UtcNow;
        Status = RefillStatus.Purchased;

        ConcurrencyVersion++;
    }

    public void MarkReimbursed()
    {
        if (Status != RefillStatus.Purchased)
            throw new InvalidOperationException(
                "Purchase must be completed before reimbursement.");

        ReimbursedAtUtc = DateTime.UtcNow;
        Status = RefillStatus.Reimbursed;

        ConcurrencyVersion++;
    }

    public void ExpireAssignment()
    {
        if (Status != RefillStatus.Assigned)
            throw new InvalidOperationException(
                "Only assigned requests can expire.");

        Status = RefillStatus.Expired;
        AssignedVolunteerId = null;

        ConcurrencyVersion++;
    }

    public void ReturnToPending()
    {
        if (Status != RefillStatus.Expired)
            throw new InvalidOperationException(
                "Only expired requests can return to pending.");

        Status = RefillStatus.Pending;

        ConcurrencyVersion++;
    }
}
