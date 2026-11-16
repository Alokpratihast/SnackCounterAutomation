using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Interfaces;
using SnackCounter.Domain.Entities;
using SnackCounter.Infrastructure.Data;

namespace SnackCounter.Infrastructure.Data.Repositories;

public sealed class ReimbursementRepository : IReimbursementRepository
{
    private readonly AppDbContext _db;
    public ReimbursementRepository(AppDbContext db) => _db = db;

    public async Task<(Reimbursement? Claim, Purchase? Purchase, Employee? Employee, Product? Product)> GetClaimContextAsync(
        Guid purchaseId, CancellationToken cancellationToken)
    {
        var claim = await _db.Reimbursements.SingleOrDefaultAsync(x => x.PurchaseId == purchaseId, cancellationToken);
        var purchase = await _db.Purchases.SingleOrDefaultAsync(x => x.Id == purchaseId, cancellationToken);
        if (purchase is null) return (claim, null, null, null);
        var employee = await _db.Employees.SingleOrDefaultAsync(x => x.Id == purchase.VolunteerId, cancellationToken);
        var request = await _db.RefillRequests.SingleOrDefaultAsync(x => x.Id == purchase.RefillRequestId, cancellationToken);
        var product = request is null ? null : await _db.Products.SingleOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        return (claim, purchase, employee, product);
    }

    public async Task MarkSubmittedAsync(Guid purchaseId, string externalClaimId, CancellationToken cancellationToken)
    {
        var claim = await _db.Reimbursements.SingleAsync(x => x.PurchaseId == purchaseId, cancellationToken);
        claim.MarkSubmitted(externalClaimId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordSubmissionFailureAsync(Guid purchaseId, string reason, CancellationToken cancellationToken)
    {
        var claim = await _db.Reimbursements.SingleAsync(x => x.PurchaseId == purchaseId, cancellationToken);
        claim.RecordSubmissionFailure(reason);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReimbursedAsync(Guid purchaseId, CancellationToken cancellationToken)
    {
        var claim = await _db.Reimbursements.SingleAsync(x => x.PurchaseId == purchaseId, cancellationToken);
        var purchase = await _db.Purchases.SingleAsync(x => x.Id == purchaseId, cancellationToken);
        var request = await _db.RefillRequests.SingleAsync(
            x => x.Id == purchase.RefillRequestId, cancellationToken);
        claim.MarkReimbursed();
        request.MarkReimbursed();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkRejectedAsync(Guid purchaseId, string reason, CancellationToken cancellationToken)
    {
        var claim = await _db.Reimbursements.SingleAsync(x => x.PurchaseId == purchaseId, cancellationToken);
        claim.MarkRejected(reason);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
