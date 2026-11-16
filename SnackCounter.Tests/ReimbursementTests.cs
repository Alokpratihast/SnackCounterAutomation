using SnackCounter.Domain.Entities;

namespace SnackCounter.Tests;

public sealed class ReimbursementTests
{
    [Fact]
    public void PendingClaim_CanBeSubmittedAndMarkedReimbursed()
    {
        var claim = new Reimbursement(Guid.NewGuid());
        claim.MarkSubmitted("HR-123");
        claim.MarkReimbursed();
        Assert.Equal("Reimbursed", claim.Status);
        Assert.NotNull(claim.ReimbursedAtUtc);
    }

    [Fact]
    public void PendingClaim_CanRecordFailureAndBeRetried()
    {
        var claim = new Reimbursement(Guid.NewGuid());
        claim.RecordSubmissionFailure("HR unavailable");
        Assert.Equal("Pending", claim.Status);
        Assert.Equal("HR unavailable", claim.FailureReason);
        claim.MarkSubmitted("HR-456");
        Assert.Equal("Submitted", claim.Status);
        Assert.Null(claim.FailureReason);
    }

    [Fact]
    public void SubmittedClaim_CanBeRejectedButCannotBeResubmitted()
    {
        var claim = new Reimbursement(Guid.NewGuid());
        claim.MarkSubmitted("HR-789");
        claim.MarkRejected("Receipt unreadable");
        Assert.Equal("Rejected", claim.Status);
        Assert.Throws<InvalidOperationException>(() => claim.MarkSubmitted("HR-789"));
    }
}
