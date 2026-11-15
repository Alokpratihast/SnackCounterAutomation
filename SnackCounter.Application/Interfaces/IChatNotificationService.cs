namespace SnackCounter.Application.Interfaces;

public interface IChatNotificationService
{
    Task SendAssignmentNotificationAsync(
        string employeeEmail,
        string productName,
        string officeName,
        Guid refillRequestId,
        CancellationToken cancellationToken = default);
}