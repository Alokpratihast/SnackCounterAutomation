using SnackCounter.Application.Models;

namespace SnackCounter.Application.Interfaces;

public interface IPurchaseRepository
{
    Task<RecordPurchaseResult> RecordPurchaseAsync(
        RecordPurchaseCommand command,
        CancellationToken cancellationToken);
}
