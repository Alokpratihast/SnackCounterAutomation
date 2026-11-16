using Microsoft.EntityFrameworkCore;
using SnackCounter.API.Contracts;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Services;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Data.Repositories;
using SnackCounter.Infrastructure.Integrations.Chat;
using SnackCounter.Infrastructure.Integrations.HR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=snackcounter.db"));

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

app.Run();
