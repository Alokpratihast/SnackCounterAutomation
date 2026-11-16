using Microsoft.EntityFrameworkCore;
using SnackCounter.API.Contracts;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Models;
using SnackCounter.Application.Services;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;
using SnackCounter.Infrastructure.Integrations.Chat;
using SnackCounter.Infrastructure.Integrations.HR;
using SnackCounter.Infrastructure.Reporting;
using SnackCounter.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=snackcounter.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

    
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

builder.Services.AddScoped<IVolunteerAssignmentService,
    VolunteerAssignmentService>();

builder.Services.AddScoped<IAssignmentTimeoutService,
    AssignmentTimeoutService>();

builder.Services.AddScoped<IChatNotificationService,
    SimulatedChatNotificationService>();

builder.Services.AddScoped<IHRReimbursementService,
    SimulatedHRReimbursementService>();


builder.Services.AddScoped<IRefillRequestRepository, RefillRequestRepository>();
builder.Services.AddScoped<LowStockRefillRequestService>();
builder.Services.AddScoped<VolunteerAcceptanceService>();
builder.Services.AddScoped<AssignmentReassignmentService>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<IReimbursementRepository, ReimbursementRepository>();
builder.Services.AddScoped<ReimbursementSubmissionService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<BudgetConfigurationService>();
builder.Services.AddScoped<DemoSeedService>();
builder.Services.AddScoped<IMonthlyReportService, MonthlyReportService>();
builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}



app.MapGet("/", () => Results.Ok(new
{
    Application = "Snack Counter Automation",
    Status = "Running"
}));

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy"
}));

app.MapPost("/api/refill-requests/low-stock", async (
    CreateLowStockRefillRequestRequest request,
    LowStockRefillRequestService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(
        request.OfficeId,
        request.ProductId,
        cancellationToken);

    return result.Status switch
    {
        RefillRequestCreationStatus.CreatedAndAssigned =>
            Results.Created(
                $"/api/refill-requests/{result.RefillRequestId}",
                result),

        RefillRequestCreationStatus.CreatedPendingAssignment =>
            Results.Accepted(
                $"/api/refill-requests/{result.RefillRequestId}",
                result),

        RefillRequestCreationStatus.AlreadyExists =>
            Results.Conflict(result),

        RefillRequestCreationStatus.OfficeNotFound =>
            Results.NotFound("Office not found."),

        RefillRequestCreationStatus.ProductNotFoundOrInactive =>
            Results.NotFound("Active product not found."),

        RefillRequestCreationStatus.InventoryNotFound =>
            Results.NotFound("Inventory record not found."),

        RefillRequestCreationStatus.StockNotLow =>
            Results.BadRequest("Product stock is not low."),

        _ => Results.BadRequest(result)
    };
});



app.MapPost("/api/refill-requests/{requestId:guid}/accept", async (
    Guid requestId,
    AcceptRefillRequestRequest request,
    VolunteerAcceptanceService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.AcceptAsync(
        requestId,
        request.VolunteerId,
        cancellationToken);

    return result.Status switch
    {
        VolunteerAcceptanceStatus.Accepted =>
            Results.Ok(result),

        VolunteerAcceptanceStatus.RequestNotFound =>
            Results.NotFound(result),

        VolunteerAcceptanceStatus.NotAssigned =>
            Results.Conflict(result),

        VolunteerAcceptanceStatus.WrongVolunteer =>
            Results.Forbid(),

        VolunteerAcceptanceStatus.AssignmentExpired =>
            Results.Conflict(result),

        _ => Results.BadRequest(result)
    };
});


app.MapPost("/api/refill-requests/process-expired", async (
    AssignmentReassignmentService service,
    CancellationToken cancellationToken) =>
{
    var count = await service.ProcessExpiredAssignmentsAsync(
        cancellationToken);

    return Results.Ok(new
    {
        ReassignedCount = count,
        Message = $"{count} expired assignment(s) processed."
    });
});



