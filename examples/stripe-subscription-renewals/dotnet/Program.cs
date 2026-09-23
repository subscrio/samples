using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Subscrio.Core.Config;
using Subscrio.Core.Domain.ValueObjects;
using Stripe;

var fixtures = JsonNode.Parse(await System.IO.File.ReadAllTextAsync("../fixtures.json"))!;
var currentJson = fixtures["initial"]!.ToJsonString();
Subscription Current() => StripeEntity.FromJson<Subscription>(currentJson);
await using var database = new LocalDatabase(); await database.CreateAsync();
using var app = new Subscrio.Core.Subscrio(new SubscrioConfig
{
    Database = new DatabaseConfig { DatabaseType = DatabaseType.SqlServer, ConnectionString = database.ConnectionString }
});
await app.InstallSchemaAsync();
const string secret = "whsec_local_fixture_only";
var worker = new RenewalWorker(app, database.ConnectionString, _ => Task.FromResult(Current()), secret, Current().Id);
await worker.InstallAsync();
await app.Products.CreateProductAsync(new("mooringdesk", "MooringDesk"));
await app.Features.CreateFeatureAsync(new("work-orders", "Create work orders", "toggle", "false"));
await app.Products.AssociateFeatureAsync("mooringdesk", "work-orders", new(SubscriptionRule: "most_generous"));
await app.Plans.CreatePlanAsync(new("mooringdesk", "marina", "Marina"));
await app.Plans.SetFeatureValueAsync("marina", "work-orders", "true");
await app.BillingCycles.CreateBillingCycleAsync(new("marina", "monthly", "Monthly", "months", DurationValue: 1, ExternalProductId: "price_mooring"));
await app.Customers.CreateCustomerAsync(new("customer", "MooringDesk customer", ExternalBillingId: "cus_mooring"));
await app.Subscriptions.CreateSubscriptionAsync(new("agreement", "customer", "monthly", ActivationDate: Current().Created,
    CurrentPeriodStart: Current().Items.Data[0].CurrentPeriodStart, CurrentPeriodEnd: Current().Items.Data[0].CurrentPeriodEnd, StripeSubscriptionId: Current().Id));

var builder = WebApplication.CreateBuilder(args); builder.Logging.ClearProviders();
var web = builder.Build(); web.Urls.Add("http://127.0.0.1:0");
web.MapPost("/stripe/webhook", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();
    try { return Results.Text(await worker.ReceiveAsync(rawBody, request.Headers["Stripe-Signature"].ToString())); }
    catch (WebhookSignatureException) { return Results.StatusCode(400); }
    catch { return Results.StatusCode(500); }
});
await web.StartAsync();
try
{
    using var http = new HttpClient { BaseAddress = new Uri(web.Urls.Single()) };
    async Task<(int Status, string Body)> Deliver(JsonNode stripeEvent, bool valid = true)
    {
        var body = stripeEvent.ToJsonString();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/stripe/webhook") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("Stripe-Signature", Sign(body, valid ? secret : "wrong-secret"));
        using var response = await http.SendAsync(request);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }
    var first = fixtures["scenarios"]![0]!;
    Check("Invalid signature HTTP", (await Deliver(first["event"]!, false)).Status, 400);
    await using (var db = new SqlConnection(database.ConnectionString))
    {
        await db.OpenAsync(); await using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM renewal_receipts";
        Check("Receipts after invalid signature", (int)(await cmd.ExecuteScalarAsync())!, 0);
    }
    foreach (var scenario in fixtures["scenarios"]!.AsArray())
    {
        currentJson = scenario!["current"]!.ToJsonString(); var label = scenario["label"]!.GetValue<string>();
        Check(label + " HTTP", (await Deliver(scenario["event"]!)).Status, 200);
        Check(label + " access", await worker.CanCreateWorkOrderAsync(), scenario["allowed"]!.GetValue<bool>());
        var agreement = await app.Subscriptions.GetSubscriptionAsync("agreement");
        Check(label + " period end", DateTime.Parse(agreement!.CurrentPeriodEnd!, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal).ToString("yyyy-MM-ddTHH:mm:ssZ"), Current().Items.Data[0].CurrentPeriodEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        Check(label + " replay", (await Deliver(scenario["event"]!)).Body, "duplicate");
    }
    Check("Linked agreement count", (await app.Subscriptions.GetSubscriptionsByCustomerAsync("customer")).Count, 1);
    var restarted = new RenewalWorker(app, database.ConnectionString, _ => Task.FromResult(Current()), secret, Current().Id);
    var replay = first["event"]!.ToJsonString();
    Check("Replay after worker restart", await restarted.ReceiveAsync(replay, Sign(replay, secret)), "duplicate");
    var delayed = first["event"]!.DeepClone(); delayed["id"] = "evt_delayed";
    Check("Delayed event HTTP", (await Deliver(delayed)).Status, 200);
    Check("Delayed event preserves cancellation", await worker.CanCreateWorkOrderAsync(), false);
    foreach (var problem in new[] { "price", "customer" })
    {
        var bad = first["current"]!.DeepClone();
        if (problem == "price") bad["items"]!["data"]![0]!["price"]!["id"] = "price_unknown";
        else bad["customer"] = "cus_unknown";
        currentJson = bad.ToJsonString();
        var retry = first["event"]!.DeepClone(); retry["id"] = "evt_retry_" + problem;
        Check(problem + " failure HTTP", (await Deliver(retry)).Status, 500);
        currentJson = first["current"]!.ToJsonString();
        Check(problem + " repair HTTP", (await Deliver(retry)).Status, 200);
    }
    Check("Recovery after processing repair", await worker.CanCreateWorkOrderAsync(), true);
    Console.WriteLine("PASS: MooringDesk renewal behavior verified.");
}
finally { await web.StopAsync(); await web.DisposeAsync(); }

static string Sign(string body, string secret)
{
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(timestamp + "." + body));
    return $"t={timestamp},v1={Convert.ToHexString(digest).ToLowerInvariant()}";
}
static void Check<T>(string label, T actual, T expected)
{
    if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    Console.WriteLine(label + ": " + System.Text.Json.JsonSerializer.Serialize(actual));
}
