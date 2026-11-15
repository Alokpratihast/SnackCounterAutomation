
using SnackCounter.Application.Interfaces;
using SnackCounter.Domain.Entities;

namespace SnackCounter.Application.Services;

public class VolunteerAssignmentService : IVolunteerAssignmentService
{
    public Employee? SelectNextVolunteer(
        Guid officeId,
        IEnumerable<Employee> employees,
        Guid? lastAssignedVolunteerId = null)
    {
        var eligibleVolunteers = employees
            .Where(e =>
                e.OfficeId == officeId &&
                e.IsVolunteer &&
                e.IsAvailable)
            .OrderBy(e => e.Name)
            .ToList();

        if (eligibleVolunteers.Count == 0)
            return null;

        if (lastAssignedVolunteerId is null)
            return eligibleVolunteers[0];

        var lastIndex = eligibleVolunteers.FindIndex(
            e => e.Id == lastAssignedVolunteerId.Value);

        if (lastIndex < 0)
            return eligibleVolunteers[0];

        var nextIndex = (lastIndex + 1) % eligibleVolunteers.Count;

        return eligibleVolunteers[nextIndex];
    }
}
