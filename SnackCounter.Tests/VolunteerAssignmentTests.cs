 
using SnackCounter.Application.Services;
using SnackCounter.Domain.Entities;

namespace SnackCounter.Tests;

public class VolunteerAssignmentTests
{
    [Fact]
    public void SelectNextVolunteer_ShouldReturnVolunteerFromSameOffice()
    {
        var osloId = Guid.NewGuid();
        var bengaluruId = Guid.NewGuid();

        var osloVolunteer = new Employee(
            "Olivia", "olivia@example.com", osloId, true);

        var bengaluruVolunteer = new Employee(
            "Rahul", "rahul@example.com", bengaluruId, true);

        var service = new VolunteerAssignmentService();

        var result = service.SelectNextVolunteer(
            bengaluruId,
            new[] { osloVolunteer, bengaluruVolunteer });

        Assert.NotNull(result);
        Assert.Equal(bengaluruId, result.OfficeId);
    }

    [Fact]
    public void SelectNextVolunteer_ShouldSkipUnavailableVolunteer()
    {
        var officeId = Guid.NewGuid();

        var first = new Employee(
            "Amit", "amit@example.com", officeId, true);

        var second = new Employee(
            "Neha", "neha@example.com", officeId, true);

        first.UpdateAvailability(false);

        var service = new VolunteerAssignmentService();

        var result = service.SelectNextVolunteer(
            officeId,
            new[] { first, second });

        Assert.Equal(second.Id, result?.Id);
    }

    [Fact]
    public void SelectNextVolunteer_ShouldReturnNull_WhenNoVolunteerAvailable()
    {
        var officeId = Guid.NewGuid();

        var employee = new Employee(
            "Amit", "amit@example.com", officeId, false);

        var service = new VolunteerAssignmentService();

        var result = service.SelectNextVolunteer(
            officeId,
            new[] { employee });

        Assert.Null(result);
    }
}
