using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;

bool CanExportUsingPlanName(string planKey) => planKey == "pro";
Console.WriteLine("Before the fix, Pro export is " +
    (CanExportUsingPlanName("pro") ? "allowed." : "denied."));
Console.WriteLine("Before the fix, Enterprise export is " +
    (CanExportUsingPlanName("enterprise") ? "allowed." : "denied."));

await using var database = new LocalDatabase();
await database.CreateAsync();

using var app = new Subscrio.Core.Subscrio(
    new SubscrioConfig
    {
        Database = new DatabaseConfig
        {
            ConnectionString = database.ConnectionString,
            DatabaseType = DatabaseType.SqlServer
        }
    }
);
await app.InstallSchemaAsync();

await app.Products.CreateProductAsync(new("feedbackdesk", "FeedbackDesk"));

await app.Features.CreateFeatureAsync(
    new("csv-export", "CSV export", "toggle", "false")
);

await app.Products.AssociateFeatureAsync("feedbackdesk", "csv-export");

foreach (var plan in new[] { "free", "pro", "enterprise" })
    await app.Plans.CreatePlanAsync(new("feedbackdesk", plan, plan));

await app.Plans.SetFeatureValueAsync("free", "csv-export", "false");
await app.Plans.SetFeatureValueAsync("pro", "csv-export", "true");
await app.Plans.SetFeatureValueAsync("enterprise", "csv-export", "true");

foreach (var plan in new[] { "free", "pro", "enterprise" })
    await app.BillingCycles.CreateBillingCycleAsync(
        new(plan, plan + "-monthly", "Monthly", "months", DurationValue: 1)
    );

foreach (var customer in new[] { "startup-team", "growth-team", "enterprise-team" })
    await app.Customers.CreateCustomerAsync(new(customer));

await app.Subscriptions.CreateSubscriptionAsync(
    new("startup-team-plan", "startup-team", "free-monthly")
);
await app.Subscriptions.CreateSubscriptionAsync(
    new("growth-team-plan", "growth-team", "pro-monthly")
);
await app.Subscriptions.CreateSubscriptionAsync(
    new("enterprise-team-plan", "enterprise-team", "enterprise-monthly")
);

async Task<string> ExportFeedbackCsv(string customerKey)
{
    bool canExport = await app.FeatureChecker.IsEnabledForCustomerAsync(
        customerKey,
        "feedbackdesk",
        "csv-export"
    );

    if (!canExport)
        return "export_not_included";

    return "feedback,votes\nDark mode,12";
}

string freeResult = await ExportFeedbackCsv("startup-team");
string proResult = await ExportFeedbackCsv("growth-team");
string enterpriseResult = await ExportFeedbackCsv("enterprise-team");

string expectedCsv = "feedback,votes\nDark mode,12";

if (freeResult != "export_not_included")
    throw new InvalidOperationException("Free must not include CSV export.");

if (proResult != expectedCsv)
    throw new InvalidOperationException("Pro must receive the feedback CSV.");

if (enterpriseResult != expectedCsv)
    throw new InvalidOperationException("Enterprise must receive the feedback CSV.");

Console.WriteLine("Free customer: export denied, as expected.");
Console.WriteLine("Pro customer: feedback CSV returned, as expected.");
Console.WriteLine("Enterprise customer: feedback CSV returned, as expected.");
Console.WriteLine("PASS: FeedbackDesk export access verified.");
