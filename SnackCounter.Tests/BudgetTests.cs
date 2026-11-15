
using SnackCounter.Domain.Entities;

namespace SnackCounter.Tests;

public class BudgetTests
{
    private readonly Guid _officeId = Guid.NewGuid();

    private Budget CreateBudget()
    {
        return new Budget(
            _officeId,
            11,
            2026,
            6000,
            "NOK");
    }

    [Fact]
    public void Reserve_ShouldReduceAvailableBudget()
    {
        var budget = CreateBudget();

        budget.Reserve(1000);

        Assert.Equal(5000, budget.AvailableBudget);
    }

    [Fact]
    public void CommitReservation_ShouldIncreaseActualSpent()
    {
        var budget = CreateBudget();

        budget.Reserve(1000);
        budget.CommitReservation(1000);

        Assert.Equal(1000, budget.ActualSpent);
        Assert.Equal(5000, budget.AvailableBudget);
    }

    [Fact]
    public void Reserve_ShouldReject_WhenBudgetExceeded()
    {
        var budget = CreateBudget();

        Assert.Throws<InvalidOperationException>(
            () => budget.Reserve(7000));
    }

    [Fact]
    public void ReleaseReservation_ShouldRestoreAvailableBudget()
    {
        var budget = CreateBudget();

        budget.Reserve(1500);
        budget.ReleaseReservation(1500);

        Assert.Equal(6000, budget.AvailableBudget);
    }
}
