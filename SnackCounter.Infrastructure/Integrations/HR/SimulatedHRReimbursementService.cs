using SnackCounter.Application.Interfaces;

namespace SnackCounter.Infrastructure.Integrations.HR;

public class SimulatedHRReimbursementService
    : IHRReimbursementService
{
    public Task<string> SubmitClaimAsync(
        Guid purchaseId,
        decimal amount,
        string currencyCode,
        string description,
        string receiptUrl,
        string employeeEmail,
        CancellationToken cancellationToken = default)
    {
        var claimId = $"HR-{Guid.NewGuid():N}";

        Console.WriteLine("========== HR CLAIM SUBMITTED ==========");
        Console.WriteLine($"Claim ID: {claimId}");
        Console.WriteLine($"Purchase ID: {purchaseId}");
        Console.WriteLine($"Employee: {employeeEmail}");
        Console.WriteLine($"Amount: {amount} {currencyCode}");
        Console.WriteLine($"Description: {description}");
        Console.WriteLine($"Receipt: {receiptUrl}");
        Console.WriteLine("Status: Submitted");
        Console.WriteLine("========================================");

        return Task.FromResult(claimId);
    }
}