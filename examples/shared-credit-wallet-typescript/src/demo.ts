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
  await app.products.createProduct({ key: 'spritemint', displayName: 'SpriteMint' });

  // Define the feature values
  await app.features.createFeature({
    key: 'texture',
    displayName: 'texture',
    valueType: 'toggle',
    defaultValue: 'false',
  });

  // Associate the features
  await app.products.associateFeature('spritemint', 'texture', {
    subscriptionRule: 'most_generous',
  });

  // Create the plan
  await app.plans.createPlan({
    key: 'standard',
    productKey: 'spritemint',
    displayName: 'standard',
  });

  // Assign the plan values
  await app.plans.setFeatureValue('standard', 'texture', 'true');

  // Create the billing cycle
  await app.billingCycles.createBillingCycle({
    key: 'standard-cycle',
    planKey: 'standard',
    displayName: 'Monthly',
    durationUnit: 'months',
    durationValue: 1,
  });

  // Create the customer
  await app.customers.createCustomer({
    key: 'customer',
    displayName: 'SpriteMint customer',
  });

  // Create the animation product
  await app.products.createProduct({ key: 'animation', displayName: 'Sprite animation' });

  // Define animation access
  await app.features.createFeature({
    key: 'animate',
    displayName: 'Animate sprite',
    valueType: 'toggle',
    defaultValue: 'false',
  });

  // Associate animation access
  await app.products.associateFeature('animation', 'animate', {
    subscriptionRule: 'most_generous',
  });

  // Create the animation plan
  await app.plans.createPlan({
    key: 'animator',
    productKey: 'animation',
    displayName: 'Animator',
  });

  // Include animation access
  await app.plans.setFeatureValue('animator', 'animate', 'true');

  // Create its billing cycle
  await app.billingCycles.createBillingCycle({
    key: 'animator-cycle',
    planKey: 'animator',
    displayName: 'Monthly',
    durationUnit: 'months',
    durationValue: 1,
  });

  // Set up one shared currency
  await app.credits.createCurrency({
    key: 'asset-credits',
    displayName: 'Asset credits',
  });

  // Fund the wallet from one plan
  await app.credits.setPlanGrant('standard', 'asset-credits', {
    amount: 60,
    cadence: 'monthly',
  });

  // Price texture and animation work
  await app.credits.setConsumptionRule('texture', 'asset-credits', 3);
  await app.credits.setConsumptionRule('animate', 'asset-credits', 11);

  // Create the customer subscription
  await app.subscriptions.createSubscription({
    key: 'agreement',
    customerKey: 'customer',
    billingCycleKey: 'standard-cycle',
    activationDate: clock.at ?? new Date(),
  });

  // Add the animation agreement
  await app.subscriptions.createSubscription({
    key: 'animation-agreement',
    customerKey: 'customer',
    billingCycleKey: 'animator-cycle',
  });
  check(
    'Shared opening balance',
    (await app.credits.getBalance('customer', 'asset-credits')).available,
    60,
  );

  // Spend from either product
  async function makeAsset(product: string, feature: string, jobKey: string) {
    if (
      !(await app.featureChecker.getValueForCustomer('customer', product, feature, false))
    )
      return 'not_included';
    await app.credits.consume({
      customerKey: 'customer',
      featureKey: feature,
      units: 1,
      idempotencyKey: jobKey,
    });
    return 'accepted';
  }
  check(
    'Texture job',
    await makeAsset('spritemint', 'texture', 'texture-oak'),
    'accepted',
  );
  check(
    'Animation job',
    await makeAsset('animation', 'animate', 'walk-cycle'),
    'accepted',
  );
  check(
    'Shared closing balance',
    (await app.credits.getBalance('customer', 'asset-credits')).available,
    46,
  );
  check('Customer wallets', (await app.credits.listBalances('customer')).length, 1);

  // Deny a product without losing the wallet
  await app.plans.setFeatureValue('animator', 'animate', 'false');
  check(
    'Animation after access ends',
    await makeAsset('animation', 'animate', 'run-cycle'),
    'not_included',
  );
  check(
    'Preserved wallet',
    (await app.credits.getBalance('customer', 'asset-credits')).available,
    46,
  );
  console.log('PASS: SpriteMint behavior verified.');
} finally {
  await app.close();
  await db.close();
}
