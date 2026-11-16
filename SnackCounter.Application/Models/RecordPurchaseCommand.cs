namespace SnackCounter.Application.Models;

public sealed record RecordPurchaseCommand(
    Guid RefillRequestId,
    Guid VolunteerId,
    int Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    string ReceiptUrl);
