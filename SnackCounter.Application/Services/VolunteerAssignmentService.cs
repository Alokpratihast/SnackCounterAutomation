
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
        // Keep all volunteers to preserve rotation order,
        // including those who are currently unavailable.
        var allVolunteers = employees
            .Where(e =>
                e.OfficeId == officeId &&
                e.IsVolunteer)
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Id)
            .ToList();

        if (allVolunteers.Count == 0)
            return null;

        var lastIndex = lastAssignedVolunteerId.HasValue
            ? allVolunteers.FindIndex(
                e => e.Id == lastAssignedVolunteerId.Value)
            : -1;

        // Check each volunteer after the last assigned volunteer.
        for (var offset = 1; offset <= allVolunteers.Count; offset++)
        {
            var index = (lastIndex + offset) % allVolunteers.Count;
            var candidate = allVolunteers[index];

            if (candidate.IsAvailable)
                return candidate;
        }

        return null;
    }
}
