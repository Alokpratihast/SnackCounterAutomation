namespace SnackCounter.Application.Interfaces;

public interface IHRReimbursementService
{
    Task<string> SubmitClaimAsync(
        Guid purchaseId,
        decimal amount,
        string currencyCode,
        string description,
        string receiptUrl,
        string employeeEmail,
        CancellationToken cancellationToken = default);
}