using SnackCounter.Application.Interfaces;

namespace SnackCounter.Infrastructure.Integrations.Chat;

public class SimulatedChatNotificationService
    : IChatNotificationService
{
    public Task SendAssignmentNotificationAsync(
        string employeeEmail,
        string productName,
        string officeName,
        Guid refillRequestId,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine("========== CHAT NOTIFICATION ==========");
        Console.WriteLine($"To: {employeeEmail}");
        Console.WriteLine($"Office: {officeName}");
        Console.WriteLine($"Product: {productName}");
        Console.WriteLine($"Refill ID: {refillRequestId}");
        Console.WriteLine("Action: Accept within 1 hour.");
        Console.WriteLine("=======================================");

        return Task.CompletedTask;
    }
}