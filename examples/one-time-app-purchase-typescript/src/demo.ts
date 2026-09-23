import 'dotenv/config';
import assert from 'node:assert/strict';
import { Subscrio } from 'subscrio';
import { isolatedDatabase } from './database.js';

const db = await isolatedDatabase();
const clock: { at: Date | null } = { at: null };
const app = new Subscrio({
  database: { connectionString: db.connectionString },
  clock: { now: () => clock.at ?? new Date() },
});
function check(label: string, actual: unknown, expected: unknown) {
  assert.deepEqual(actual, expected);
  console.log(label + ': ' + JSON.stringify(actual));
}
try {
  await app.installSchema();
  // Create the product
  await app.products.createProduct({ key: 'ridgeatlas', displayName: 'RidgeAtlas' });

  // Define the feature values
  await app.features.createFeature({
    key: 'app-access',
    displayName: 'app-access',
    valueType: 'toggle',
    defaultValue: 'false',
  });

  // Associate the features
  await app.products.associateFeature('ridgeatlas', 'app-access', {
    subscriptionRule: 'most_generous',
  });

  // Create the plan
  await app.plans.createPlan({
    key: 'standard',
    productKey: 'ridgeatlas',
    displayName: 'standard',
  });

  // Assign the plan values
  await app.plans.setFeatureValue('standard', 'app-access', 'true');

  // Create the billing cycle
  await app.billingCycles.createBillingCycle({
    key: 'standard-cycle',
    planKey: 'standard',
    displayName: 'Ongoing',
    durationUnit: 'forever',
  });

  // Create the customer
  await app.customers.createCustomer({
    key: 'customer',
    displayName: 'RidgeAtlas customer',
  });

  // Record the paid order
  await app.subscriptions.createSubscription({
    key: 'purchase-ridge-204',
    customerKey: 'customer',
    billingCycleKey: 'standard-cycle',
  });
  await app.customers.createCustomer({
    key: 'visitor',
    displayName: 'Customer without a purchase',
  });

  // Use one entitlement for the whole planner
  async function openPlanner(customer: string, device: string) {
    const purchased = await app.featureChecker.getValueForCustomer(
      customer,
      'ridgeatlas',
      'app-access',
      false,
    );
    return { device, tools: purchased ? ['route', 'elevation', 'itinerary'] : [] };
  }
  check('Laptop tools', (await openPlanner('customer', 'laptop')).tools, [
    'route',
    'elevation',
    'itinerary',
  ]);
  check('Visitor tools', (await openPlanner('visitor', 'phone')).tools, []);

  // Return from another device
  clock.at = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000);
  check('Phone tools on later visit', (await openPlanner('customer', 'phone')).tools, [
    'route',
    'elevation',
    'itinerary',
  ]);
  const purchase = await app.subscriptions.getSubscription('purchase-ridge-204');
  check('Original purchase customer', purchase?.customerKey, 'customer');
  console.log('PASS: RidgeAtlas behavior verified.');
} finally {
  await app.close();
  await db.close();
}
