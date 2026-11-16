
using SnackCounter.Application.Interfaces;
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;

namespace SnackCounter.Application.Services;

public enum VolunteerAcceptanceStatus
{
    Accepted,
    RequestNotFound,
    NotAssigned,
    WrongVolunteer,
    AssignmentExpired
}

public record VolunteerAcceptanceResult(
    VolunteerAcceptanceStatus Status);

public class VolunteerAcceptanceService
{
    private readonly IRefillRequestRepository _repository;
    private readonly IAssignmentTimeoutService _timeoutService;

    public VolunteerAcceptanceService(
        IRefillRequestRepository repository,
        IAssignmentTimeoutService timeoutService)
    {
        _repository = repository;
        _timeoutService = timeoutService;
    }

    
public async Task<VolunteerAcceptanceResult> AcceptAsync(
    Guid requestId,
    Guid volunteerId,
    CancellationToken cancellationToken)
{
    if (requestId == Guid.Empty)
        throw new ArgumentException("Request ID is required.");

    if (volunteerId == Guid.Empty)
        throw new ArgumentException("Volunteer ID is required.");

    var request = await _repository.GetByIdAsync(
        requestId,
        cancellationToken);

    if (request is null)
        return new(VolunteerAcceptanceStatus.RequestNotFound);

    if (request.Status != RefillStatus.Assigned)
        return new(VolunteerAcceptanceStatus.NotAssigned);

    if (request.AssignedVolunteerId != volunteerId)
        return new(VolunteerAcceptanceStatus.WrongVolunteer);

    if (_timeoutService.HasExpired(request))
    {
        request.ExpireAssignment();

        await _repository.SaveChangesAsync(cancellationToken);

        return new(VolunteerAcceptanceStatus.AssignmentExpired);
    }

    request.AcceptAssignment(volunteerId);

    await _repository.SaveChangesAsync(cancellationToken);

    return new(VolunteerAcceptanceStatus.Accepted);
}

}
