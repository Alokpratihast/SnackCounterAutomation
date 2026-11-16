
using Microsoft.EntityFrameworkCore;
using SnackCounter.Application.Interfaces;
using SnackCounter.Application.Services;
using SnackCounter.Infrastructure.Data;
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

app.Run();
