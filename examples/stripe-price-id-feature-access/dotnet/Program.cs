using System.Text.Json;
using FeedbackDesk.Api;
using Subscrio.Core.Config;
using Subscrio.Core.DependencyInjection;
using Subscrio.Core.Domain.ValueObjects;
using SubscrioInstance = Subscrio.Core.Subscrio;

var verify = args.Contains("--verify");
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Set DATABASE_URL to a dedicated PostgreSQL demo database.");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args.Where(arg => arg != "--verify").ToArray()
});
builder.Services.AddSubscrio(new SubscrioConfig
{
    Database = new DatabaseConfig
    {
        ConnectionString = connectionString,
        DatabaseType = DatabaseType.PostgreSQL
    }
});
builder.WebHost.UseUrls(verify ? "http://127.0.0.1:0" : "http://127.0.0.1:5079");
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

await using var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var subscrio = scope.ServiceProvider.GetRequiredService<SubscrioInstance>();
    if (await subscrio.VerifySchemaAsync() is null)
        await subscrio.InstallSchemaAsync();
    await Catalog.SeedAsync(subscrio);
}

app.MapGet("/api/demo/export/{customerKey}", async (string customerKey, SubscrioInstance subscrio) =>
{
    if (customerKey is not ("legacy-team" or "current-team" or "free-team"))
        return Results.NotFound(new { error = "demo_customer_not_found" });

    var allowed = await subscrio.FeatureChecker.IsEnabledForCustomerAsync(
        customerKey, "feedbackdesk", "csv-export");
    if (!allowed)
        return Results.Json(new { error = "export_not_included" }, statusCode: 403);
    return Results.Text("feedback,votes\nDark mode,12", "text/csv");
});

try
{
    if (verify)
    {
        await app.StartAsync();
        await SelfCheck.RunAsync(app.Urls.Single());
    }
    else
    {
        Console.WriteLine("FeedbackDesk API: http://127.0.0.1:5079/api/demo/export/legacy-team");
        Console.WriteLine("Demo route keys are fixed fixtures; replace them with your authenticated customer lookup.");
        await app.RunAsync();
    }
}
finally
{
    await app.StopAsync();
}

static class SelfCheck
{
    public static async Task RunAsync(string address)
    {
        using var http = new HttpClient { BaseAddress = new Uri(address) };
        await ExpectAsync(http, "legacy-team", 200, "feedback,votes\nDark mode,12");
        await ExpectAsync(http, "current-team", 200, "feedback,votes\nDark mode,12");
        await ExpectAsync(http, "free-team", 403, "export_not_included");
        await ExpectAsync(http, "not-a-demo-customer", 404, "demo_customer_not_found");
        Console.WriteLine("PASS: legacy and current Stripe price mappings allow export; Free is denied.");
    }

    private static async Task ExpectAsync(HttpClient http, string customer, int status, string body)
    {
        using var response = await http.GetAsync("/api/demo/export/" + customer);
        if ((int)response.StatusCode != status)
            throw new InvalidOperationException($"{customer}: expected HTTP {status}, got {(int)response.StatusCode}.");
        var content = await response.Content.ReadAsStringAsync();
        if (!content.Contains(body, StringComparison.Ordinal))
            throw new InvalidOperationException($"{customer}: unexpected response {JsonSerializer.Serialize(content)}.");
        Console.WriteLine($"{customer}: HTTP {(int)response.StatusCode}");
    }
}
