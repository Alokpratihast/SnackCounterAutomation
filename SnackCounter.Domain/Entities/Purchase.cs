
namespace SnackCounter.Domain.Entities;

public class Purchase
{
    public Guid Id { get; private set; }

    public Guid RefillRequestId { get; private set; }

    public Guid VolunteerId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string ReceiptUrl { get; private set; } = string.Empty;

    public DateTime PurchasedAtUtc { get; private set; }

    private Purchase() { } // EF Core

    public Purchase(
        Guid refillRequestId,
        Guid volunteerId,
        decimal totalAmount,
        string currencyCode,
        string receiptUrl)
    {
        if (refillRequestId == Guid.Empty)
            throw new ArgumentException("Refill request is required.");

        if (volunteerId == Guid.Empty)
            throw new ArgumentException("Volunteer is required.");

        if (totalAmount <= 0)
            throw new ArgumentException("Purchase amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency is required.");

        if (string.IsNullOrWhiteSpace(receiptUrl))
            throw new ArgumentException("Receipt is required.");

        Id = Guid.NewGuid();
        RefillRequestId = refillRequestId;
        VolunteerId = volunteerId;
        TotalAmount = totalAmount;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        ReceiptUrl = receiptUrl.Trim();
        PurchasedAtUtc = DateTime.UtcNow;
    }
}
