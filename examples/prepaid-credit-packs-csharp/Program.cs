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
await app.Products.CreateProductAsync(new("slicefoundry", "SliceFoundry"));

// Define the entitlement
await app.Features.CreateFeatureAsync(new("slice", "slice", "toggle", "false"));

// Associate the feature
await app.Products.AssociateFeatureAsync("slicefoundry",
    "slice",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("slicefoundry", "standard", "Application access"));

// Set the included value
await app.Plans.SetFeatureValueAsync("standard", "slice", "true");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new("standard", "standard-cycle", "Ongoing", "forever"));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "SliceFoundry customer"));

// Give the customer a subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement",
    "customer",
    "standard-cycle",
    ActivationDate: clock.UtcNow));

// Define slicing credits
await app.Credits.CreateCurrencyAsync(new("slice-credits", "Slicing credits"));

// Set the cost per job
await app.Credits.SetConsumptionRuleAsync("slice", "slice-credits", 5);

// Fulfill the paid pack
var pack = new CreditGrantInput("customer", "slice-credits", 500, "prepaid", "paid-order-500");
await app.Credits.GrantAsync(pack);
await app.Credits.GrantAsync(pack);
Check("Credits after payment replay",
    (await app.Credits.GetBalanceAsync("customer",
    "slice-credits")).Available,
    500L);

// Charge one slicing job
if (!await app.FeatureChecker.GetValueForCustomerAsync<bool>("customer", "slicefoundry", "slice", false))
    throw new InvalidOperationException("Slicing is not included");
var slice = new CreditConsumeInput("customer", "slice", 1, "slice-model-bracket");
await app.Credits.ConsumeAsync(slice);
Check("Credits after slicing", (await app.Credits.GetBalanceAsync("customer", "slice-credits")).Available, 495L);

// Retry the job without charging twice
await app.Credits.ConsumeAsync(slice);
Check("Credits after job retry",
    (await app.Credits.GetBalanceAsync("customer",
    "slice-credits")).Available,
    495L);
Console.WriteLine("PASS: SliceFoundry behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual,
        expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
public sealed class DemoClock : IClock {
    private DateTime? value;
    public DateTime UtcNow { get => value ?? DateTime.UtcNow; set => this.value = value; }
}