app.MapPost("/api/refill-requests/{requestId:guid}/purchase", async (
    Guid requestId, RecordPurchaseRequest request, PurchaseService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RecordAsync(new(
        requestId, request.VolunteerId, request.Quantity, request.UnitPrice,
        request.CurrencyCode, request.ReceiptUrl), cancellationToken);
    return result.Status switch
    {
        RecordPurchaseStatus.Purchased => Results.Created($"/api/purchases/{result.PurchaseId}", result),
        RecordPurchaseStatus.RequestNotFound => Results.NotFound(result),
        RecordPurchaseStatus.InventoryNotFound => Results.NotFound(result),
        RecordPurchaseStatus.BudgetNotFound or RecordPurchaseStatus.BudgetExceeded or RecordPurchaseStatus.DuplicatePurchase => Results.Conflict(result),
        RecordPurchaseStatus.RequestNotAccepted or RecordPurchaseStatus.WrongVolunteer or RecordPurchaseStatus.VolunteerNotEligible => Results.Conflict(result),
        RecordPurchaseStatus.CurrencyMismatch => Results.BadRequest(result),
        _ => Results.BadRequest(result)
    };
});

app.MapPost("/api/purchases/{purchaseId:guid}/retry-hr-claim", async (
    Guid purchaseId, ReimbursementSubmissionService service, CancellationToken cancellationToken) =>
{
    var submitted = await service.SubmitAsync(purchaseId, cancellationToken);
    return submitted ? Results.Ok(new { Status = "Submitted" }) : Results.Conflict(new { Status = "Pending", Message = "Claim is not available or HR submission failed." });
});

app.MapPut("/api/budgets", async (
    ConfigureBudgetRequest request, BudgetConfigurationService service, CancellationToken cancellationToken) =>
{
    try
    {
        var budget = await service.ConfigureAsync(request.OfficeId, request.Year, request.Month, request.MonthlyLimit, cancellationToken);
        return Results.Ok(new { budget.OfficeId, budget.Year, budget.Month, budget.MonthlyLimit, budget.CurrencyCode, budget.ActualSpent, budget.ReservedAmount, budget.AvailableBudget });
    }
    catch (KeyNotFoundException ex) { return Results.NotFound(ex.Message); }
    catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
});

app.MapPost("/api/reports/monthly/{officeId:guid}", async (
    Guid officeId, MonthlyReportRequest request, IMonthlyReportService service, CancellationToken cancellationToken) =>
{
    var report = await service.GetAsync(officeId, request.Year, request.Month,
        request.ReportingCurrency, request.RatesToReportingCurrency, cancellationToken);
    return report is null ? Results.NotFound() : Results.Ok(report);
});


app.MapPost("/api/purchases/{purchaseId:guid}/hr-reimbursed", async (
    Guid purchaseId, IReimbursementRepository repository, CancellationToken cancellationToken) =>
{
    try
    {
        await repository.MarkReimbursedAsync(purchaseId, cancellationToken);
        return Results.Ok(new { PurchaseId = purchaseId, Status = "Reimbursed" });
    }
    catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
});

app.MapPost("/api/purchases/{purchaseId:guid}/hr-rejected", async (
    Guid purchaseId, HrRejectionRequest request, IReimbursementRepository repository, CancellationToken cancellationToken) =>
{
    try
    {
        await repository.MarkRejectedAsync(purchaseId, request.Reason, cancellationToken);
        return Results.Ok(new { PurchaseId = purchaseId, Status = "Rejected", request.Reason });
    }
    catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
});


app.MapPost("/api/demo/seed", async (DemoSeedService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.SeedAsync(cancellationToken)));

app.MapPut("/api/inventory/{officeId:guid}/{productId:guid}/count", async (
    Guid officeId, Guid productId, ManualInventoryCountRequest request,
    AppDbContext db, CancellationToken cancellationToken) =>
{
    if (request.Quantity < 0) return Results.BadRequest("Quantity cannot be negative.");
    var inventory = await db.Inventories.SingleOrDefaultAsync(
        x => x.OfficeId == officeId && x.ProductId == productId, cancellationToken);
    if (inventory is null) return Results.NotFound("Inventory record not found.");
    inventory.UpdateStock(request.Quantity);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(new { inventory.OfficeId, inventory.ProductId, inventory.CurrentQuantity, inventory.MinimumQuantity, inventory.LastUpdatedAtUtc });
});

app.Run();
