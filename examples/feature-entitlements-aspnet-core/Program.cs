using System.Security.Claims;
using KilnBook;
using Subscrio.Core.Config;
using Subscrio.Core.DependencyInjection;
using Subscrio.Core.Domain.ValueObjects;
using SubscrioInstance = Subscrio.Core.Subscrio;

var verify = args.Contains("--verify");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args.Where(arg => arg != "--verify").ToArray(),
    EnvironmentName = verify ? Environments.Development : null
});
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("This sample's test identities require DOTNET_ENVIRONMENT=Development.");

var databaseName = verify
    ? $"KilnBookEntitlements_Check_{Guid.NewGuid():N}"
    : "KilnBookEntitlements";
var connectionString = LocalDatabase.ConnectionString(databaseName);
await LocalDatabase.CreateAsync(databaseName);

var config = new SubscrioConfig
{
    Database = new DatabaseConfig
    {
        ConnectionString = connectionString,
        DatabaseType = DatabaseType.SqlServer
    }
};
builder.Services.AddSubscrio(config);
builder.Services.AddSingleton(new ReservationStore(connectionString));
builder.Services.AddAuthentication("Demo")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        DemoAuthenticationHandler>("Demo", _ => { });
builder.Services.AddAuthorizationBuilder().AddPolicy("CreateReservations", policy =>
    policy.RequireAuthenticatedUser()
          .RequireClaim("permission", "reservations:create")
          .RequireClaim("customer_key"));
builder.WebHost.UseUrls(verify ? "http://127.0.0.1:0" : "http://127.0.0.1:5078");
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

var app = builder.Build();
try
{
    using (var scope = app.Services.CreateScope())
    {
        var subscrio = scope.ServiceProvider.GetRequiredService<SubscrioInstance>();
        if (await subscrio.VerifySchemaAsync() is null)
            await subscrio.InstallSchemaAsync();
        await Catalog.SeedAsync(subscrio);
        await scope.ServiceProvider.GetRequiredService<ReservationStore>().InstallAsync();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapPost("/reservations", async (CreateReservation request, ClaimsPrincipal user,
        SubscrioInstance subscrio, ReservationStore reservations) =>
    {
        var customerKey = user.FindFirstValue("customer_key")!;
        if (request.Recurring && !await subscrio.FeatureChecker.IsEnabledForCustomerAsync(
                customerKey, "kilnbook", "recurring-bookings"))
            return Results.Json(new { error = "recurring_bookings_not_included" }, statusCode: 403);

        var limit = await subscrio.FeatureChecker.GetValueForCustomerAsync<int>(
            customerKey, "kilnbook", "active-reservations", 0);
        if (limit <= 0)
            return Results.Json(new { error = "no_reservation_allowance" }, statusCode: 403);

        var reservation = await reservations.TryCreateAsync(customerKey, request.Recurring, limit);
        return reservation is null
            ? Results.Json(new { error = "reservation_limit_reached", limit }, statusCode: 409)
            : Results.Json(reservation, statusCode: 201);
    }).RequireAuthorization("CreateReservations");

    if (verify)
    {
        await app.StartAsync();
        await SelfCheck.RunAsync(app.Urls.Single(), app.Services.GetRequiredService<ReservationStore>());
    }
    else
    {
        Console.WriteLine("KilnBook: http://127.0.0.1:5078");
        Console.WriteLine("Development-only bearer identities: basic-owner, studio-owner, studio-viewer, visitor-owner");
        await app.RunAsync();
    }
}
finally
{
    await app.StopAsync();
    await app.DisposeAsync();
    if (verify)
        await LocalDatabase.DropVerificationDatabaseAsync(databaseName);
}

public record CreateReservation(bool Recurring);
