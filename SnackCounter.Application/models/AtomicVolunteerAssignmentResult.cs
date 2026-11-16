namespace SnackCounter.Application.Models;

public enum AtomicAssignmentStatus
{
    Assigned,
    NoVolunteerAvailable,
    RequestNotFound,
    RequestNoLongerAssigned,
    RetryLimitExceeded
}

public sealed record AtomicVolunteerAssignmentResult(
    AtomicAssignmentStatus Status,
    Guid? VolunteerId = null,
    string? Error = null);