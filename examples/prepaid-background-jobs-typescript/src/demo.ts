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
  await app.products.createProduct({ key: 'roomraster', displayName: 'RoomRaster' });

  // Define the feature values
  await app.features.createFeature({
    key: 'render',
    displayName: 'render',
    valueType: 'toggle',
    defaultValue: 'false',
  });

  // Associate the features
  await app.products.associateFeature('roomraster', 'render', {
    subscriptionRule: 'most_generous',
  });

  // Create the plan
  await app.plans.createPlan({
    key: 'standard',
    productKey: 'roomraster',
    displayName: 'standard',
  });

  // Assign the plan values
  await app.plans.setFeatureValue('standard', 'render', 'true');

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
    displayName: 'RoomRaster customer',
  });

  // Create the customer subscription
  await app.subscriptions.createSubscription({
    key: 'agreement',
    customerKey: 'customer',
    billingCycleKey: 'standard-cycle',
    activationDate: clock.at ?? new Date(),
  });

  // Configure render pricing
  await app.credits.createCurrency({
    key: 'render-credits',
    displayName: 'Render credits',
  });

  // Set the cost per job
  await app.credits.setConsumptionRule('render', 'render-credits', 12);

  // Load a confirmed prepaid purchase
  await app.credits.grant({
    customerKey: 'customer',
    currencyKey: 'render-credits',
    amount: 300,
    grantType: 'prepaid',
    idempotencyKey: 'paid-render-pack',
  });
  await app.credits.consume({
    customerKey: 'customer',
    featureKey: 'render',
    units: 24,
    idempotencyKey: 'completed-render-batch',
  });
  check(
    'Credits before competing jobs',
    (await app.credits.getBalance('customer', 'render-credits')).available,
    12,
  );

  // Let the debit decide which job can proceed
  async function admit(job: string) {
    if (
      !(await app.featureChecker.getValueForCustomer(
        'customer',
        'roomraster',
        'render',
        false,
      ))
    )
      throw new Error('Rendering is not included');
    return app.credits.consume({
      customerKey: 'customer',
      featureKey: 'render',
      units: 1,
      idempotencyKey: job,
    });
  }
  const attempts = await Promise.allSettled([
    admit('render-kitchen'),
    admit('render-bedroom'),
  ]);
  check('Accepted jobs', attempts.filter((r) => r.status === 'fulfilled').length, 1);
  check(
    'Rejected jobs',
    attempts.filter(
      (r) => r.status === 'rejected' && r.reason.name === 'InsufficientCreditsError',
    ).length,
    1,
  );
  check(
    'Remaining credits',
    (await app.credits.getBalance('customer', 'render-credits')).available,
    0,
  );

  // Correct a failed render once
  const winner = attempts.findIndex((r) => r.status === 'fulfilled');
  const failedJob = ['render-kitchen', 'render-bedroom'][winner]!;
  const correction = {
    customerKey: 'customer',
    currencyKey: 'render-credits',
    amount: 12,
    reason: 'Renderer failed before producing an image',
    idempotencyKey: 'correction-' + failedJob,
  };
  await app.credits.adjust(correction);
  await app.credits.adjust(correction);
  check(
    'Credits after repeated correction',
    (await app.credits.getBalance('customer', 'render-credits')).available,
    12,
  );
  console.log('PASS: RoomRaster behavior verified.');
} finally {
  await app.close();
  await db.close();
}
