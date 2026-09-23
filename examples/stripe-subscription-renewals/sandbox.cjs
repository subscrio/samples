// Reader-operated sandbox lifecycle. Never uses live-mode credentials.
const fs = require('node:fs');
const Stripe = require('./typescript/node_modules/stripe');
const path = require('node:path');
const statePath = path.join(__dirname, '.sandbox-session.json');
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
async function main() {
  const key = process.env.STRIPE_SECRET_KEY;
  if (!key || !/^[sr]k_test_/.test(key)) throw Error('Set STRIPE_SECRET_KEY to a sandbox secret key.');
  const stripe = new Stripe(key);
  const action = process.argv[2];
  if (action === 'setup') {
    if (fs.existsSync(statePath)) throw Error('An existing session must be cleaned up first.');
    const price = await stripe.prices.retrieve(process.env.STRIPE_PRICE_ID || '');
    if (price.livemode || price.recurring?.interval !== 'month' || price.recurring.interval_count !== 1)
      throw Error('Set STRIPE_PRICE_ID to your recurring monthly sandbox price.');
    const clock = await stripe.testHelpers.testClocks.create({frozen_time: Math.floor(Date.now() / 1000), name: 'MooringDesk renewal tutorial'});
    try {
      const customer = await stripe.customers.create({name: 'MooringDesk test customer', test_clock: clock.id,
        payment_method: 'pm_card_visa', invoice_settings: {default_payment_method: 'pm_card_visa'}});
      const subscription = await stripe.subscriptions.create({customer: customer.id, items: [{price: price.id}], expand: ['latest_invoice'],
        metadata: {subscrioCustomerKey: 'customer', subscrioSubscriptionKey: 'agreement'}});
      if (subscription.status !== 'active' || subscription.latest_invoice.status !== 'paid') throw Error('Initial payment did not complete.');
      fs.writeFileSync(statePath, JSON.stringify({clock: clock.id, customer: customer.id, subscription: subscription.id,
        price: price.id, paymentMethod: customer.invoice_settings.default_payment_method}, null, 2));
      console.log('STRIPE_SUBSCRIPTION_ID=' + subscription.id);
      console.log('Customer: ' + customer.id);
      console.log('Initial period end: ' + new Date(subscription.items.data[0].current_period_end * 1000).toISOString());
      console.log('Keep this session for renew, fail, recover, cancel, and cleanup.');
    } catch (error) { await stripe.testHelpers.testClocks.del(clock.id); throw error; }
    return;
  }
  const session = JSON.parse(fs.readFileSync(statePath, 'utf8'));
  if (action === 'cleanup') {
    await stripe.testHelpers.testClocks.del(session.clock);
    fs.unlinkSync(statePath);
    console.log('Deleted the tutorial clock, customer, and subscription. Your catalog price is unchanged.');
    return;
  }
  let current = await stripe.subscriptions.retrieve(session.subscription, {expand: ['latest_invoice']});
  async function advance(time) {
    await stripe.testHelpers.testClocks.advance(session.clock, {frozen_time: time});
    for (let attempt = 0; attempt < 90; attempt++) {
      await wait(2000);
      if ((await stripe.testHelpers.testClocks.retrieve(session.clock)).status === 'ready') return;
    }
    throw Error('Clock is still advancing. Inspect it in Stripe before continuing.');
  }
  if (action === 'renew' || action === 'fail') {
    if (current.status !== 'active') throw Error('Start this step with an active subscription.');
    if (action === 'fail') {
      const failed = await stripe.paymentMethods.attach('pm_card_chargeCustomerFail', {customer: session.customer});
      await stripe.subscriptions.update(current.id, {default_payment_method: failed.id});
    }
    const boundary = current.items.data[0].current_period_end;
    await advance(boundary + 1);
    // Allow invoice finalization and collection to happen in simulated time.
    await advance(boundary + 7200);
  } else if (action === 'recover') {
    await stripe.subscriptions.update(current.id, {default_payment_method: session.paymentMethod});
    await stripe.invoices.pay(current.latest_invoice.id, {payment_method: session.paymentMethod});
  } else if (action === 'cancel') {
    await stripe.subscriptions.cancel(current.id);
  } else throw Error('Use setup, renew, fail, recover, cancel, or cleanup.');
  current = await stripe.subscriptions.retrieve(session.subscription, {expand: ['latest_invoice']});
  console.log(JSON.stringify({stripeStatus: current.status, invoiceStatus: current.latest_invoice.status,
    currentPeriodEnd: new Date(current.items.data[0].current_period_end * 1000).toISOString()}, null, 2));
  console.log('Wait for a forwarded HTTP 200, then inspect http://127.0.0.1:4242/status');
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
