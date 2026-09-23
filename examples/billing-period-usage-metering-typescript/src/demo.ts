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
  await app.products.createProduct({ key: 'subtitledock', displayName: 'SubtitleDock' });

  // Define the feature values
  await app.features.createFeature({
    key: 'processing-minutes',
    displayName: 'processing-minutes',
    valueType: 'metered',
    defaultValue: '0',
    meteredConfig: {
      resetPeriod: 'billing_period',
      enforcement: 'hard',
      aggregation: 'sum',
      usageScope: 'subscription',
    },
  });

  // Associate the features
  await app.products.associateFeature('subtitledock', 'processing-minutes', {
    subscriptionRule: 'most_generous',
  });

  // Create the plan
  await app.plans.createPlan({
    key: 'standard',
    productKey: 'subtitledock',
    displayName: 'standard',
  });

  // Assign the plan values
  await app.plans.setFeatureValue('standard', 'processing-minutes', '600');

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
    displayName: 'SubtitleDock customer',
  });

  // Assign the first billing period
  clock.at = new Date('2030-04-12T00:00:00Z');
  await app.subscriptions.createSubscription({
    key: 'agreement',
    customerKey: 'customer',
    billingCycleKey: 'standard-cycle',
    activationDate: clock.at,
    currentPeriodStart: clock.at,
    currentPeriodEnd: new Date('2030-05-12T00:00:00Z'),
  });

  // Charge the job in whole minutes
  const mediaSeconds = 16 * 60 + 21;
  const minutes = Math.ceil(mediaSeconds / 60);
  const recorded = await app.metering.reportUsage(
    'customer',
    'subtitledock',
    'processing-minutes',
    minutes,
    { subscriptionKey: 'agreement', idempotencyKey: 'film-spring-subtitles' },
  );
  check('Charged minutes', recorded.quantity, 17);
  check('Consumed minutes', recorded.usage.consumed, 17);
  check('Minutes remaining', recorded.usage.remaining, 583);

  // Detect stale period dates
  clock.at = new Date('2030-05-12T00:00:00Z');
  await assert.rejects(
    () =>
      app.metering.getUsage('customer', 'subtitledock', 'processing-minutes', {
        subscriptionKey: 'agreement',
      }),
    { name: 'MeteringPeriodError' },
  );
  check('Stale period rejected', true, true);

  // Store the renewed period
  await app.subscriptions.updateSubscription('agreement', {
    currentPeriodStart: clock.at,
    currentPeriodEnd: new Date('2030-06-12T00:00:00Z'),
  });
  const renewed = await app.metering.getUsage(
    'customer',
    'subtitledock',
    'processing-minutes',
    { subscriptionKey: 'agreement' },
  );
  check('Renewed consumption', renewed.consumed, 0);
  check('Renewed allowance', renewed.remaining, 600);

  // Retry an earlier job after renewal
  const replayed = await app.metering.reportUsage(
    'customer',
    'subtitledock',
    'processing-minutes',
    17,
    { subscriptionKey: 'agreement', idempotencyKey: 'film-spring-subtitles' },
  );
  check('Original job snapshot', replayed.usage.consumed, 17);
  check(
    'Current period still unused',
    (
      await app.metering.getUsage('customer', 'subtitledock', 'processing-minutes', {
        subscriptionKey: 'agreement',
      })
    ).consumed,
    0,
  );
  console.log('PASS: SubtitleDock behavior verified.');
} finally {
  await app.close();
  await db.close();
}
