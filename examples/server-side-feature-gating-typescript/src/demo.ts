import 'dotenv/config';
import assert from 'node:assert/strict';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';
import {replayPublicationRequests} from './http-check.js';
const db = await isolatedDatabase();
const app = new Subscrio({database: {connectionString: db.connectionString}});
try {
await app.installSchema();
// Create the product
await app.products.createProduct({ key: 'bracketcamp', displayName: 'BracketCamp' });

// Define the features
await app.features.createFeature({
  key: 'live-brackets',
  displayName: 'live-brackets',
  valueType: 'toggle',
  defaultValue: 'false',
});

// Associate the features
await app.products.associateFeature('bracketcamp', 'live-brackets');

// Create the offerings
for (const key of ['club', 'broadcast'])
  await app.plans.createPlan({ key, productKey: 'bracketcamp', displayName: key });

// Assign publication access
await app.plans.setFeatureValue('club', 'live-brackets', 'false');
await app.plans.setFeatureValue('broadcast', 'live-brackets', 'true');

// Create the cycles
for (const planKey of ['club', 'broadcast'])
  await app.billingCycles.createBillingCycle({
    key: planKey + '-monthly',
    planKey,
    displayName: 'Monthly',
    durationUnit: 'months',
    durationValue: 1,
  });

// Create the customers
await app.customers.createCustomer({ key: 'local-club' });
await app.customers.createCustomer({ key: 'regional-open' });

// Assign the subscriptions
await app.subscriptions.createSubscription({
  key: 'club-agreement',
  customerKey: 'local-club',
  billingCycleKey: 'club-monthly',
});
await app.subscriptions.createSubscription({
  key: 'broadcast-agreement',
  customerKey: 'regional-open',
  billingCycleKey: 'broadcast-monthly',
});

// Check before making the bracket public
const published: string[] = [];
async function publish(customerKey: string) {
  const allowed = await app.featureChecker.isEnabledForCustomer(
    customerKey,
    'bracketcamp',
    'live-brackets',
  );
  if (!allowed) return { status: 403, body: { error: 'live_brackets_not_included' } };
  published.push(customerKey);
  return { status: 201, body: { status: 'published' } };
}
await replayPublicationRequests(publish);
assert.deepEqual(published, ['regional-open']);
console.log('PASS: BracketCamp behavior verified.');
} finally { await app.close(); await db.close(); }
