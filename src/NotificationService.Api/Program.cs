using Microsoft.EntityFrameworkCore;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("NotificationDb")
    ?? throw new InvalidOperationException("ConnectionStrings:NotificationDb is required."));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapGet("/health", async (NotificationDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "Healthy", database = "Connected" })
        : Results.Problem("Database unavailable", statusCode: 503))
    .WithName("Health").WithTags("Health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.Run();
