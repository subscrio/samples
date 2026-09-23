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
await app.Products.CreateProductAsync(new("badgeharbor", "BadgeHarbor"));

// Define the entitlement
await app.Features.CreateFeatureAsync(new("desks", "desks", "numeric", "0"));

// Associate the feature
await app.Products.AssociateFeatureAsync("badgeharbor",
    "desks",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("badgeharbor", "standard", "Venue"));

// Set the included value
await app.Plans.SetFeatureValueAsync("standard", "desks", "12");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new("standard",
    "standard-cycle",
    "Monthly",
    "months",
    DurationValue: 1));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "BadgeHarbor customer"));

// Give the customer a subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement",
    "customer",
    "standard-cycle",
    ActivationDate: clock.UtcNow));

// Create the desk pack
await app.Addons.CreateAddonAsync(new("desk-pack",
    "badgeharbor",
    "Eight desks",
    FeatureValues: new() { ["desks"] = "8" }));

// Attach the desk pack
await app.Subscriptions.AttachAddonAsync("agreement", "desk-pack");
Check("Normal desks",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer",
    "badgeharbor",
    "desks",
    0),
    20);

// Grant the opening-weekend capacity
var closesAt = clock.UtcNow.AddDays(3);
await app.Subscriptions.AddFeatureOverrideAsync("agreement", "desks", "80", OverrideType.Timed, closesAt);
Check("Festival desks",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer",
    "badgeharbor",
    "desks",
    0),
    80);

// Check the exact expiry boundary
clock.UtcNow = closesAt.AddMilliseconds(-1);
Check("Desks before closing",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer",
    "badgeharbor",
    "desks",
    0),
    80);
clock.UtcNow = closesAt;
Check("Desks at closing",
    await app.FeatureChecker.GetValueForCustomerAsync<int>("customer",
    "badgeharbor",
    "desks",
    0),
    20);

// Inspect the retained override
var agreement = await app.Subscriptions.GetSubscriptionAsync("agreement");
Check("Retained override records", agreement!.FeatureOverrides.Count, 1);
Console.WriteLine("PASS: BadgeHarbor behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual,
        expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
public sealed class DemoClock : IClock {
    private DateTime? value;
    public DateTime UtcNow { get => value ?? DateTime.UtcNow; set => this.value = value; }
}
