using Subscrio.Core.Application.DTOs;
using SubscrioInstance = Subscrio.Core.Subscrio;

namespace ReleaseBoard;

public static class Catalog
{
    public static async Task SeedAsync(SubscrioInstance subscrio)
    {
        await subscrio.Products.CreateProductAsync(new("releaseboard", "ReleaseBoard"));

        await subscrio.Features.CreateFeatureAsync(
            new("download-notes", "Download release notes", "toggle", "false"));

        await subscrio.Products.AssociateFeatureAsync(
            "releaseboard", "download-notes",
            new FeatureResolutionOptions(SubscriptionRule: "most_generous"));

        await subscrio.Plans.CreatePlanAsync(new("releaseboard", "starter", "Starter"));
        await subscrio.Plans.CreatePlanAsync(new("releaseboard", "team", "Team"));

        await subscrio.Plans.SetFeatureValueAsync("starter", "download-notes", "false");
        await subscrio.Plans.SetFeatureValueAsync("team", "download-notes", "true");

        await subscrio.BillingCycles.CreateBillingCycleAsync(
            new("starter", "starter-monthly", "Monthly", "months", DurationValue: 1));
        await subscrio.BillingCycles.CreateBillingCycleAsync(
            new("team", "team-monthly", "Monthly", "months", DurationValue: 1));

        await subscrio.Customers.CreateCustomerAsync(new("starter-customer", "Starter demo customer"));
        await subscrio.Customers.CreateCustomerAsync(new("team-customer", "Team demo customer"));
        await subscrio.Customers.CreateCustomerAsync(new("unassigned-customer", "Unassigned demo customer"));

        await subscrio.Subscriptions.CreateSubscriptionAsync(
            new("starter-agreement", "starter-customer", "starter-monthly"));
        await subscrio.Subscriptions.CreateSubscriptionAsync(
            new("team-agreement", "team-customer", "team-monthly"));
    }
}
