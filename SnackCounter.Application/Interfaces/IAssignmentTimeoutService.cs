using SnackCounter.Domain.Entities;

namespace SnackCounter.Application.Interfaces;

public interface IAssignmentTimeoutService
{
    bool HasExpired(RefillRequest request);
}