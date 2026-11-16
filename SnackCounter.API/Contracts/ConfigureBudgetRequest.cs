namespace SnackCounter.API.Contracts;
public sealed record ConfigureBudgetRequest(Guid OfficeId, int Year, int Month, decimal MonthlyLimit);
