namespace SnackCounter.API.Contracts;

public sealed record CreateLowStockRefillRequestRequest(
    Guid OfficeId,
    Guid ProductId);