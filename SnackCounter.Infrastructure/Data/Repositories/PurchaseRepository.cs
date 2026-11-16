using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;
using SnackCounter.Domain.Entities;
using SnackCounter.Domain.Enums;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure.Data.Repositories;

public sealed class PurchaseRepository : IPurchaseRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PurchaseRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<RecordPurchaseResult> RecordPurchaseAsync(
        RecordPurchaseCommand command,
        CancellationToken cancellationToken)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        // An immediate SQLite write transaction serializes purchase attempts before validation.
        await using var transaction = connection.BeginTransaction(deferred: false);
        await db.Database.UseTransactionAsync(transaction, cancellationToken);

        var request = await db.RefillRequests.SingleOrDefaultAsync(
            x => x.Id == command.RefillRequestId, cancellationToken);
        if (request is null) return new(RecordPurchaseStatus.RequestNotFound);
        var duplicate = await db.Purchases.AnyAsync(
            x => x.RefillRequestId == request.Id, cancellationToken);
        if (duplicate) return new(RecordPurchaseStatus.DuplicatePurchase);
        if (request.Status != RefillStatus.Accepted)
            return new(RecordPurchaseStatus.RequestNotAccepted);
        if (request.AssignedVolunteerId != command.VolunteerId)
            return new(RecordPurchaseStatus.WrongVolunteer);

        var employee = await db.Employees.SingleOrDefaultAsync(
            x => x.Id == command.VolunteerId, cancellationToken);
        if (employee is null || !employee.IsVolunteer || employee.OfficeId != request.OfficeId)
            return new(RecordPurchaseStatus.VolunteerNotEligible);

        var office = await db.Offices.SingleAsync(x => x.Id == request.OfficeId, cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == request.ProductId && x.IsActive, cancellationToken);
        if (product is null) return new(RecordPurchaseStatus.ProductNotFound);
        var inventory = await db.Inventories.SingleOrDefaultAsync(
            x => x.OfficeId == request.OfficeId && x.ProductId == request.ProductId, cancellationToken);
        if (inventory is null) return new(RecordPurchaseStatus.InventoryNotFound);

        var currency = command.CurrencyCode.Trim().ToUpperInvariant();
        if (currency != office.CurrencyCode)
            return new(RecordPurchaseStatus.CurrencyMismatch, Message: $"Office currency is {office.CurrencyCode}.");

        var now = DateTime.UtcNow;
        var budget = await db.Budgets.SingleOrDefaultAsync(
            x => x.OfficeId == office.Id && x.Year == now.Year && x.Month == now.Month,
            cancellationToken);
        if (budget is null) return new(RecordPurchaseStatus.BudgetNotFound,
            Message: $"No budget configured for {now:yyyy-MM}.");
        if (budget.CurrencyCode != currency)
            return new(RecordPurchaseStatus.CurrencyMismatch, Message: "Budget and purchase currencies differ.");

        var total = decimal.Round(command.Quantity * command.UnitPrice, 2, MidpointRounding.AwayFromZero);
        if (!budget.CanSpend(total))
            return new(RecordPurchaseStatus.BudgetExceeded, TotalAmount: total,
                RemainingBudget: budget.AvailableBudget);

        var purchase = new Purchase(request.Id, command.VolunteerId, command.Quantity,
            command.UnitPrice, currency, command.ReceiptUrl);
        var reservation = new BudgetReservation(budget.Id, request.Id, total);

        budget.Reserve(total);
        db.BudgetReservations.Add(reservation);
        reservation.Commit();
        budget.CommitReservation(total);
        inventory.AddStock(command.Quantity);
        request.MarkPurchased();
        db.Purchases.Add(purchase);
        db.Reimbursements.Add(new Reimbursement(purchase.Id));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(RecordPurchaseStatus.Purchased, purchase.Id, total,
            budget.AvailableBudget, "Purchase recorded; HR claim is pending submission.",
            employee.Email, product.Name);
    }
}
