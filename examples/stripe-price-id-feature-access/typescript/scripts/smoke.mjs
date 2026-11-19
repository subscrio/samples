import assert from 'node:assert/strict';

const origin = process.env.SAMPLE_ORIGIN ?? 'http://127.0.0.1:3000';
for (const [key, status, body] of [
  ['legacy-team', 200, 'feedback,votes'],
  ['current-team', 200, 'feedback,votes'],
  ['free-team', 403, 'export_not_included'],
  ['not-a-demo-customer', 404, 'demo_customer_not_found'],
]) {
  const response = await fetch(`${origin}/api/demo/export/${key}`);
  const text = await response.text();
  assert.equal(response.status, status, `${key}: unexpected HTTP status`);
  assert.ok(text.includes(body), `${key}: unexpected body ${text}`);
  console.log(`${key}: HTTP ${status}`);
}
console.log('PASS: Next.js Route Handler checks both price mappings and the Free denial.');
