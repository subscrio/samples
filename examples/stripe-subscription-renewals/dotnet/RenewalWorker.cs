using Microsoft.Data.SqlClient;
using Stripe;

public sealed class RenewalWorker(Subscrio.Core.Subscrio app, string connection,
    Func<string, Task<Subscription>> retrieve, string secret, string subscriptionId,
    string priceId = "price_mooring", string customerId = "cus_mooring")
{
    // One serialized worker. Multiple processes need a distributed lock per
    // subscription covering retrieval, Subscrio updates, and receipt commit.
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task InstallAsync()
    {
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE renewal_receipts(event_id nvarchar(255) PRIMARY KEY); " +
            "CREATE TABLE renewal_access(subscription_id nvarchar(255) PRIMARY KEY, allowed bit NOT NULL)";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string> ReceiveAsync(string rawBody, string signature)
    {
        Event stripeEvent;
        try { stripeEvent = EventUtility.ConstructEvent(rawBody, signature, secret); }
        catch (StripeException error) { throw new WebhookSignatureException(error); }
        await gate.WaitAsync();
        try { return await ProcessAsync(stripeEvent); }
        finally { gate.Release(); }
    }

    private async Task<string> ProcessAsync(Event stripeEvent)
    {
        if (stripeEvent.Type is not ("invoice.payment_succeeded" or "invoice.payment_failed" or
            "customer.subscription.updated" or "customer.subscription.deleted")) return "ignored";
        var id = stripeEvent.Data.Object switch
        {
            Subscription sub => sub.Id,
            Invoice invoice => invoice.Parent?.SubscriptionDetails?.SubscriptionId,
            _ => null
        };
        if (id != subscriptionId) return "ignored";
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        await using var receipt = db.CreateCommand();
        receipt.CommandText = "SELECT COUNT(*) FROM renewal_receipts WHERE event_id=@id";
        receipt.Parameters.AddWithValue("@id", stripeEvent.Id);
        if ((int)(await receipt.ExecuteScalarAsync())! > 0) return "duplicate";

        var current = await retrieve(id);
        if (current.Id != id) throw new InvalidOperationException("Subscription retrieval mismatch");
        var item = current.Items.Data.Find(i => i.Price.Id == priceId)
            ?? throw new InvalidOperationException("Unmapped Stripe price");
        if (current.CustomerId != customerId) throw new InvalidOperationException("Missing customer link");
        var latestInvoice = current.LatestInvoice
            ?? throw new InvalidOperationException("Expand latest_invoice when retrieving");

        if (stripeEvent.Type == "invoice.payment_succeeded" && stripeEvent.Data.Object is Invoice paid &&
            paid.Lines.Data.Any(l => l.Pricing?.PriceDetails?.PriceId == item.Price.Id && l.Period.End == item.CurrentPeriodEnd))
            await app.Stripe.ProcessStripeEventAsync(stripeEvent);
        await app.Stripe.ProcessStripeEventAsync(new Event
        {
            Id = stripeEvent.Id, Type = "customer.subscription.updated",
            Data = new EventData { Object = current }
        });

        // Explicit MooringDesk policy; invoice.payment_failed is not handled by core.
        var allowed = current.Status == "active" && latestInvoice.Status == "paid";
        using var transaction = db.BeginTransaction();
        await using var save = db.CreateCommand(); save.Transaction = transaction;
        save.CommandText = "UPDATE renewal_access SET allowed=@allowed WHERE subscription_id=@sub; " +
            "IF @@ROWCOUNT=0 INSERT INTO renewal_access VALUES(@sub,@allowed); " +
            "INSERT INTO renewal_receipts VALUES(@event)";
        save.Parameters.AddWithValue("@allowed", allowed);
        save.Parameters.AddWithValue("@sub", id);
        save.Parameters.AddWithValue("@event", stripeEvent.Id);
        await save.ExecuteNonQueryAsync(); await transaction.CommitAsync();
        return "processed";
    }

    public async Task<bool> CanCreateWorkOrderAsync()
    {
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT allowed FROM renewal_access WHERE subscription_id=@id";
        cmd.Parameters.AddWithValue("@id", subscriptionId);
        return await cmd.ExecuteScalarAsync() is true &&
            await app.FeatureChecker.GetValueForCustomerAsync<bool>("customer", "mooringdesk", "work-orders", false);
    }

    public static Func<string, Task<Subscription>> StripeRetriever(StripeClient client) =>
        id => new SubscriptionService(client).GetAsync(id, new SubscriptionGetOptions { Expand = ["latest_invoice"] });
}

public sealed class WebhookSignatureException(Exception cause) : Exception("Invalid webhook signature", cause);
