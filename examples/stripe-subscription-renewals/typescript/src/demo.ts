import 'dotenv/config';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createServer} from 'node:http';
import {once} from 'node:events';
import Stripe from 'stripe';
import {Client} from 'pg';
import {Subscrio} from 'subscrio';
import {isolatedDatabase} from './database.js';
import {RenewalWorker} from './renewals.js';

const fixtures = JSON.parse(readFileSync(new URL('../../fixtures.json',import.meta.url),'utf8')) as {
  initial: Stripe.Subscription;
  scenarios: {label:string;event:Stripe.Event;current:Stripe.Subscription;allowed:boolean}[];
};
const database = await isolatedDatabase();
const app = new Subscrio({database:{connectionString:database.connectionString}});
const db = new Client({connectionString:database.connectionString});
await db.connect();
const secret='whsec_local_fixture_only';
let current=fixtures.initial;
const worker = new RenewalWorker(app,db,async()=>structuredClone(current),secret,current.id);
const check=(label:string,actual:unknown,expected:unknown)=>{assert.deepEqual(actual,expected);console.log(label+': '+JSON.stringify(actual));};
try {
  await app.installSchema();
  await worker.install();
  // Product, feature, association, plan, value, cycle, customer, and existing agreement.
  await app.products.createProduct({key:'mooringdesk',displayName:'MooringDesk'});
  await app.features.createFeature({key:'work-orders',displayName:'Create work orders',valueType:'toggle',defaultValue:'false'});
  await app.products.associateFeature('mooringdesk','work-orders',{subscriptionRule:'most_generous'});
  await app.plans.createPlan({key:'marina',productKey:'mooringdesk',displayName:'Marina'});
  await app.plans.setFeatureValue('marina','work-orders','true');
  await app.billingCycles.createBillingCycle({key:'monthly',planKey:'marina',displayName:'Monthly',durationUnit:'months',durationValue:1,externalProductId:'price_mooring'});
  await app.customers.createCustomer({key:'customer',displayName:'MooringDesk customer',externalBillingId:'cus_mooring'});
  await app.subscriptions.createSubscription({key:'agreement',customerKey:'customer',billingCycleKey:'monthly',stripeSubscriptionId:current.id,
    activationDate:new Date(current.created*1000),currentPeriodStart:new Date(current.items.data[0]!.current_period_start*1000),currentPeriodEnd:new Date(current.items.data[0]!.current_period_end*1000)});
  const server=createServer(async(req,res)=>{
    if(req.method!=='POST'||req.url!=='/stripe/webhook'){res.writeHead(404).end();return;}
    const chunks:Buffer[]=[];for await(const chunk of req)chunks.push(Buffer.from(chunk));
    try{const result=await worker.receive(Buffer.concat(chunks),String(req.headers['stripe-signature']??''));res.writeHead(200).end(result);}
    catch(error){res.writeHead(error instanceof Stripe.errors.StripeSignatureVerificationError?400:500).end('processing_failed');}
  });
  server.listen(0,'127.0.0.1');await once(server,'listening');
  const address=server.address();if(!address||typeof address==='string')throw Error('No port');
  const deliver=async(event:Stripe.Event,valid=true)=>{const payload=JSON.stringify(event);const signature=Stripe.webhooks.generateTestHeaderString({payload,secret:valid?secret:'wrong-secret'});return fetch(`http://127.0.0.1:${address.port}/stripe/webhook`,{method:'POST',headers:{'stripe-signature':signature},body:payload});};
  try {
    const first=fixtures.scenarios[0]!;
    check('Invalid signature HTTP',(await deliver(first.event,false)).status,400);
    check('Receipts after invalid signature',(await db.query('SELECT * FROM renewal_receipts')).rowCount,0);
    for(const scenario of fixtures.scenarios){
      current=scenario.current;
      check(scenario.label+' HTTP',(await deliver(scenario.event)).status,200);
      check(scenario.label+' access',await worker.canCreateWorkOrder(),scenario.allowed);
      const agreement=await app.subscriptions.getSubscription('agreement');
      check(scenario.label+' period end',new Date(agreement!.currentPeriodEnd!).toISOString(),new Date(current.items.data[0]!.current_period_end*1000).toISOString());
      check(scenario.label+' replay',await (await deliver(scenario.event)).text(),'duplicate');
    }
    check('Linked agreement count',(await app.subscriptions.getSubscriptionsByCustomer('customer')).length,1);
    // Simulate process restart: the new worker uses the same durable receipts.
    const restarted=new RenewalWorker(app,db,async()=>structuredClone(current),secret,current.id);
    const replay=JSON.stringify(first.event);
    check('Replay after worker restart',await restarted.receive(Buffer.from(replay),Stripe.webhooks.generateTestHeaderString({payload:replay,secret})),'duplicate');
    // A delayed old snapshot has a new event ID but must reconcile current cancellation.
    check('Delayed event HTTP',(await deliver({...first.event,id:'evt_delayed'})).status,200);
    check('Delayed event preserves cancellation',await worker.canCreateWorkOrder(),false);
    for(const problem of ['price','customer']){
      current=structuredClone(first.current);
      if(problem==='price')current.items.data[0]!.price.id='price_unknown';else current.customer='cus_unknown';
      const event={...first.event,id:'evt_retry_'+problem};
      check(problem+' failure HTTP',(await deliver(event)).status,500);
      current=first.current;
      check(problem+' repair HTTP',(await deliver(event)).status,200);
    }
    check('Recovery after processing repair',await worker.canCreateWorkOrder(),true);
    console.log('PASS: MooringDesk renewal behavior verified.');
  } finally {server.closeAllConnections();await new Promise<void>((resolve,reject)=>server.close(e=>e?reject(e):resolve()));}
} finally {await db.end();await app.close();await database.close();}
