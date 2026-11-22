import 'dotenv/config';
import assert from 'node:assert/strict';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';

const db = await isolatedDatabase();

const app = new Subscrio({database: {connectionString: db.connectionString}});
try {
await app.installSchema();
// Create the product
await app.products.createProduct({ key: 'orbittile', displayName: 'OrbitTile' });

// Define the features
await app.features.createFeature({
  key: 'export-format',
  displayName: 'export-format',
  valueType: 'text',
  defaultValue: 'none',
});

// Associate the features
await app.products.associateFeature('orbittile', 'export-format', {
  addonRule: 'override_wins',
  subscriptionRule: 'override_wins',
});

// Create the plan
await app.plans.createPlan({
  key: 'imagery',
  productKey: 'orbittile',
  displayName: 'imagery',
});

// Set the plan values
await app.plans.setFeatureValue('imagery', 'export-format', 'PNG');

// Create the billing cycle
await app.billingCycles.createBillingCycle({
  key: 'imagery-monthly',
  planKey: 'imagery',
  displayName: 'Monthly',
  durationUnit: 'months',
  durationValue: 1,
});

// Create the customer
await app.customers.createCustomer({
  key: 'customer',
  displayName: 'OrbitTile demo customer',
});

// Assign the imagery agreement
await app.subscriptions.createSubscription({
  key: 'agreement',
  customerKey: 'customer',
  billingCycleKey: 'imagery-monthly',
});

// Define the replacement priorities
await app.addons.createAddon({
  key: 'tiff-export',
  productKey: 'orbittile',
  displayName: 'TIFF export',
  compositionMode: 'override',
  priority: 20,
  featureValues: { 'export-format': 'TIFF' },
});
await app.addons.createAddon({
  key: 'science-export',
  productKey: 'orbittile',
  displayName: 'Scientific export',
  compositionMode: 'override',
  priority: 5,
  featureValues: { 'export-format': 'NetCDF' },
});
const readExportFormat = () =>
  app.featureChecker.getValueForCustomer('customer', 'orbittile', 'export-format', 'none');
const baseExportFormat = await readExportFormat();
assert.deepEqual(baseExportFormat, 'PNG');
console.log('Base format' + ": " + JSON.stringify(baseExportFormat));

// Attach each package once
await app.subscriptions.attachAddon('agreement', 'tiff-export', 1);
const formatWithTiff = await readExportFormat();
assert.deepEqual(formatWithTiff, 'TIFF');
console.log('TIFF attached' + ": " + JSON.stringify(formatWithTiff));
await app.subscriptions.attachAddon('agreement', 'science-export', 1);
const formatWithBothPackages = await readExportFormat();
assert.deepEqual(formatWithBothPackages, 'NetCDF');
console.log('Both attached' + ": " + JSON.stringify(formatWithBothPackages));
await assert.rejects(
  () => app.subscriptions.attachAddon('agreement', 'science-export', 2),
  /quantity/i,
);
console.log('Replacement quantity two: rejected');

// Remove the winning source
await app.subscriptions.detachAddon('agreement', 'science-export');
const formatAfterScientificRemoval = await readExportFormat();
assert.deepEqual(formatAfterScientificRemoval, 'TIFF');
console.log('Scientific detached' + ": " + JSON.stringify(formatAfterScientificRemoval));
await app.subscriptions.detachAddon('agreement', 'tiff-export');
const formatAfterAllRemovals = await readExportFormat();
assert.deepEqual(formatAfterAllRemovals, 'PNG');
console.log('All detached' + ": " + JSON.stringify(formatAfterAllRemovals));
await app.subscriptions.attachAddon('agreement', 'science-export', 1);
await app.subscriptions.attachAddon('agreement', 'tiff-export', 1);
assert.equal(await readExportFormat(), 'NetCDF');
console.log('Scientific first, TIFF second: NetCDF still selected');
console.log('PASS: OrbitTile behavior verified.');
} finally { await app.close(); await db.close(); }
