using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;

namespace SnackCounter.Application.Services;

public sealed class PurchaseService
{
    private readonly IPurchaseRepository _repository;
    private readonly ReimbursementSubmissionService _reimbursements;

    public PurchaseService(IPurchaseRepository repository, ReimbursementSubmissionService reimbursements)
    {
        _repository = repository;
        _reimbursements = reimbursements;
    }

    public async Task<RecordPurchaseResult> RecordAsync(
        RecordPurchaseCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RefillRequestId == Guid.Empty) throw new ArgumentException("Refill request is required.");
        if (command.VolunteerId == Guid.Empty) throw new ArgumentException("Volunteer is required.");
        if (command.Quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (command.UnitPrice <= 0 || decimal.Round(command.UnitPrice, 2) != command.UnitPrice)
            throw new ArgumentException("Unit price must be positive and have at most two decimal places.");
        if (string.IsNullOrWhiteSpace(command.CurrencyCode)) throw new ArgumentException("Currency is required.");
        if (string.IsNullOrWhiteSpace(command.ReceiptUrl)) throw new ArgumentException("Receipt reference is required.");

        var result = await _repository.RecordPurchaseAsync(command, cancellationToken);
        if (result.Status == RecordPurchaseStatus.Purchased && result.PurchaseId.HasValue)
        {
            var submitted = await _reimbursements.SubmitAsync(result.PurchaseId.Value, cancellationToken);
            return result with
            {
                Message = submitted
                    ? "Purchase recorded and HR claim submitted."
                    : "Purchase recorded. HR claim remains pending and can be retried."
            };
        }
        return result;
    }
}
