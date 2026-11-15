using SnackCounter.Domain.Entities;

namespace SnackCounter.Application.Interfaces;

public interface IVolunteerAssignmentService
{
    Employee? SelectNextVolunteer(
        Guid officeId,
        IEnumerable<Employee> employees,
        Guid? lastAssignedVolunteerId = null);
}