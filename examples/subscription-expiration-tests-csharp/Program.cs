using System.Text.Json;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;
await using var database = new LocalDatabase();
await database.CreateAsync();
var clock = new DemoClock();
using var app = new Subscrio.Core.Subscrio(new SubscrioConfig {
    Database = new DatabaseConfig { ConnectionString = database.ConnectionString, DatabaseType = DatabaseType.SqlServer }, Clock = clock
});
await app.InstallSchemaAsync();
// Create the product
await app.Products.CreateProductAsync(new("inspectroute", "InspectRoute"));

// Define the feature
await app.Features.CreateFeatureAsync(new("create-report", "create-report", "toggle", "false"));

// Associate the feature
await app.Products.AssociateFeatureAsync(
    "inspectroute",
    "create-report",
    new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("inspectroute", "inspector", "inspector"));

// Set the plan value
await app.Plans.SetFeatureValueAsync("inspector", "create-report", "true");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new(
    "inspector",
    "inspector-monthly",
    "Monthly",
    "months",
    DurationValue: 1));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "InspectRoute demo customer"));

// Assign the subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement", "customer", "inspector-monthly"));

// Store the exact contract end
var end = new DateTime(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);
await app.Subscriptions.UpdateSubscriptionAsync(
    "agreement",
    new UpdateSubscriptionDto(ExpirationDate: end));
var existingReports = new[] { "inspection-104.pdf" };

// Test the last eligible instant
clock.UtcNow = end.AddMilliseconds(-1);
var canCreateBeforeExpiration = await app.FeatureChecker.IsEnabledForCustomerAsync("customer", "inspectroute", "create-report");
if (!Equals(canCreateBeforeExpiration, true))
    throw new InvalidOperationException("Before expiry" + " did not match the expected result.");
Console.WriteLine("Before expiry" + ": " + JsonSerializer.Serialize(canCreateBeforeExpiration));

// Test equality and the next instant
clock.UtcNow = end;
var canCreateAtExpiration = await app.FeatureChecker.IsEnabledForCustomerAsync("customer", "inspectroute", "create-report");
if (!Equals(canCreateAtExpiration, false))
    throw new InvalidOperationException("At expiry" + " did not match the expected result.");
Console.WriteLine("At expiry" + ": " + JsonSerializer.Serialize(canCreateAtExpiration));
clock.UtcNow = end.AddMilliseconds(1);
var canCreateAfterExpiration = await app.FeatureChecker.IsEnabledForCustomerAsync("customer", "inspectroute", "create-report");
if (!Equals(canCreateAfterExpiration, false))
    throw new InvalidOperationException("After expiry" + " did not match the expected result.");
Console.WriteLine("After expiry" + ": " + JsonSerializer.Serialize(canCreateAfterExpiration));
var retainedReportCount = existingReports.Length;
if (!Equals(retainedReportCount, 1))
    throw new InvalidOperationException("Archived reports retained" + " did not match the expected result.");
Console.WriteLine("Archived reports retained" + ": " + JsonSerializer.Serialize(retainedReportCount));
var configuredSubscriptionValue = await app.FeatureChecker.IsEnabledForSubscriptionAsync("agreement", "create-report");
if (!Equals(configuredSubscriptionValue, true))
    throw new InvalidOperationException("Subscription value alone" + " did not match the expected result.");
Console.WriteLine("Subscription value alone" + ": " + JsonSerializer.Serialize(configuredSubscriptionValue));
Console.WriteLine("PASS: InspectRoute behavior verified.");
public sealed class DemoClock : IClock {
    private DateTime? value;
    public DateTime UtcNow { get => value ?? DateTime.UtcNow; set => this.value = value; }
}
