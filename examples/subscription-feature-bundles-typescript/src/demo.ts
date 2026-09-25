import 'dotenv/config';
import assert from 'node:assert/strict';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';

const db = await isolatedDatabase();

const app = new Subscrio({database: {connectionString: db.connectionString}});
function check(label: string, actual: unknown, expected: unknown) {
  assert.deepEqual(actual, expected); console.log(label + ': ' + JSON.stringify(actual));
}
try {
await app.installSchema();
// Create the product
await app.products.createProduct({ key: 'vowgallery', displayName: 'VowGallery' });

// Define the features
await app.features.createFeature({
  key: 'custom-branding',
  displayName: 'custom-branding',
  valueType: 'toggle',
  defaultValue: 'false',
});
await app.features.createFeature({
  key: 'private-selections',
  displayName: 'private-selections',
  valueType: 'toggle',
  defaultValue: 'false',
});
await app.features.createFeature({
  key: 'gallery-limit',
  displayName: 'gallery-limit',
  valueType: 'numeric',
  defaultValue: '0',
});

// Associate the features
await app.products.associateFeature('vowgallery', 'custom-branding', {
  addonRule: 'additive',
  subscriptionRule: 'most_generous',
});
await app.products.associateFeature('vowgallery', 'private-selections', {
  addonRule: 'additive',
  subscriptionRule: 'most_generous',
});
await app.products.associateFeature('vowgallery', 'gallery-limit', {
  addonRule: 'additive',
  subscriptionRule: 'most_generous',
});

// Create the plan
await app.plans.createPlan({
  key: 'standard',
  productKey: 'vowgallery',
  displayName: 'standard',
});

// Set the plan values
await app.plans.setFeatureValue('standard', 'custom-branding', 'false');
await app.plans.setFeatureValue('standard', 'private-selections', 'false');
await app.plans.setFeatureValue('standard', 'gallery-limit', '20');

// Create the billing cycle
await app.billingCycles.createBillingCycle({
  key: 'standard-monthly',
  planKey: 'standard',
  displayName: 'Monthly',
  durationUnit: 'months',
  durationValue: 1,
});

// Create the customer
await app.customers.createCustomer({
  key: 'customer',
  displayName: 'VowGallery demo customer',
});

// Assign the standard subscription
await app.subscriptions.createSubscription({
  key: 'agreement',
  customerKey: 'customer',
  billingCycleKey: 'standard-monthly',
});

// Define the bundle
await app.addons.createAddon({
  key: 'presentation',
  productKey: 'vowgallery',
  displayName: 'Client presentation',
  featureValues: { 'custom-branding': 'true', 'private-selections': 'true' },
});

// Read the three decisions
async function snapshot(label: string, enabled: boolean) {
  const branding = await app.featureChecker.isEnabledForCustomer(
    'customer',
    'vowgallery',
    'custom-branding',
  );
  const selections = await app.featureChecker.isEnabledForCustomer(
    'customer',
    'vowgallery',
    'private-selections',
  );
  const galleries = await app.featureChecker.getValueForCustomer(
    'customer',
    'vowgallery',
    'gallery-limit',
    0,
  );
  assert.deepEqual([branding, selections, galleries], [enabled, enabled, 20]);
  console.log(label + ': ' + JSON.stringify({ branding, selections, galleries }));
}
await snapshot('Before purchase', false);

// Attach both benefits together
await app.subscriptions.attachAddon('agreement', 'presentation');
await snapshot('Bundle attached', true);

// Detach and verify fallback
await app.subscriptions.detachAddon('agreement', 'presentation');
await snapshot('Bundle detached', false);
console.log('PASS: VowGallery behavior verified.');
} finally { await app.close(); await db.close(); }
