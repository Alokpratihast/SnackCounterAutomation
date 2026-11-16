using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;

namespace SnackCounter.Application.Services;

public sealed class PurchaseService
{
    private readonly IPurchaseRepository _repository;

    public PurchaseService(IPurchaseRepository repository) => _repository = repository;

    public Task<RecordPurchaseResult> RecordAsync(
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
        return _repository.RecordPurchaseAsync(command, cancellationToken);
    }
}
