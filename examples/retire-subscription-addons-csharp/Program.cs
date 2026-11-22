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
await app.Products.CreateProductAsync(new("curtainpass", "CurtainPass"));

// Define the feature
await app.Features.CreateFeatureAsync(new("scanning-lanes", "scanning-lanes", "numeric", "0"));

// Associate the feature
await app.Products.AssociateFeatureAsync(
    "curtainpass",
    "scanning-lanes",
    new FeatureResolutionOptions(AddonRule: "additive", SubscriptionRule: "most_generous"));

// Create the plan
await app.Plans.CreatePlanAsync(new("curtainpass", "venue", "venue"));

// Set the plan value
await app.Plans.SetFeatureValueAsync("venue", "scanning-lanes", "1");

// Create the billing cycle
await app.BillingCycles.CreateBillingCycleAsync(new(
    "venue",
    "venue-monthly",
    "Monthly",
    "months",
    DurationValue: 1));

// Create the customer
await app.Customers.CreateCustomerAsync(new("customer", "CurtainPass demo customer"));

// Assign the subscription
await app.Subscriptions.CreateSubscriptionAsync(new("agreement", "customer", "venue-monthly"));

// Define the legacy package
await app.Addons.CreateAddonAsync(new(
    "legacy-lanes",
    "curtainpass",
    "Legacy scanning lanes",
    FeatureValues: new() { ["scanning-lanes"] = "2" }));

// Record the existing purchase
await app.Subscriptions.AttachAddonAsync("agreement", "legacy-lanes", 2);
var lanesBeforeRetirement = await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "curtainpass", "scanning-lanes", 0);
if (!Equals(lanesBeforeRetirement, 5))
    throw new InvalidOperationException("Existing venue before retirement" + " did not match the expected result.");
Console.WriteLine("Existing venue before retirement" + ": " + JsonSerializer.Serialize(lanesBeforeRetirement));

// Create the new venue
await app.Customers.CreateCustomerAsync(new("new-venue"));

// Assign the new venue base plan
await app.Subscriptions.CreateSubscriptionAsync(new("new-agreement", "new-venue", "venue-monthly"));

// Archive the package
await app.Addons.ArchiveAddonAsync("legacy-lanes");
var lanesAfterRetirement = await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "curtainpass", "scanning-lanes", 0);
if (!Equals(lanesAfterRetirement, 5))
    throw new InvalidOperationException("Existing venue after retirement" + " did not match the expected result.");
Console.WriteLine("Existing venue after retirement" + ": " + JsonSerializer.Serialize(lanesAfterRetirement));
bool newAttachmentRejected = false;
try
{
    await app.Subscriptions.AttachAddonAsync("new-agreement", "legacy-lanes");
}
catch (ValidationException)
{
    newAttachmentRejected = true;
}
if (!newAttachmentRejected)
    throw new InvalidOperationException("New purchase rejected was expected.");
Console.WriteLine("New purchase rejected: true");
var newVenueLanes = await app.FeatureChecker.GetValueForCustomerAsync<int>("new-venue", "curtainpass", "scanning-lanes", 0);
if (!Equals(newVenueLanes, 1))
    throw new InvalidOperationException("New venue allowance" + " did not match the expected result.");
Console.WriteLine("New venue allowance" + ": " + JsonSerializer.Serialize(newVenueLanes));

// Inspect the effect of an explicit detachment
await app.Subscriptions.DetachAddonAsync("agreement", "legacy-lanes");
var lanesAfterDetachment = await app.FeatureChecker.GetValueForCustomerAsync<int>("customer", "curtainpass", "scanning-lanes", 0);
if (!Equals(lanesAfterDetachment, 1))
    throw new InvalidOperationException("After explicit detachment" + " did not match the expected result.");
Console.WriteLine("After explicit detachment" + ": " + JsonSerializer.Serialize(lanesAfterDetachment));
bool deletionRejected = false;
try
{
    await app.Addons.DeleteAddonAsync("legacy-lanes");
}
catch (ConflictException)
{
    deletionRejected = true;
}
if (!deletionRejected)
    throw new InvalidOperationException("Delete with history rejected was expected.");
Console.WriteLine("Delete with history rejected: true");
Console.WriteLine("PASS: CurtainPass behavior verified.");
