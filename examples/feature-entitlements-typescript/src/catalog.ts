import type { Subscrio } from 'subscrio';

export async function seedCatalog(subscrio: Subscrio) {
  if (!await subscrio.products.getProduct('castcoop')) {
    await subscrio.products.createProduct({ key: 'castcoop', displayName: 'CastCoop' });
  }

  const features = [
    { key: 'scheduled-publishing', displayName: 'Scheduled publishing', valueType: 'toggle', defaultValue: 'false' },
    { key: 'show-limit', displayName: 'Show limit', valueType: 'numeric', defaultValue: '0' },
    { key: 'editorial-route', displayName: 'Editorial route', valueType: 'text', defaultValue: 'none' }
  ] as const;
  for (const feature of features) {
    if (!await subscrio.features.getFeature(feature.key)) {
      await subscrio.features.createFeature(feature);
    }
    await subscrio.products.associateFeature('castcoop', feature.key);
  }

  for (const plan of [
    { key: 'castcoop-free', displayName: 'Free' },
    { key: 'castcoop-collective', displayName: 'Collective' }
  ]) {
    if (!await subscrio.plans.getPlan(plan.key)) {
      await subscrio.plans.createPlan({ ...plan, productKey: 'castcoop' });
    }
  }

  await subscrio.plans.setFeatureValue('castcoop-free', 'scheduled-publishing', 'false');
  await subscrio.plans.setFeatureValue('castcoop-free', 'show-limit', '1');
  await subscrio.plans.setFeatureValue('castcoop-free', 'editorial-route', 'standard');
  await subscrio.plans.setFeatureValue('castcoop-collective', 'scheduled-publishing', 'true');
  await subscrio.plans.setFeatureValue('castcoop-collective', 'show-limit', '6');
  await subscrio.plans.setFeatureValue('castcoop-collective', 'editorial-route', 'priority');

  for (const cycle of [
    { key: 'castcoop-free-ongoing', planKey: 'castcoop-free', displayName: 'Free ongoing', durationUnit: 'forever' as const },
    { key: 'castcoop-collective-monthly', planKey: 'castcoop-collective', displayName: 'Collective monthly', durationUnit: 'months' as const, durationValue: 1 }
  ]) {
    if (!await subscrio.billingCycles.getBillingCycle(cycle.key)) {
      await subscrio.billingCycles.createBillingCycle(cycle);
    }
  }

  for (const customer of [
    { key: 'neighborhood-radio', displayName: 'Neighborhood Radio' },
    { key: 'harbor-network', displayName: 'Harbor Network' },
    { key: 'new-publisher', displayName: 'New publisher' }
  ]) {
    if (!await subscrio.customers.getCustomer(customer.key)) {
      await subscrio.customers.createCustomer(customer);
    }
  }

  for (const subscription of [
    { key: 'neighborhood-radio-free', customerKey: 'neighborhood-radio', billingCycleKey: 'castcoop-free-ongoing' },
    { key: 'harbor-network-collective', customerKey: 'harbor-network', billingCycleKey: 'castcoop-collective-monthly' }
  ]) {
    if (!await subscrio.subscriptions.getSubscription(subscription.key)) {
      await subscrio.subscriptions.createSubscription(subscription);
    }
  }
}
