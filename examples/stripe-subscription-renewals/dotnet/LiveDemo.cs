using Microsoft.Data.SqlClient;
using Stripe;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;

public static class LiveDemo
{
    public static async Task RunAsync()
    {
        static string Required(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Set {name}. See the sample README.");
        var key = Required("STRIPE_SECRET_KEY");
        if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^[sr]k_test_"))
            throw new InvalidOperationException("Use a sandbox secret key.");
        var secret = Required("STRIPE_WEBHOOK_SECRET");
        var priceId = Required("STRIPE_PRICE_ID");
        var retrieve = RenewalWorker.StripeRetriever(new StripeClient(key));
        var current = await retrieve(Required("STRIPE_SUBSCRIPTION_ID"));
        var item = current.Items.Data.Find(i => i.Price.Id == priceId);
        if (current.Livemode || item?.Price.Recurring?.Interval != "month" || item.Price.Recurring.IntervalCount != 1)
            throw new InvalidOperationException("Select a sandbox subscription with the monthly price.");
        await using var database = new LocalDatabase();
        await database.CreateAsync();
        using var app = new Subscrio.Core.Subscrio(new SubscrioConfig
        {
            Database = new DatabaseConfig { DatabaseType = DatabaseType.SqlServer, ConnectionString = database.ConnectionString }
        });
        await app.InstallSchemaAsync();
        var worker = new RenewalWorker(app, database.ConnectionString, retrieve, secret, current.Id, priceId, current.CustomerId);
        await worker.InstallAsync();
        await app.Products.CreateProductAsync(new("mooringdesk", "MooringDesk"));
        await app.Features.CreateFeatureAsync(new("work-orders", "Create work orders", "toggle", "false"));
        await app.Products.AssociateFeatureAsync("mooringdesk", "work-orders", new(SubscriptionRule: "most_generous"));
        await app.Plans.CreatePlanAsync(new("mooringdesk", "marina", "Marina"));
        await app.Plans.SetFeatureValueAsync("marina", "work-orders", "true");
        await app.BillingCycles.CreateBillingCycleAsync(new("marina", "monthly", "Monthly", "months", DurationValue: 1, ExternalProductId: priceId));
        await app.Customers.CreateCustomerAsync(new("customer", "MooringDesk customer", ExternalBillingId: current.CustomerId));
        await app.Subscriptions.CreateSubscriptionAsync(new("agreement", "customer", "monthly", ActivationDate: current.Created,
            CurrentPeriodStart: item.CurrentPeriodStart, CurrentPeriodEnd: item.CurrentPeriodEnd, StripeSubscriptionId: current.Id));
        await using (var db = new SqlConnection(database.ConnectionString))
        {
            await db.OpenAsync();
            await using var command = db.CreateCommand();
            command.CommandText = "INSERT INTO renewal_access VALUES(@id,@allowed)";
            command.Parameters.AddWithValue("@id", current.Id);
            command.Parameters.AddWithValue("@allowed", current.Status == "active" && current.LatestInvoice?.Status == "paid");
            await command.ExecuteNonQueryAsync();
        }
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        await using var web = builder.Build();
        web.Urls.Add("http://127.0.0.1:4242");
        web.MapPost("/stripe/webhook", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body);
            try
            {
                var result = await worker.ReceiveAsync(await reader.ReadToEndAsync(), request.Headers["Stripe-Signature"].ToString());
                Console.WriteLine($"Webhook: {result}");
                return Results.Text(result);
            }
            catch (WebhookSignatureException) { return Results.StatusCode(400); }
            catch { Console.WriteLine("Webhook processing failed"); return Results.StatusCode(500); }
        });
        web.MapGet("/status", async () =>
        {
            var agreement = await app.Subscriptions.GetSubscriptionAsync("agreement");
            return Results.Json(new { subscriptionKey = agreement!.Key, stripeSubscriptionId = agreement.StripeSubscriptionId,
                currentPeriodEnd = agreement.CurrentPeriodEnd, canCreateWorkOrder = await worker.CanCreateWorkOrderAsync(),
                subscriptionCount = (await app.Subscriptions.GetSubscriptionsByCustomerAsync("customer")).Count });
        });
        Console.WriteLine("MooringDesk listening: http://127.0.0.1:4242/status");
        _ = Task.Run(async () => { if (await Console.In.ReadLineAsync() == "stop") await web.StopAsync(); });
        await web.RunAsync();
    }
}
