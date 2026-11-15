
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;

namespace SnackCounter.Tests;

public class RefillRequestTests
{
    [Fact]
    public void AcceptAssignment_ShouldAcceptOnlyAssignedVolunteer()
    {
        var officeId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var volunteer1 = Guid.NewGuid();
        var volunteer2 = Guid.NewGuid();

        var request = new RefillRequest(officeId, productId);

        request.AssignVolunteer(volunteer1);
        request.AcceptAssignment(volunteer1);

        Assert.Equal(RefillStatus.Accepted, request.Status);

        Assert.Throws<InvalidOperationException>(
            () => request.AcceptAssignment(volunteer2));
    }

    [Fact]
    public void ExpireAssignment_ShouldAllowReassignment()
    {
        var request = new RefillRequest(
            Guid.NewGuid(), Guid.NewGuid());

        var firstVolunteer = Guid.NewGuid();
        var secondVolunteer = Guid.NewGuid();

        request.AssignVolunteer(firstVolunteer);
        request.ExpireAssignment();
        request.AssignVolunteer(secondVolunteer);

        Assert.Equal(RefillStatus.Assigned, request.Status);
        Assert.Equal(secondVolunteer, request.AssignedVolunteerId);
    }

    [Fact]
    public void MarkReimbursed_ShouldFailBeforePurchase()
    {
        var request = new RefillRequest(
            Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(
            () => request.MarkReimbursed());
    }
}
    