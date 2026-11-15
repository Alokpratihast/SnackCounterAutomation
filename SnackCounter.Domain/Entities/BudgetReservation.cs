namespace SnackCounter.Domain.Entities;

public class BudgetReservation
{
    public Guid Id { get; private set; }

    public Guid BudgetId { get; private set; }

    public decimal Amount { get; private set; }

    public Guid RefillRequestId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public bool IsReleased { get; private set; }

    public bool IsCommitted { get; private set; }

    private BudgetReservation() { } // EF Core

    public BudgetReservation(
        Guid budgetId,
        Guid refillRequestId,
        decimal amount)
    {
        if (budgetId == Guid.Empty)
            throw new ArgumentException("Budget is required.");

        if (refillRequestId == Guid.Empty)
            throw new ArgumentException("Refill request is required.");

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        Id = Guid.NewGuid();
        BudgetId = budgetId;
        RefillRequestId = refillRequestId;
        Amount = amount;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Release()
    {
        if (IsCommitted)
            throw new InvalidOperationException(
                "Committed reservation cannot be released.");

        IsReleased = true;
    }

    public void Commit()
    {
        if (IsReleased)
            throw new InvalidOperationException(
                "Released reservation cannot be committed.");

        if (IsCommitted)
            throw new InvalidOperationException(
                "Reservation is already committed.");

        IsCommitted = true;
    }
}