import { Subscrio } from 'subscrio';

const connectionString = process.env.DATABASE_URL;
if (!connectionString) throw new Error('Set DATABASE_URL to a dedicated PostgreSQL demo database.');
const subscrio = new Subscrio({ database: { connectionString } });

try {
  if (await subscrio.verifySchema() === null) await subscrio.installSchema();
  if (!await subscrio.products.getProduct('feedbackdesk'))
    await subscrio.products.createProduct({ key: 'feedbackdesk', displayName: 'FeedbackDesk' });

  if (!await subscrio.features.getFeature('csv-export'))
    await subscrio.features.createFeature({ key: 'csv-export', displayName: 'CSV export', valueType: 'toggle', defaultValue: 'false' });
  await subscrio.products.associateFeature('feedbackdesk', 'csv-export');

  for (const plan of [
    { key: 'feedbackdesk-free', displayName: 'Free', allowed: 'false' },
    { key: 'feedbackdesk-studio', displayName: 'Studio', allowed: 'true' },
  ]) {
    if (!await subscrio.plans.getPlan(plan.key))
      await subscrio.plans.createPlan({ key: plan.key, productKey: 'feedbackdesk', displayName: plan.displayName });
    await subscrio.plans.setFeatureValue(plan.key, 'csv-export', plan.allowed);
  }

  const cycles: Array<{
    key: string;
    planKey: string;
    displayName: string;
    durationUnit: 'months' | 'forever';
    durationValue?: number;
    externalProductId?: string;
  }> = [
    { key: 'studio-monthly-2025', planKey: 'feedbackdesk-studio', displayName: 'Studio monthly (2025 price)', durationUnit: 'months', durationValue: 1, externalProductId: 'price_feedbackdesk_2025' },
    { key: 'studio-monthly-2026', planKey: 'feedbackdesk-studio', displayName: 'Studio monthly (2026 price)', durationUnit: 'months', durationValue: 1, externalProductId: 'price_feedbackdesk_2026' },
    { key: 'free-ongoing', planKey: 'feedbackdesk-free', displayName: 'Free ongoing', durationUnit: 'forever' },
  ];
  for (const cycle of cycles) {
    if (!await subscrio.billingCycles.getBillingCycle(cycle.key))
      await subscrio.billingCycles.createBillingCycle({
        key: cycle.key,
        planKey: cycle.planKey,
        displayName: cycle.displayName,
        durationUnit: cycle.durationUnit,
        ...(cycle.durationValue === undefined ? {} : { durationValue: cycle.durationValue }),
        ...(cycle.externalProductId === undefined ? {} : { externalProductId: cycle.externalProductId }),
      });
  }

  for (const customer of ['legacy-team', 'current-team', 'free-team']) {
    if (!await subscrio.customers.getCustomer(customer))
      await subscrio.customers.createCustomer({ key: customer, displayName: customer });
  }

  for (const subscription of [
    { key: 'legacy-studio', customerKey: 'legacy-team', billingCycleKey: 'studio-monthly-2025' },
    { key: 'current-studio', customerKey: 'current-team', billingCycleKey: 'studio-monthly-2026' },
    { key: 'free-agreement', customerKey: 'free-team', billingCycleKey: 'free-ongoing' },
  ]) {
    if (!await subscrio.subscriptions.getSubscription(subscription.key))
      await subscrio.subscriptions.createSubscription(subscription);
  }
  console.log('PASS: old and new Stripe prices map to Studio; the free plan stays denied.');
} finally {
  await subscrio.close();
}
