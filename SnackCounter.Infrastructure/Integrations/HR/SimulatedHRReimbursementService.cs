using SnackCounter.Application.Interfaces;

namespace SnackCounter.Infrastructure.Integrations.HR;

public class SimulatedHRReimbursementService : IHRReimbursementService
{
    public Task<string> SubmitClaimAsync(
        Guid purchaseId,
        decimal amount,
        string currencyCode,
        string description,
        string receiptUrl,
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Stable ID makes retries idempotent in this simulated HR adapter.
        var claimId = $"HR-{purchaseId:N}";
        Console.WriteLine("========== HR CLAIM SUBMITTED ==========");
        Console.WriteLine($"Claim ID: {claimId}");
        Console.WriteLine($"Purchase ID: {purchaseId}");
        Console.WriteLine($"Employee ID: {employeeId}");
        Console.WriteLine($"Amount: {amount:0.00} {currencyCode}");
        Console.WriteLine($"Description: {description}");
        Console.WriteLine($"Receipt: {receiptUrl}");
        Console.WriteLine("Status: Submitted");
        Console.WriteLine("========================================");
        return Task.FromResult(claimId);
    }
}
