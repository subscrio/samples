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
await app.Products.CreateProductAsync(new("folioworks", "FolioWorks"));

// Define the entitlement
await app.Features.CreateFeatureAsync(new("ocr", "ocr", "toggle", "false"));

// Associate the feature
await app.Products.AssociateFeatureAsync("folioworks",
    "ocr",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("folioworks", "standard", "Archive"));

// Set the included value
await app.Plans.SetFeatureValueAsync("standard", "ocr", "true");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new("standard",
    "standard-cycle",
    "Monthly",
    "months",
    DurationValue: 1));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "FolioWorks customer"));

// Define photograph restoration
await app.Features.CreateFeatureAsync(new("restore-photo", "Restore photograph", "toggle", "false"));

// Associate restoration with FolioWorks
await app.Products.AssociateFeatureAsync("folioworks",
    "restore-photo",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Include restoration in the plan
await app.Plans.SetFeatureValueAsync("standard", "restore-photo", "true");

// Define the credit currency
await app.Credits.CreateCurrencyAsync(new("archive-credits", "Archive credits"));

// Set the monthly grant
await app.Credits.SetPlanGrantAsync("standard", "archive-credits", new(1200, "monthly"));

// Price the two operations
await app.Credits.SetConsumptionRuleAsync("ocr", "archive-credits", 2);
await app.Credits.SetConsumptionRuleAsync("restore-photo", "archive-credits", 8);

// Give the customer a subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement",
    "customer",
    "standard-cycle",
    ActivationDate: clock.UtcNow));

// Issue the due grant
await app.Credits.ProcessScheduledGrantsAsync("customer");
Check("Monthly credits", (await app.Credits.GetBalanceAsync("customer", "archive-credits")).Available, 1200L);
var repeated = await app.Credits.ProcessScheduledGrantsAsync("customer");
Check("Repeated grants", repeated.Issued, 0);

// Spend credits on archive work
async Task Process(string feature, long units, string jobKey) {
    if (!await app.FeatureChecker.GetValueForCustomerAsync<bool>("customer", "folioworks", feature, false))
        throw new InvalidOperationException("Feature is not included");
    await app.Credits.ConsumeAsync(new("customer", feature, units, jobKey));
}
await Process("ocr", 10, "collection-ocr");
await Process("restore-photo", 2, "collection-photos");
Check("Credits after archive work",
    (await app.Credits.GetBalanceAsync("customer",
    "archive-credits")).Available,
    1164L);

// Keep the entitlement check when credits remain
await app.Plans.SetFeatureValueAsync("standard", "restore-photo", "false");
await ExpectError<InvalidOperationException>(() => Process("restore-photo",
    1,
    "unavailable-photo"),
    "Restoration denied");
Check("Unspent credits preserved",
    (await app.Credits.GetBalanceAsync("customer",
    "archive-credits")).Available,
    1164L);

// Issue the next monthly allocation
clock.UtcNow = DateTime.UtcNow.AddMonths(1).AddSeconds(1);
await app.Credits.ProcessScheduledGrantsAsync("customer");
Check("Credits after next monthly grant",
    (await app.Credits.GetBalanceAsync("customer",
    "archive-credits")).Available,
    2364L);
Console.WriteLine("PASS: FolioWorks behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual,
        expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
static async Task ExpectError<T>(Func<Task> operation, string label) where T : Exception {
    try { await operation(); } catch (T) { Console.WriteLine(label + ": true"); return; }
    throw new InvalidOperationException(label + " did not fail");
}
public sealed class DemoClock : IClock {
    private DateTime? value;
    public DateTime UtcNow { get => value ?? DateTime.UtcNow; set => this.value = value; }
}
