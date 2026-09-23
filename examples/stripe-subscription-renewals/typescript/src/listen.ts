import 'dotenv/config';
import {createServer} from 'node:http';
import {Client} from 'pg';
import Stripe from 'stripe';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';
import {RenewalWorker, stripeRetriever} from './renewals.js';

function required(name: string) {
  const value = process.env[name];
  if (!value) throw new Error(`Set ${name}. See the sample README.`);
  return value;
}
const key = required('STRIPE_SECRET_KEY');
if (!/^[sr]k_test_/.test(key)) throw new Error('Use a sandbox secret key.');
const secret = required('STRIPE_WEBHOOK_SECRET');
const priceId = required('STRIPE_PRICE_ID');
const subscriptionId = required('STRIPE_SUBSCRIPTION_ID');
const stripe = new Stripe(key);
const retrieve = stripeRetriever(stripe);
const current = await retrieve(subscriptionId);
const item = current.items.data.find(i => i.price.id === priceId);
if (!item || item.price.recurring?.interval !== 'month' || item.price.recurring.interval_count !== 1)
  throw new Error('Select the monthly price used by this subscription.');
if (current.livemode || typeof current.customer !== 'string') throw new Error('Expected a sandbox customer ID.');

const database = await isolatedDatabase();
const app = new Subscrio({database: {connectionString: database.connectionString}});
const db = new Client({connectionString: database.connectionString});
await db.connect();
const worker = new RenewalWorker(app, db, retrieve, secret, current.id, priceId, current.customer);
await app.installSchema();
await worker.install();
await app.products.createProduct({key: 'mooringdesk', displayName: 'MooringDesk'});
await app.features.createFeature({key: 'work-orders', displayName: 'Create work orders', valueType: 'toggle', defaultValue: 'false'});
await app.products.associateFeature('mooringdesk', 'work-orders', {subscriptionRule: 'most_generous'});
await app.plans.createPlan({key: 'marina', productKey: 'mooringdesk', displayName: 'Marina'});
await app.plans.setFeatureValue('marina', 'work-orders', 'true');
await app.billingCycles.createBillingCycle({key: 'monthly', planKey: 'marina', displayName: 'Monthly', durationUnit: 'months', durationValue: 1, externalProductId: priceId});
await app.customers.createCustomer({key: 'customer', displayName: 'MooringDesk customer', externalBillingId: current.customer});
await app.subscriptions.createSubscription({key: 'agreement', customerKey: 'customer', billingCycleKey: 'monthly', stripeSubscriptionId: current.id,
  activationDate: new Date(current.created * 1000), currentPeriodStart: new Date(item.current_period_start * 1000), currentPeriodEnd: new Date(item.current_period_end * 1000)});
// Seed payment eligibility from the authenticated Stripe API response.
const invoice = current.latest_invoice as Stripe.Invoice;
await db.query('INSERT INTO renewal_access VALUES ($1,$2)', [current.id, current.status === 'active' && invoice.status === 'paid']);

const server = createServer(async (req, res) => {
  try {
    if (req.method === 'GET' && req.url === '/status') {
      const agreement = await app.subscriptions.getSubscription('agreement');
      res.setHeader('Content-Type', 'application/json');
      res.end(JSON.stringify({subscriptionKey: agreement!.key, stripeSubscriptionId: agreement!.stripeSubscriptionId,
        currentPeriodEnd: agreement!.currentPeriodEnd, canCreateWorkOrder: await worker.canCreateWorkOrder(),
        subscriptionCount: (await app.subscriptions.getSubscriptionsByCustomer('customer')).length}));
      return;
    }
    if (req.method !== 'POST' || req.url !== '/stripe/webhook') { res.writeHead(404).end(); return; }
    const chunks: Buffer[] = [];
    for await (const chunk of req) chunks.push(Buffer.from(chunk));
    const result = await worker.receive(Buffer.concat(chunks), String(req.headers['stripe-signature'] ?? ''));
    console.log(`Webhook: ${result}`);
    res.writeHead(200).end(result);
  } catch (error) {
    const status = error instanceof Stripe.errors.StripeSignatureVerificationError ? 400 : 500;
    console.error(`Webhook/status request failed: HTTP ${status}`);
    res.writeHead(status).end('processing_failed');
  }
});
server.listen(4242, '127.0.0.1', () => console.log('MooringDesk listening: http://127.0.0.1:4242/status'));
let closing = false;
async function close() {
  if (closing) return;
  closing = true;
  await new Promise<void>(resolve => server.close(() => resolve()));
  await db.end(); await app.close(); await database.close();
  process.stdin.pause();
}
process.stdin.on('data', value => { if (value.toString().trim() === 'stop') void close(); });
process.on('SIGINT', () => { void close(); });
process.on('SIGTERM', () => { void close(); });
