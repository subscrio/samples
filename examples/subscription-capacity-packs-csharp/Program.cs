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
// Create the product
await app.Products.CreateProductAsync(new("dockflow", "DockFlow"));

// Define the feature
await app.Features.CreateFeatureAsync(new("dispatch-docks", "dispatch-docks", "numeric", "0"));

// Associate the feature
await app.Products.AssociateFeatureAsync(
    "dockflow",
    "dispatch-docks",
    new FeatureResolutionOptions(AddonRule: "additive", SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("dockflow", "warehouse", "warehouse"));

// Set the plan value
await app.Plans.SetFeatureValueAsync("warehouse", "dispatch-docks", "4");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new(
    "warehouse",
    "warehouse-monthly",
    "Monthly",
    "months",
    DurationValue: 1));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "DockFlow demo customer"));

// Assign the subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement", "customer", "warehouse-monthly"));

// Define the two-dock pack
await app.Addons.CreateAddonAsync(new(
    "two-dock-pack",
    "dockflow",
    "Two dispatch docks",
    FeatureValues: new() { ["dispatch-docks"] = "2" }));
Check(
    "Base docks",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "dockflow", "dispatch-docks", 0),
    4);

// Set the purchased quantity to three
await app.Subscriptions.AttachAddonAsync("agreement", "two-dock-pack", 3);
Check(
    "Three packs",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "dockflow", "dispatch-docks", 0),
    10);

// Replace three with one
await app.Subscriptions.AttachAddonAsync("agreement", "two-dock-pack", 1);
Check(
    "One pack",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "dockflow", "dispatch-docks", 0),
    6);
await app.Subscriptions.AttachAddonAsync("agreement", "two-dock-pack", 1);
Check(
    "One pack repeated",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "dockflow", "dispatch-docks", 0),
    6);

// Remove the contribution
await app.Subscriptions.DetachAddonAsync("agreement", "two-dock-pack");
Check(
    "Detached",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "dockflow", "dispatch-docks", 0),
    4);
Console.WriteLine("PASS: DockFlow behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
