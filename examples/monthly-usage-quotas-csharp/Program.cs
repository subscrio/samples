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
await app.Products.CreateProductAsync(new("beamcheck", "BeamCheck"));

// Define the entitlement
await app.Features.CreateFeatureAsync(new("validations",
    "validations",
    "metered",
    "0",
    MeteredConfig: new("monthly",
    "hard",
    "count",
    "customer")));

// Associate the feature
await app.Products.AssociateFeatureAsync("beamcheck",
    "validations",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("beamcheck", "standard", "Studio"));

// Set the included value
await app.Plans.SetFeatureValueAsync("standard", "validations", "250");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new("standard",
    "standard-cycle",
    "Monthly",
    "months",
    DurationValue: 1));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "BeamCheck customer"));

// Give the customer a subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement",
    "customer",
    "standard-cycle",
    ActivationDate: clock.UtcNow));

// Read the monthly allowance
var initial = await app.Metering.GetUsageAsync("customer", "beamcheck", "validations");
Check("Monthly limit", initial.Limit, 250L);
Check("Initially consumed", initial.Consumed, 0L);
Check("Initially remaining", initial.Remaining, 250L);

// Record the accepted validations
for (var job = 1; job <= 250; job++)
    await app.Metering.ReportUsageAsync("customer", "beamcheck", "validations", 1,
        new UsageReportOptions($"validation-{job}"));
var full = await app.Metering.GetUsageAsync("customer", "beamcheck", "validations");
Check("Accepted validations", full.Consumed, 250L);
Check("Remaining validations", full.Remaining, 0L);

// Retry a recorded job
await app.Metering.ReportUsageAsync("customer", "beamcheck", "validations", 1,
    new UsageReportOptions("validation-250"));
Check("Consumed after retry",
    (await app.Metering.GetUsageAsync("customer",
    "beamcheck",
    "validations")).Consumed,
    250L);

// Reject the next validation
await ExpectError<UsageLimitExceededException>(
    () => app.Metering.ReportUsageAsync("customer",
        "beamcheck",
        "validations",
        1,
        new UsageReportOptions("validation-251")),
    "Next validation rejected");

// Start the next calendar month
var now = clock.UtcNow;
clock.UtcNow = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
var nextMonth = await app.Metering.GetUsageAsync("customer", "beamcheck", "validations");
Check("Next month consumed", nextMonth.Consumed, 0L);
Check("Next month remaining", nextMonth.Remaining, 250L);
Console.WriteLine("PASS: BeamCheck behavior verified.");
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
