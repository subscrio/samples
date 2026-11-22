using Subscrio.Core.Application.DTOs;
using SubscrioInstance = Subscrio.Core.Subscrio;

namespace FeedbackDesk.Api;

public static class Catalog
{
    public static async Task SeedAsync(SubscrioInstance subscrio)
    {
        if (await subscrio.Products.GetProductAsync("feedbackdesk") is null)
            await subscrio.Products.CreateProductAsync(new CreateProductDto("feedbackdesk", "FeedbackDesk"));

        if (await subscrio.Features.GetFeatureAsync("csv-export") is null)
            await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "csv-export", DisplayName: "CSV export", ValueType: "toggle", DefaultValue: "false"));
        await subscrio.Products.AssociateFeatureAsync("feedbackdesk", "csv-export");

        foreach (var (key, displayName, enabled) in new[]
        {
            ("feedbackdesk-free", "Free", "false"),
            ("feedbackdesk-studio", "Studio", "true")
        })
        {
            if (await subscrio.Plans.GetPlanAsync(key) is null)
                await subscrio.Plans.CreatePlanAsync(new CreatePlanDto("feedbackdesk", key, displayName));
            await subscrio.Plans.SetFeatureValueAsync(key, "csv-export", enabled);
        }

        var cycles = new[]
        {
            ("feedbackdesk-studio", "studio-monthly-2025", "Studio monthly (2025 price)", "months", 1, "price_feedbackdesk_2025"),
            ("feedbackdesk-studio", "studio-monthly-2026", "Studio monthly (2026 price)", "months", 1, "price_feedbackdesk_2026"),
            ("feedbackdesk-free", "free-ongoing", "Free ongoing", "forever", 0, (string?)null)
        };
        foreach (var (plan, key, displayName, unit, duration, priceId) in cycles)
            if (await subscrio.BillingCycles.GetBillingCycleAsync(key) is null)
                await subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                    PlanKey: plan, Key: key, DisplayName: displayName,
                    DurationUnit: unit, DurationValue: duration == 0 ? null : duration,
                    ExternalProductId: priceId));

        foreach (var customer in new[] { "legacy-team", "current-team", "free-team" })
            if (await subscrio.Customers.GetCustomerAsync(customer) is null)
                await subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(customer, customer));

        var subscriptions = new[]
        {
            ("legacy-studio", "legacy-team", "studio-monthly-2025"),
            ("current-studio", "current-team", "studio-monthly-2026"),
            ("free-agreement", "free-team", "free-ongoing")
        };
        foreach (var (key, customer, cycle) in subscriptions)
            if (await subscrio.Subscriptions.GetSubscriptionAsync(key) is null)
                await subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                    Key: key, CustomerKey: customer, BillingCycleKey: cycle));
    }
}
