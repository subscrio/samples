import 'dotenv/config';
import assert from 'node:assert/strict';
import { Subscrio } from 'subscrio';
import { seedCatalog } from './catalog.js';
import { PodcastActions, type Actor } from './actions.js';

const connectionString = process.env.DATABASE_URL;
if (!connectionString) throw new Error('Set DATABASE_URL to a dedicated PostgreSQL sample database.');
const subscrio = new Subscrio({ database: { connectionString } });

try {
  if (await subscrio.verifySchema() === null) await subscrio.installSchema();
  await seedCatalog(subscrio);
  const actions = new PodcastActions(subscrio);
  const permissions = ['publish', 'manage-shows', 'request-help'];
  const free: Actor = { customerKey: 'neighborhood-radio', permissions };
  const collective: Actor = { customerKey: 'harbor-network', permissions };
  const visitor: Actor = { customerKey: 'new-publisher', permissions };

  for (const [actor, expected] of [
    [free, [false, 1, 'standard']],
    [collective, [true, 6, 'priority']],
    [visitor, [false, 0, 'none']]
  ] as const) {
    const scheduled = await subscrio.featureChecker.isEnabledForCustomer(actor.customerKey, 'castcoop', 'scheduled-publishing');
    const limit = await subscrio.featureChecker.getValueForCustomer(actor.customerKey, 'castcoop', 'show-limit', 0);
    const route = await subscrio.featureChecker.getValueForCustomer(actor.customerKey, 'castcoop', 'editorial-route', 'none');
    assert.deepEqual([scheduled, limit, route], expected);
    assert.equal(typeof limit, 'number');
    console.log(`${actor.customerKey}: ${JSON.stringify({ scheduled, limit, route })}`);
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

  for (const [actor, queue] of [[free, 'standard'], [collective, 'priority']] as const) {
    const result = await actions.requestEditorialHelp(actor, 'Review our trailer');
    assert.deepEqual(result, { status: 'queued', queue, subject: 'Review our trailer' });
    console.log(`${actor.customerKey} editorial: ${JSON.stringify(result)}`);
  }
  assert.deepEqual(await actions.scheduleEpisode(visitor, request), { error: 'scheduled_publishing_not_included' });
  assert.deepEqual(await actions.createShow(visitor, 'No plan'), { error: 'show_limit_reached', limit: 0, used: 0 });
  assert.deepEqual(await actions.requestEditorialHelp(visitor, 'Help'), { error: 'editorial_support_not_included' });
  const viewer: Actor = { customerKey: collective.customerKey, permissions: [] };
  assert.deepEqual(await actions.scheduleEpisode(viewer, request), { error: 'permission_denied' });
  assert.deepEqual(await actions.createShow(viewer, 'No permission'), { error: 'permission_denied' });
  assert.deepEqual(await actions.requestEditorialHelp(viewer, 'No permission'), { error: 'permission_denied' });
  assert.equal(actions.scheduled.length, 1);
  assert.equal(actions.editorial.length, 2);
  console.log('PASS: toggle, numeric and text results; show boundaries; no-subscription defaults; user permissions.');
} finally {
  await subscrio.close();
}
