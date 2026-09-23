const fs=require('fs');
const Stripe=require('./typescript/node_modules/stripe');
const wait=ms=>new Promise(r=>setTimeout(r,ms));
async function main(){
 const key=process.env.STRIPE_SECRET_KEY;
 if(!key || !/^([sr]k_test_)/.test(key))throw Error('Set STRIPE_SECRET_KEY to a Stripe sandbox key');
 const stripe=new Stripe(key,{apiVersion:'2026-07-29.dahlia'});
 let clock,product,price;const result={origin:'Stripe sandbox test clock',apiVersion:'2026-07-29.dahlia',scenarios:[]};
 async function advance(at){await stripe.testHelpers.testClocks.advance(clock.id,{frozen_time:at});for(let n=0;n<90;n++){await wait(2000);const c=await stripe.testHelpers.testClocks.retrieve(clock.id);if(c.status==='ready')return;}throw Error('Clock did not become ready');}
 try{
  clock=await stripe.testHelpers.testClocks.create({frozen_time:Math.floor(Date.now()/1000),name:'MooringDesk sample verification'});
  product=await stripe.products.create({name:'MooringDesk sample verification',metadata:{purpose:'subscrio-sample'}});
  price=await stripe.prices.create({product:product.id,currency:'usd',unit_amount:1000,recurring:{interval:'month'}});
  const customer=await stripe.customers.create({name:'MooringDesk fictional test customer',test_clock:clock.id,payment_method:'pm_card_visa',invoice_settings:{default_payment_method:'pm_card_visa'}});
  let sub=await stripe.subscriptions.create({customer:customer.id,items:[{price:price.id}],expand:['latest_invoice'],metadata:{subscrioCustomerKey:'customer',subscrioSubscriptionKey:'agreement'}});
  result.initial=sub;result.price=price.id;result.customer=customer.id;
  async function capture(label,type){sub=await stripe.subscriptions.retrieve(sub.id,{expand:['latest_invoice']});let event;for(let n=0;n<30;n++){const events=await stripe.events.list({type,limit:100});event=events.data.find(e=>e.data.object.id===sub.id || e.data.object.id===sub.latest_invoice.id);if(event)break;await wait(2000);}if(!event)throw Error('Event missing: '+type);result.scenarios.push({label,event,current:sub,allowed:label!=='Failed renewal'&&label!=='Cancellation'});console.log(label+': '+sub.status+', invoice '+sub.latest_invoice.status);}
  await advance(sub.items.data[0].current_period_end+1);
  await advance(sub.items.data[0].current_period_end+7200);
  await capture('Successful renewal','invoice.payment_succeeded');
  if(sub.latest_invoice.status!=='paid')throw Error('Renewal was not paid');
  await capture('Subscription update','customer.subscription.updated');
  const failed=await stripe.paymentMethods.attach('pm_card_chargeCustomerFail',{customer:customer.id});
  await stripe.subscriptions.update(sub.id,{default_payment_method:failed.id});
  const boundary=sub.items.data[0].current_period_end;
  await advance(boundary+1);await advance(boundary+7200);
  await capture('Failed renewal','invoice.payment_failed');
  if(sub.status!=='past_due')throw Error('Expected failed renewal');
  await stripe.subscriptions.update(sub.id,{default_payment_method:customer.invoice_settings.default_payment_method});
  await stripe.invoices.pay(sub.latest_invoice.id,{payment_method:customer.invoice_settings.default_payment_method});
  await capture('Payment recovery','invoice.payment_succeeded');
  await stripe.subscriptions.cancel(sub.id);
  await capture('Cancellation','customer.subscription.deleted');
  fs.writeFileSync(__dirname+'/.sandbox-capture.json',JSON.stringify(result,null,2));
  require('./reduce-fixtures.cjs');
  console.log('PASS: real sandbox renewal, failure, recovery, and cancellation captured.');
 }finally{if(clock)await stripe.testHelpers.testClocks.del(clock.id);if(price)await stripe.prices.update(price.id,{active:false});if(product)await stripe.products.update(product.id,{active:false});console.log('Sandbox test clock deleted; sample price and product archived.');}
}
main().catch(e=>{console.error(e.type?e.type+': '+e.message:e.message);process.exitCode=1;});
