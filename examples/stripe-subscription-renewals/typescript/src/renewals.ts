import Stripe from 'stripe';
import {Client} from 'pg';
import {Subscrio} from 'subscrio';

// Run one serialized renewal worker. Multiple workers need a distributed lock
// per Stripe subscription covering retrieval, Subscrio updates, and receipt commit.
export class RenewalWorker {
  private tail: Promise<unknown> = Promise.resolve();
  constructor(private app: Subscrio, private db: Client,
    private retrieve: (id: string) => Promise<Stripe.Subscription>,
    private secret: string, private subscriptionId: string,
    private priceId = 'price_mooring', private customerId = 'cus_mooring') {}

  async install() {
    await this.db.query(`CREATE TABLE renewal_receipts (event_id text PRIMARY KEY);
      CREATE TABLE renewal_access (subscription_id text PRIMARY KEY, allowed boolean NOT NULL)`);
  }

  receive(rawBody: Buffer, signature: string): Promise<string> {
    const event = Stripe.webhooks.constructEvent(rawBody, signature, this.secret);
    const job = this.tail.then(() => this.process(event));
    this.tail = job.catch(() => undefined);
    return job;
  }

  private async process(event: Stripe.Event): Promise<string> {
    if (!['invoice.payment_succeeded','invoice.payment_failed',
      'customer.subscription.updated','customer.subscription.deleted'].includes(event.type)) return 'ignored';
    const object = event.data.object as Stripe.Subscription | Stripe.Invoice;
    const reference = object.object === 'subscription' ? object.id
      : object.parent?.subscription_details?.subscription;
    const id = typeof reference === 'string' ? reference : reference?.id;
    if (id !== this.subscriptionId) return 'ignored';
    if ((await this.db.query('SELECT 1 FROM renewal_receipts WHERE event_id=$1',[event.id])).rowCount) return 'duplicate';

    // Fetch current billing state rather than treating event delivery order as time order.
    const current = await this.retrieve(id);
    if (current.id !== id) throw new Error('Subscription retrieval mismatch');
    const item = current.items.data.find(i=>i.price.id===this.priceId);
    if (!item) throw new Error('Unmapped Stripe price');
    if (current.customer !== this.customerId) throw new Error('Missing customer link');
    const invoice = current.latest_invoice;
    if (!invoice || typeof invoice === 'string') throw new Error('Expand latest_invoice when retrieving');

    // Only pass the paid invoice snapshot when it describes the current period.
    if (event.type === 'invoice.payment_succeeded') {
      const paid = event.data.object as Stripe.Invoice;
      if (paid.lines.data.some(l=>l.pricing?.price_details?.price === item.price.id
        && l.period.end === item.current_period_end)) await this.app.stripe.processStripeEvent(event);
    }
    const reconciliation = {...event,type:'customer.subscription.updated',data:{object:current}} as Stripe.Event;
    await this.app.stripe.processStripeEvent(reconciliation);

    // MooringDesk suspends new work while the latest invoice is unpaid.
    // invoice.payment_failed has no built-in Subscrio dispatcher handler.
    const allowed = current.status === 'active' && invoice.status === 'paid';
    await this.db.query('BEGIN');
    try {
      await this.db.query(`INSERT INTO renewal_access VALUES ($1,$2)
        ON CONFLICT (subscription_id) DO UPDATE SET allowed=EXCLUDED.allowed`,[id,allowed]);
      await this.db.query('INSERT INTO renewal_receipts VALUES ($1)',[event.id]);
      await this.db.query('COMMIT');
    } catch (error) { await this.db.query('ROLLBACK'); throw error; }
    return 'processed';
  }

  async canCreateWorkOrder() {
    const state = await this.db.query('SELECT allowed FROM renewal_access WHERE subscription_id=$1',[this.subscriptionId]);
    return state.rows[0]?.allowed === true &&
      await this.app.featureChecker.getValueForCustomer('customer','mooringdesk','work-orders',false);
  }
}

// Live sandbox adapter: supply this function instead of the recorded test response.
export function stripeRetriever(stripe: Stripe) {
  return (id: string) => stripe.subscriptions.retrieve(id,{expand:['latest_invoice']});
}
