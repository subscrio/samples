using System.Text.Json;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;
await using var database = new LocalDatabase();
await database.CreateAsync();
var clock = new DemoClock();
using var app = new Subscrio.Core.Subscrio(new SubscrioConfig {
    Database = new DatabaseConfig { ConnectionString = database.ConnectionString,
        DatabaseType = DatabaseType.SqlServer },
        Clock = clock
});
await app.InstallSchemaAsync();
// Create the product
await app.Products.CreateProductAsync(new("threaddraft", "ThreadDraft"));

// Define the entitlement
await app.Features.CreateFeatureAsync(new("app-access", "app-access", "toggle", "false"));

// Associate the feature
await app.Products.AssociateFeatureAsync("threaddraft",
    "app-access",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("threaddraft", "standard", "Complete app"));

// Set the included value
await app.Plans.SetFeatureValueAsync("standard", "app-access", "true");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new("standard", "standard-cycle", "Ongoing", "forever"));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "ThreadDraft customer"));

// Fulfill a confirmed purchase
async Task Fulfill(string paidOrderKey) {
    var key = "purchase-" + paidOrderKey;
    if (await app.Subscriptions.GetSubscriptionAsync(key) != null) return;
    try { await app.Subscriptions.CreateSubscriptionAsync(new(key, "customer", "standard-cycle")); }
    catch (ConflictException) { if (await app.Subscriptions.GetSubscriptionAsync(key) == null) throw; }
}
await Fulfill("order-thread-101");
await Fulfill("order-thread-101");
Check("Purchase replay keeps agreement",
    (await app.Subscriptions.GetSubscriptionAsync("purchase-order-thread-101"))!.Key,
    "purchase-order-thread-101");
Check("Whole-app agreements", (await app.Subscriptions.GetSubscriptionsByCustomerAsync("customer")).Count, 1);

// Protect every whole-app tool
await app.Customers.CreateCustomerAsync(new("visitor", "Customer without a purchase"));
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
var web = builder.Build();
foreach (var tool in new[] { "design", "edit", "export" }) {
    var action = tool;
    web.MapPost("/customers/{customer}/" + action, async (string customer) =>
        await app.FeatureChecker.GetValueForCustomerAsync<bool>(customer, "threaddraft", "app-access", false)
            ? Results.Ok(new { tool = action, status = "available" })
            : Results.Json(new { error = "app_purchase_required" }, statusCode: 403));
}
web.Urls.Add("http://127.0.0.1:0");
await web.StartAsync();
try {
    using var http = new HttpClient { BaseAddress = new Uri(web.Urls.Single()) };
    foreach (var tool in new[] { "design", "edit", "export" }) {
        using var paid = await http.PostAsync($"/customers/customer/{tool}", null);
        using var unpaid = await http.PostAsync($"/customers/visitor/{tool}", null);
        Check(tool + " purchaser HTTP", (int)paid.StatusCode, 200);
        Check(tool + " visitor HTTP", (int)unpaid.StatusCode, 403);
    }
} finally { await web.StopAsync(); await web.DisposeAsync(); }

// Check a later visit
clock.UtcNow = DateTime.UtcNow.AddYears(1);
Check("Access on a later visit",
    await app.FeatureChecker.GetValueForCustomerAsync<bool>("customer",
    "threaddraft",
    "app-access",
    false),
    true);
Console.WriteLine("PASS: ThreadDraft behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual,
        expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
public sealed class DemoClock : IClock {
    private DateTime? value;
    public DateTime UtcNow { get => value ?? DateTime.UtcNow; set => this.value = value; }
}
