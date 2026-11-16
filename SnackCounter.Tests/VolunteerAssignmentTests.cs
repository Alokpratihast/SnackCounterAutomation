 
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



[Fact]
public void SelectNextVolunteer_ShouldContinueRotation_WhenLastAssignedVolunteerIsUnavailable()
{
    var officeId = Guid.NewGuid();

    var amit = new Employee(
        "Amit", "amit@example.com", officeId, true);

    var neha = new Employee(
        "Neha", "neha@example.com", officeId, true);

    var rahul = new Employee(
        "Rahul", "rahul@example.com", officeId, true);

    amit.UpdateAvailability(false);

    var service = new VolunteerAssignmentService();

    var result = service.SelectNextVolunteer(
        officeId,
        new[] { amit, neha, rahul },
        amit.Id);

    Assert.NotNull(result);
    Assert.Equal(neha.Id, result.Id);
}

[Fact]
public void SelectNextVolunteer_ShouldContinueToNextVolunteer_InRoundRobinOrder()
{
    var officeId = Guid.NewGuid();

    var amit = new Employee(
        "Amit", "amit@example.com", officeId, true);

    var neha = new Employee(
        "Neha", "neha@example.com", officeId, true);

    var rahul = new Employee(
        "Rahul", "rahul@example.com", officeId, true);

    var service = new VolunteerAssignmentService();

    var first = service.SelectNextVolunteer(
        officeId,
        new[] { amit, neha, rahul });

    var second = service.SelectNextVolunteer(
        officeId,
        new[] { amit, neha, rahul },
        first!.Id);

    var third = service.SelectNextVolunteer(
        officeId,
        new[] { amit, neha, rahul },
        second!.Id);

    Assert.Equal(amit.Id, first.Id);
    Assert.Equal(neha.Id, second.Id);
    Assert.Equal(rahul.Id, third.Id);
}

}
