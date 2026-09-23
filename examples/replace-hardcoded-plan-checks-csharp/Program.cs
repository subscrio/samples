using System.Text.Json;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;
await using var database = new LocalDatabase();
await database.CreateAsync();

using var app = new Subscrio.Core.Subscrio(new SubscrioConfig {
    Database = new DatabaseConfig { ConnectionString = database.ConnectionString, DatabaseType = DatabaseType.SqlServer }
});
await app.InstallSchemaAsync();
// Capture the old failure
bool OldExport(string planKey) => planKey == "professional";
Check("Old professional", OldExport("professional"), true);
Check("Old institutional", OldExport("institutional"), false);

// Create the product
await app.Products.CreateProductAsync(new("seedshelf", "SeedShelf"));

// Define the feature
await app.Features.CreateFeatureAsync(new("csv-export", "csv-export", "toggle", "false"));

// Associate the feature
await app.Products.AssociateFeatureAsync("seedshelf", "csv-export");

// Create the offerings
foreach (var plan in new[] { "community", "professional", "institutional" })
    await app.Plans.CreatePlanAsync(new("seedshelf", plan, plan));

// Assign export access
await app.Plans.SetFeatureValueAsync("community", "csv-export", "false");
await app.Plans.SetFeatureValueAsync("professional", "csv-export", "true");
await app.Plans.SetFeatureValueAsync("institutional", "csv-export", "true");

// Create the billing cycles
foreach (var plan in new[] { "community", "professional", "institutional" })
    await app.BillingCycles.CreateBillingCycleAsync(new(
        plan,
        plan + "-monthly",
        "Monthly",
        "months",
        DurationValue: 1));

// Create the libraries
foreach (var customer in new[] { "village", "city", "consortium" })
    await app.Customers.CreateCustomerAsync(new(customer));

// Assign the subscriptions
await app.Subscriptions.CreateSubscriptionAsync(new("village-plan", "village", "community-monthly"));
await app.Subscriptions.CreateSubscriptionAsync(new("city-plan", "city", "professional-monthly"));
await app.Subscriptions.CreateSubscriptionAsync(new(
    "consortium-plan",
    "consortium",
    "institutional-monthly"));

// Replace the condition at export
async Task<string> Export(string customerKey) {
    if (!await app.FeatureChecker.IsEnabledForCustomerAsync(customerKey, "seedshelf", "csv-export"))
        return "export_not_included";
    return "seed,quantity\nBean,12";
}
Check("Community export", await Export("village"), "export_not_included");
Check("Professional export", await Export("city"), "seed,quantity\nBean,12");
Check("Institutional export", await Export("consortium"), "seed,quantity\nBean,12");
Console.WriteLine("PASS: SeedShelf behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
