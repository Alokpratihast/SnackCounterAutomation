
namespace SnackCounter.Domain.Entities;

public class Budget
{
    public Guid Id { get; private set; }

    public Guid OfficeId { get; private set; }

    public Office Office { get; private set; } = null!;

    public int Month { get; private set; }

    public int Year { get; private set; }

    public decimal MonthlyLimit { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal ActualSpent { get; private set; }

    public decimal ReservedAmount { get; private set; }

    public decimal AvailableBudget =>
        MonthlyLimit - ActualSpent - ReservedAmount;

    public decimal RemainingBudget => AvailableBudget;

    private Budget() { } // EF Core

    public Budget(
        Guid officeId,
        int month,
        int year,
        decimal monthlyLimit,
        string currencyCode)
    {
        if (officeId == Guid.Empty)
            throw new ArgumentException("Office is required.");

        if (month < 1 || month > 12)
            throw new ArgumentException("Invalid month.");

        if (year < 2020)
            throw new ArgumentException("Invalid year.");

        if (monthlyLimit <= 0)
            throw new ArgumentException("Budget must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency is required.");

        Id = Guid.NewGuid();
        OfficeId = officeId;
        Month = month;
        Year = year;
        MonthlyLimit = monthlyLimit;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
    }

    public bool CanSpend(decimal amount)
    {
        return amount > 0 && amount <= AvailableBudget;
    }

    public void RecordExpense(decimal amount)
    {
        if (!CanSpend(amount))
            throw new InvalidOperationException("Monthly budget exceeded.");

        ActualSpent += amount;
    }

    public void Reserve(decimal amount)
    {
        if (!CanSpend(amount))
            throw new InvalidOperationException(
                "Insufficient available budget.");

        ReservedAmount += amount;
    }

    public void ReleaseReservation(decimal amount)
    {
        if (amount <= 0 || amount > ReservedAmount)
            throw new InvalidOperationException(
                "Invalid reservation release.");

        ReservedAmount -= amount;
    }

    public void CommitReservation(decimal amount)
    {
        if (amount <= 0 || amount > ReservedAmount)
            throw new InvalidOperationException(
                "Invalid reservation commit.");

        ReservedAmount -= amount;
        ActualSpent += amount;
    }
}
