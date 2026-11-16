
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;

namespace SnackCounter.Application.Services;

public class AssignmentReassignmentService
{
    private readonly IRefillRequestRepository _repository;
    private readonly IAssignmentTimeoutService _timeoutService;
    private readonly IChatNotificationService _chatNotificationService;

    public AssignmentReassignmentService(
        IRefillRequestRepository repository,
        IAssignmentTimeoutService timeoutService,
        IChatNotificationService chatNotificationService)
    {
        _repository = repository;
        _timeoutService = timeoutService;
        _chatNotificationService = chatNotificationService;
    }

    public async Task<int> ProcessExpiredAssignmentsAsync(
        CancellationToken cancellationToken)
    {
        var assignedRequests =
            await _repository.GetAssignedRequestsAsync(cancellationToken);

        var reassignedCount = 0;

        foreach (var request in assignedRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_timeoutService.HasExpired(request))
                continue;

            var result =
                await _repository.TryReassignExpiredRequestAsync(
                    request.Id,
                    request.ConcurrencyVersion,
                    cancellationToken);

            if (result.Status != AtomicAssignmentStatus.Assigned ||
                result.VolunteerId is null)
            {
                continue;
            }

            reassignedCount++;

            var volunteers = await _repository.GetAllVolunteersAsync(
                request.OfficeId,
                cancellationToken);

            var volunteer = volunteers.FirstOrDefault(
                x => x.Id == result.VolunteerId.Value);

            var office = await _repository.GetOfficeAsync(
                request.OfficeId,
                cancellationToken);

            var product = await _repository.GetActiveProductAsync(
                request.ProductId,
                cancellationToken);

            if (volunteer is null || office is null || product is null)
                continue;

            try
            {
                await _chatNotificationService.SendAssignmentNotificationAsync(
                    volunteer.Email,
                    product.Name,
                    office.Name,
                    request.Id,
                    cancellationToken);
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException ||
                !cancellationToken.IsCancellationRequested)
            {
                // Assignment is already committed.
                // A production implementation should record notification
                // failures and retry them through an outbox.
            }
        }

        return reassignedCount;
    }
}
