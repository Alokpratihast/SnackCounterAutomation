using SnackCounter.Application.Interfaces;

namespace SnackCounter.Application.Services;

public sealed class ReimbursementSubmissionService
{
    private readonly IReimbursementRepository _repository;
    private readonly IHRReimbursementService _hr;

    public ReimbursementSubmissionService(IReimbursementRepository repository, IHRReimbursementService hr)
    {
        _repository = repository;
        _hr = hr;
    }

    public async Task<bool> SubmitAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        var data = await _repository.GetClaimContextAsync(purchaseId, cancellationToken);
        if (data.Claim is null || data.Purchase is null || data.Employee is null || data.Product is null)
            return false;
        if (data.Claim.Status == "Submitted" || data.Claim.Status == "Reimbursed")
            return true;
        if (data.Claim.Status != "Pending")
            return false;

        try
        {
            var claimId = await _hr.SubmitClaimAsync(
                data.Purchase.Id, data.Purchase.TotalAmount, data.Purchase.CurrencyCode,
                $"{data.Purchase.Quantity} x {data.Product.Name}",
                data.Purchase.ReceiptUrl, data.Employee.Id.ToString(), cancellationToken);
            await _repository.MarkSubmittedAsync(purchaseId, claimId, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _repository.RecordSubmissionFailureAsync(purchaseId, ex.Message, cancellationToken);
            return false;
        }
    }
}
