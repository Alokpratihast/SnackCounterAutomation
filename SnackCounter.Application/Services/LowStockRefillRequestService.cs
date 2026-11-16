
using SnackCounter.Application.Exceptions;
using SnackCounter.Application.Interfaces;
using SnackCounter.Domain.Entities;

namespace SnackCounter.Application.Services;

public enum RefillRequestCreationStatus
{
    CreatedAndAssigned,
    CreatedPendingAssignment,
    OfficeNotFound,
    ProductNotFoundOrInactive,
    InventoryNotFound,
    StockNotLow,
    AlreadyExists
}

public sealed record RefillRequestCreationResult(
    RefillRequestCreationStatus Status,
    Guid? RefillRequestId = null,
    Guid? AssignedVolunteerId = null,
    string? Warning = null);

public class LowStockRefillRequestService
{
    private readonly IRefillRequestRepository _repository;
    private readonly IVolunteerAssignmentService _volunteerAssignment;
    private readonly IChatNotificationService _chatNotification;

    public LowStockRefillRequestService(
        IRefillRequestRepository repository,
        IVolunteerAssignmentService volunteerAssignment,
        IChatNotificationService chatNotification)
    {
        _repository = repository;
        _volunteerAssignment = volunteerAssignment;
        _chatNotification = chatNotification;
    }

    public async Task<RefillRequestCreationResult> CreateAsync(
        Guid officeId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        if (officeId == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Office and product IDs are required.");

        var office = await _repository.GetOfficeAsync(
            officeId, cancellationToken);

        if (office is null)
            return new(RefillRequestCreationStatus.OfficeNotFound);

        var product = await _repository.GetActiveProductAsync(
            productId, cancellationToken);

        if (product is null)
            return new(RefillRequestCreationStatus.ProductNotFoundOrInactive);

        var inventory = await _repository.GetInventoryAsync(
            officeId, productId, cancellationToken);

        if (inventory is null)
            return new(RefillRequestCreationStatus.InventoryNotFound);

        if (!inventory.IsLowStock())
            return new(RefillRequestCreationStatus.StockNotLow);

        var alreadyExists = await _repository.HasActiveRequestAsync(
            officeId, productId, cancellationToken);

        if (alreadyExists)
            return new(RefillRequestCreationStatus.AlreadyExists);

        var request = new RefillRequest(officeId, productId);

        // Load all volunteers, including unavailable ones,
        // so the persistent rotation position is preserved.
        var volunteers = await _repository.GetAllVolunteersAsync(
            officeId, cancellationToken);

        var lastAssignedVolunteerId =
            await _repository.GetLastAssignedVolunteerIdAsync(
                officeId, cancellationToken);

        var selectedVolunteer = _volunteerAssignment.SelectNextVolunteer(
            officeId,
            volunteers,
            lastAssignedVolunteerId);

        if (selectedVolunteer is not null)
        {
            request.AssignVolunteer(selectedVolunteer.Id);

            // Update the office rotation cursor.
            // Both changes are persisted by the same SaveChanges call.
            await _repository.UpdateOfficeRotationAsync(
                officeId,
                selectedVolunteer.Id,
                cancellationToken);
        }

        await _repository.AddAsync(request, cancellationToken);

        try
        {
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateActiveRefillRequestException)
        {
            return new(RefillRequestCreationStatus.AlreadyExists);
        }

        if (selectedVolunteer is null)
        {
            return new(
                RefillRequestCreationStatus.CreatedPendingAssignment,
                request.Id);
        }

        try
        {
            await _chatNotification.SendAssignmentNotificationAsync(
                selectedVolunteer.Email,
                product.Name,
                office.Name,
                request.Id,
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is not OperationCanceledException ||
            !cancellationToken.IsCancellationRequested)
        {
            return new(
                RefillRequestCreationStatus.CreatedAndAssigned,
                request.Id,
                selectedVolunteer.Id,
                $"Request saved, but notification failed: {ex.Message}");
        }

        return new(
            RefillRequestCreationStatus.CreatedAndAssigned,
            request.Id,
            selectedVolunteer.Id);
    }
}
