using SnackCounter.Application.Interfaces;
using SnackCounter.Domain.Entities;

namespace SnackCounter.Application.Services;

public class AssignmentTimeoutService : IAssignmentTimeoutService
{
    private readonly TimeProvider _timeProvider;

    private static readonly TimeSpan AcceptanceWindow =
        TimeSpan.FromHours(1);

    public AssignmentTimeoutService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public bool HasExpired(RefillRequest request)
    {
        if (request.AssignedAtUtc is null)
            return false;

        var expiryTime = request.AssignedAtUtc.Value
                         + AcceptanceWindow;

        return _timeProvider.GetUtcNow().UtcDateTime >= expiryTime;
    }
}