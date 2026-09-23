import 'dotenv/config';
import assert from 'node:assert/strict';
import { Subscrio } from 'subscrio';
import { seedCatalog } from './catalog.js';
import { PodcastActions } from './actions.js';

const connectionString = process.env.DATABASE_URL;
if (!connectionString) throw new Error('Set DATABASE_URL to a dedicated PostgreSQL sample database.');
const subscrio = new Subscrio({ database: { connectionString } });

try {
  if (await subscrio.verifySchema() === null) await subscrio.installSchema();
  await seedCatalog(subscrio);
  const actions = new PodcastActions(subscrio);
  const free = 'neighborhood-radio';
  const collective = 'harbor-network';
  const visitor = 'new-publisher';

  for (const [customerKey, expected] of [
    [free, [false, 1, 'standard']],
    [collective, [true, 6, 'priority']],
    [visitor, [false, 0, 'none']]
  ] as const) {
    const scheduled = await subscrio.featureChecker.isEnabledForCustomer(customerKey, 'castcoop', 'scheduled-publishing');
    const limit = await subscrio.featureChecker.getValueForCustomer(customerKey, 'castcoop', 'show-limit', 0);
    const route = await subscrio.featureChecker.getValueForCustomer(customerKey, 'castcoop', 'editorial-route', 'none');
    assert.deepEqual([scheduled, limit, route], expected);
    assert.equal(typeof limit, 'number');
    console.log(`${customerKey}: ${JSON.stringify({ scheduled, limit, route })}`);
  }

  const request = { episodeKey: 'harbor-stories-12', publishAt: '2030-04-06T08:00:00Z' };
  const denied = await actions.scheduleEpisode(free, request);
  assert.deepEqual(denied, { error: 'scheduled_publishing_not_included' });
  assert.equal(actions.scheduled.length, 0);
  console.log(`Free scheduling: ${JSON.stringify(denied)}`);
  const queued = await actions.scheduleEpisode(collective, request);
  assert.deepEqual(queued, { status: 'queued', ...request });
  assert.equal(actions.scheduled.length, 1);
  console.log(`Collective scheduling: ${JSON.stringify(queued)}`);

  assert.deepEqual(await actions.createShow(free, 'Neighborhood Notes'), { status: 'created', title: 'Neighborhood Notes', limit: 1, used: 1 });
  const full = await actions.createShow(free, 'Second Show');
  assert.deepEqual(full, { error: 'show_limit_reached', limit: 1, used: 1 });
  console.log(`Free second show: ${JSON.stringify(full)}`);
  for (let i = 1; i <= 6; i++) assert.equal((await actions.createShow(collective, `Show ${i}`)).status, 'created');
  assert.deepEqual(await actions.createShow(collective, 'Seventh Show'), { error: 'show_limit_reached', limit: 6, used: 6 });
  assert.equal(actions.shows.length, 7);

  for (const [customerKey, queue] of [[free, 'standard'], [collective, 'priority']] as const) {
    const result = await actions.requestEditorialHelp(customerKey, 'Review our trailer');
    assert.deepEqual(result, { status: 'queued', queue, subject: 'Review our trailer' });
    console.log(`${customerKey} editorial: ${JSON.stringify(result)}`);
  }
  assert.deepEqual(await actions.scheduleEpisode(visitor, request), { error: 'scheduled_publishing_not_included' });
  assert.deepEqual(await actions.createShow(visitor, 'No plan'), { error: 'show_limit_reached', limit: 0, used: 0 });
  assert.deepEqual(await actions.requestEditorialHelp(visitor, 'Help'), { error: 'editorial_support_not_included' });
  assert.equal(actions.scheduled.length, 1);
  assert.equal(actions.editorial.length, 2);
  console.log('PASS: toggle, numeric and text results; show boundaries; no-subscription defaults.');
} finally {
  await subscrio.close();
}

