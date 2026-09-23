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
await app.Products.CreateProductAsync(new("gaugebench", "GaugeBench"));

// Define the feature
await app.Features.CreateFeatureAsync(new(
    "generate-certificates",
    "generate-certificates",
    "toggle",
    "false"));

// Associate the feature
await app.Products.AssociateFeatureAsync("gaugebench", "generate-certificates");

// Define archive access
await app.Features.CreateFeatureAsync(new(
    "read-certificates",
    "Read stored certificates",
    "toggle",
    "false"));

// Associate archive access
await app.Products.AssociateFeatureAsync("gaugebench", "read-certificates");

// Create the two plans
await app.Plans.CreatePlanAsync(new("gaugebench", "trial", "Certificate trial"));
await app.Plans.CreatePlanAsync(new("gaugebench", "read-only", "Read only"));

// Assign the capabilities
await app.Plans.SetFeatureValueAsync("trial", "generate-certificates", "true");
await app.Plans.SetFeatureValueAsync("trial", "read-certificates", "true");
await app.Plans.SetFeatureValueAsync("read-only", "generate-certificates", "false");
await app.Plans.SetFeatureValueAsync("read-only", "read-certificates", "true");

// Create the cycles
await app.BillingCycles.CreateBillingCycleAsync(new(
    "trial",
    "trial-monthly",
    "Monthly",
    "months",
    DurationValue: 1));
await app.BillingCycles.CreateBillingCycleAsync(new(
    "read-only",
    "read-only-ongoing",
    "Ongoing",
    "forever"));

// Choose the expiration destination
await app.Plans.UpdatePlanAsync(
    "trial",
    new UpdatePlanDto(OnExpireTransitionToBillingCycleKey: "read-only-ongoing"));

// Create the laboratory
await app.Customers.CreateCustomerAsync(new("north-lab", "North calibration laboratory"));

// Start the fourteen-day trial
var end = DateTime.UtcNow.AddDays(14);
await app.Subscriptions.CreateSubscriptionAsync(new(
    "lab-trial",
    "north-lab",
    "trial-monthly",
    TrialEndDate: end,
    ExpirationDate: end));
Check(
    "During trial: generate",
    await app.FeatureChecker.IsEnabledForCustomerAsync("north-lab", "gaugebench", "generate-certificates"),
    true);

// Prepare an expired test agreement
var past = DateTime.UtcNow.AddMinutes(-1);
await app.Subscriptions.UpdateSubscriptionAsync(
    "lab-trial",
    new UpdateSubscriptionDto(TrialEndDate: past, ExpirationDate: past));
Check(
    "Expired: generate",
    await app.FeatureChecker.IsEnabledForCustomerAsync("north-lab", "gaugebench", "generate-certificates"),
    false);

// Process the transition
var report = await app.Subscriptions.TransitionExpiredSubscriptionsAsync();
Check("Transitions", report.Transitioned, 1);
Check("Transition errors", report.Errors.Count, 0);
Check(
    "Old agreement archived",
    (await app.Subscriptions.GetSubscriptionAsync("lab-trial"))!.IsArchived,
    true);
Check(
    "Read-only: generate",
    await app.FeatureChecker.IsEnabledForCustomerAsync("north-lab", "gaugebench", "generate-certificates"),
    false);
Check(
    "Read-only: read",
    await app.FeatureChecker.IsEnabledForCustomerAsync("north-lab", "gaugebench", "read-certificates"),
    true);
var again = await app.Subscriptions.TransitionExpiredSubscriptionsAsync();
Check("Second run transitions", again.Transitioned, 0);
Console.WriteLine("PASS: GaugeBench behavior verified.");
static void Check<T>(string label, T actual, T expected) {
    if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + JsonSerializer.Serialize(actual));
}
