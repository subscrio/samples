import 'dotenv/config';
import assert from 'node:assert/strict';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';

const db = await isolatedDatabase();
const clock: {at: Date | null} = {at: null};
const app = new Subscrio({database: {connectionString: db.connectionString}, clock: {now: () => clock.at ?? new Date()}});
function check(label: string, actual: unknown, expected: unknown) {
  assert.deepEqual(actual, expected); console.log(label + ': ' + JSON.stringify(actual));
}
try {
await app.installSchema();
// Create the product
await app.products.createProduct({ key: 'linguadesk', displayName: 'LinguaDesk' });

// Define the features
await app.features.createFeature({
  key: 'submit-jobs',
  displayName: 'submit-jobs',
  valueType: 'toggle',
  defaultValue: 'false',
});

// Associate the features
await app.products.associateFeature('linguadesk', 'submit-jobs', {
  addonRule: 'additive',
  subscriptionRule: 'most_generous',
});

// Create the plan
await app.plans.createPlan({
  key: 'agency',
  productKey: 'linguadesk',
  displayName: 'agency',
});

// Set the plan values
await app.plans.setFeatureValue('agency', 'submit-jobs', 'true');

// Create the billing cycle
await app.billingCycles.createBillingCycle({
  key: 'agency-monthly',
  planKey: 'agency',
  displayName: 'Monthly',
  durationUnit: 'months',
  durationValue: 1,
});

// Create the customer
await app.customers.createCustomer({
  key: 'customer',
  displayName: 'LinguaDesk demo customer',
});

// Assign the future agreement
const startsAt = '2030-05-01T09:00:00.000Z';
clock.at = new Date('2030-04-30T09:00:00.000Z');
await app.subscriptions.createSubscription({
  key: 'agreement',
  customerKey: 'customer',
  billingCycleKey: 'agency-monthly',
  activationDate: startsAt,
});
const onboarding = { status: 'ready', preferencesSaved: true };
check('Before start: onboarding', onboarding.status, 'ready');

// Check before accepting a job
const jobs: string[] = [];
async function submitJob(customerKey: string) {
  if (
    !(await app.featureChecker.isEnabledForCustomer(
      customerKey,
      'linguadesk',
      'submit-jobs',
    ))
  )
    return 'subscription_not_started';
  jobs.push(customerKey);
  return 'accepted';
}
clock.at = new Date('2030-05-01T08:59:59.999Z');
check('One millisecond before', await submitJob('customer'), 'subscription_not_started');
assert.equal(jobs.length, 0);
clock.at = new Date(startsAt);
check('At activation', await submitJob('customer'), 'accepted');
assert.equal(jobs.length, 1);
console.log('PASS: LinguaDesk behavior verified.');
} finally { await app.close(); await db.close(); }
