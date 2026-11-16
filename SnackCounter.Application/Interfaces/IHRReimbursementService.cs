namespace SnackCounter.Application.Interfaces;

public interface IHRReimbursementService
{
    Task<string> SubmitClaimAsync(
        Guid purchaseId,
        decimal amount,
        string currencyCode,
        string description,
        string receiptUrl,
        string employeeId,
        CancellationToken cancellationToken = default);
}
