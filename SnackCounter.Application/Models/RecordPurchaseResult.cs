namespace SnackCounter.Application.Models;

public enum RecordPurchaseStatus
{
    Purchased, RequestNotFound, RequestNotAccepted, WrongVolunteer,
    InventoryNotFound, ProductNotFound, BudgetNotFound, CurrencyMismatch, BudgetExceeded,
    DuplicatePurchase, VolunteerNotEligible
}

public sealed record RecordPurchaseResult(
    RecordPurchaseStatus Status,
    Guid? PurchaseId = null,
    decimal? TotalAmount = null,
    decimal? RemainingBudget = null,
    string? Message = null,
    string? EmployeeEmail = null,
    string? ProductName = null);
