import 'dotenv/config';
import assert from 'node:assert/strict';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';

const db = await isolatedDatabase();
const clock: {at: Date | null} = {at: null};
const app = new Subscrio({database: {connectionString: db.connectionString}, clock: {now: () => clock.at ?? new Date()}});
try {
await app.installSchema();
// Create the product
await app.products.createProduct({ key: 'pairclass', displayName: 'PairClass' });

// Define the features
await app.features.createFeature({
  key: 'live-classroom',
  displayName: 'live-classroom',
  valueType: 'toggle',
  defaultValue: 'false',
});

// Associate the features
await app.products.associateFeature('pairclass', 'live-classroom');

// Define the lesson allowance
await app.features.createFeature({
  key: 'lessons',
  displayName: 'Lessons',
  valueType: 'metered',
  defaultValue: '0',
  meteredConfig: {
    resetPeriod: 'monthly',
    enforcement: 'hard',
    aggregation: 'count',
    usageScope: 'customer',
  },
});

// Associate the lesson allowance
await app.products.associateFeature('pairclass', 'lessons');

// Create the plan
await app.plans.createPlan({
  key: 'classroom',
  productKey: 'pairclass',
  displayName: 'classroom',
});

// Set the plan values
await app.plans.setFeatureValue('classroom', 'live-classroom', 'true');
await app.plans.setFeatureValue('classroom', 'lessons', '10');

// Create the billing cycle
await app.billingCycles.createBillingCycle({
  key: 'classroom-monthly',
  planKey: 'classroom',
  displayName: 'Monthly',
  durationUnit: 'months',
  durationValue: 1,
});

// Create the customer
await app.customers.createCustomer({
  key: 'customer',
  displayName: 'PairClass demo customer',
});

// Start the classroom subscription
const started = new Date();
clock.at = started;
const cancellation = new Date(started.getTime() + 86400000);
await app.subscriptions.createSubscription({
  key: 'agreement',
  customerKey: 'customer',
  billingCycleKey: 'classroom-monthly',
  activationDate: started.toISOString(),
  currentPeriodStart: started.toISOString(),
  currentPeriodEnd: cancellation.toISOString(),
});

// Schedule the cancellation
await app.subscriptions.updateSubscription('agreement', {
  cancellationDate: cancellation.toISOString(),
});

// Observe the default mismatch
const defaultClassroomAccess = await app.featureChecker.isEnabledForCustomer('customer', 'pairclass', 'live-classroom');
assert.deepEqual(defaultClassroomAccess, false);
console.log('Default classroom check' + ": " + JSON.stringify(defaultClassroomAccess));
const lesson = await app.metering.reportUsage('customer', 'pairclass', 'lessons', 1, {
  idempotencyKey: 'lesson-before-cancel',
});
const lessonAcceptedBeforeCancellation = lesson.usage.hasAccess;
assert.deepEqual(lessonAcceptedBeforeCancellation, true);
console.log('Lesson accepted before cancellation' + ": " + JSON.stringify(lessonAcceptedBeforeCancellation));
const lessonsConsumed = lesson.usage.consumed;
assert.deepEqual(lessonsConsumed, 1);
console.log('Lessons consumed' + ": " + JSON.stringify(lessonsConsumed));

// Select the intended classroom policy
await app.products.associateFeature('pairclass', 'live-classroom', {
  subscriptionRule: 'most_generous',
});
const classroomAccessBeforeCancellation = await app.featureChecker.isEnabledForCustomer('customer', 'pairclass', 'live-classroom');
assert.deepEqual(classroomAccessBeforeCancellation, true);
console.log('Explicit classroom check' + ": " + JSON.stringify(classroomAccessBeforeCancellation));

// Check the actual cancellation instant
clock.at = cancellation;
const classroomAccessAtCancellation = await app.featureChecker.isEnabledForCustomer('customer', 'pairclass', 'live-classroom');
assert.deepEqual(classroomAccessAtCancellation, false);
console.log('Classroom at cancellation' + ": " + JSON.stringify(classroomAccessAtCancellation));
const usage = await app.metering.getUsage('customer', 'pairclass', 'lessons');
const lessonAccessAtCancellation = usage.hasAccess;
assert.deepEqual(lessonAccessAtCancellation, false);
console.log('Accounting access at cancellation' + ": " + JSON.stringify(lessonAccessAtCancellation));
await assert.rejects(() =>
  app.metering.reportUsage('customer', 'pairclass', 'lessons', 1, {
    idempotencyKey: 'lesson-after-cancel',
  }),
);
console.log('New lesson after cancellation: rejected');
console.log('PASS: PairClass behavior verified.');
} finally { await app.close(); await db.close(); }
