using Subscrio.Core.Application.DTOs;
using SubscrioInstance = Subscrio.Core.Subscrio;

namespace KilnBook;

public static class Catalog
{
    public static async Task SeedAsync(SubscrioInstance subscrio)
    {
        if (await subscrio.Products.GetProductAsync("kilnbook") is null)
            await subscrio.Products.CreateProductAsync(new CreateProductDto("kilnbook", "KilnBook"));

        if (await subscrio.Features.GetFeatureAsync("recurring-bookings") is null)
            await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "recurring-bookings", DisplayName: "Recurring bookings",
                ValueType: "toggle", DefaultValue: "false"));
        if (await subscrio.Features.GetFeatureAsync("active-reservations") is null)
            await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
                Key: "active-reservations", DisplayName: "Active reservations",
                ValueType: "numeric", DefaultValue: "0"));

        await subscrio.Products.AssociateFeatureAsync("kilnbook", "recurring-bookings");
        await subscrio.Products.AssociateFeatureAsync("kilnbook", "active-reservations");

        foreach (var (planKey, name, limit, recurring) in new[]
        {
            ("kilnbook-basic", "Basic", "2", "false"),
            ("kilnbook-studio", "Studio", "8", "true")
        })
        {
            if (await subscrio.Plans.GetPlanAsync(planKey) is null)
                await subscrio.Plans.CreatePlanAsync(new CreatePlanDto("kilnbook", planKey, name));
            await subscrio.Plans.SetFeatureValueAsync(planKey, "active-reservations", limit);
            await subscrio.Plans.SetFeatureValueAsync(planKey, "recurring-bookings", recurring);
            var cycleKey = $"{planKey}-monthly";
            if (await subscrio.BillingCycles.GetBillingCycleAsync(cycleKey) is null)
                await subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
                    PlanKey: planKey, Key: cycleKey, DisplayName: $"{name} monthly",
                    DurationUnit: "months", DurationValue: 1));
        }

        foreach (var customerKey in new[] { "clay-room", "river-studio", "visitor" })
            if (await subscrio.Customers.GetCustomerAsync(customerKey) is null)
                await subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(customerKey));

        foreach (var (customerKey, cycleKey) in new[]
        {
            ("clay-room", "kilnbook-basic-monthly"),
            ("river-studio", "kilnbook-studio-monthly")
        })
        {
            var subscriptionKey = $"{customerKey}-agreement";
            if (await subscrio.Subscriptions.GetSubscriptionAsync(subscriptionKey) is null)
                await subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
                    Key: subscriptionKey, CustomerKey: customerKey, BillingCycleKey: cycleKey));
        }
    }
}
