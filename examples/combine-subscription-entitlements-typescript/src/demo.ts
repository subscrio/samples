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
  await app.products.createProduct({ key: 'exhibitloop', displayName: 'ExhibitLoop' });

  // Define the feature values
  await app.features.createFeature({
    key: 'display-slots',
    displayName: 'display-slots',
    valueType: 'numeric',
    defaultValue: '0',
  });

  // Associate the features
  await app.products.associateFeature('exhibitloop', 'display-slots', {
    subscriptionRule: 'most_generous',
  });

  // Create the plan
  await app.plans.createPlan({
    key: 'standard',
    productKey: 'exhibitloop',
    displayName: 'standard',
  });

  // Assign the plan values
  await app.plans.setFeatureValue('standard', 'display-slots', '4');

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
    displayName: 'ExhibitLoop customer',
  });

  // Create the second exhibition plan
  await app.plans.createPlan({
    key: 'visiting',
    productKey: 'exhibitloop',
    displayName: 'Visiting exhibition',
  });

  // Set the visiting exhibition allowance
  await app.plans.setFeatureValue('visiting', 'display-slots', '7');

  // Create the second billing cycle
  await app.billingCycles.createBillingCycle({
    key: 'visiting-cycle',
    planKey: 'visiting',
    displayName: 'Monthly',
    durationUnit: 'months',
    durationValue: 1,
  });

  // Create the customer subscription
  await app.subscriptions.createSubscription({
    key: 'agreement',
    customerKey: 'customer',
    billingCycleKey: 'standard-cycle',
    activationDate: clock.at ?? new Date(),
  });

  // Assign the visiting exhibition
  await app.subscriptions.createSubscription({
    key: 'visiting-agreement',
    customerKey: 'customer',
    billingCycleKey: 'visiting-cycle',
  });

  // Observe the most-generous rule
  check(
    'Most generous display slots',
    await app.featureChecker.getValueForCustomer(
      'customer',
      'exhibitloop',
      'display-slots',
      0,
    ),
    7,
  );

  // Use addition for purchased capacity
  await app.products.associateFeature('exhibitloop', 'display-slots', {
    subscriptionRule: 'additive',
  });
  check(
    'Combined display slots',
    await app.featureChecker.getValueForCustomer(
      'customer',
      'exhibitloop',
      'display-slots',
      0,
    ),
    11,
  );

  // Explain the returned allowance
  const explanation = await app.featureChecker.explainForCustomer(
    'customer',
    'exhibitloop',
    'display-slots',
  );
  check('Explained value', explanation.effectiveValue, '11');
  check('Contributing values', explanation.subscriptions.map((s) => s.value).sort(), [
    '4',
    '7',
  ]);
  console.log('PASS: ExhibitLoop behavior verified.');
} finally {
  await app.close();
  await db.close();
}
