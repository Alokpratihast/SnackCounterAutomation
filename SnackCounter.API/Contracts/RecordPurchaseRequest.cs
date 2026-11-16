namespace SnackCounter.API.Contracts;

public sealed record RecordPurchaseRequest(
    Guid VolunteerId,
    int Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    string ReceiptUrl);
