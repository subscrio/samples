# How to handle automated subscription renewals with Stripe and Subscrio

MooringDesk follows one agreement through renewal, failed payment, recovery, cancellation, duplicates, and delayed delivery. Article status: unpublished website draft. Its public URL will be added after publication.

## Current status

TypeScript passes with public Subscrio 0.5.0 and Stripe 22.5.0. The pending C# implementation exposes a UTC materialization bug in public Subscrio.Core 0.5.0: subscription DTO period dates shift by the machine timezone after SQL Server reads. The exact-date assertion intentionally detects it. A tested UTC mapping fix passes this runner against LocalDB, but the library release is pending. **C# is not ready for public execution.** Its implementation remains with the unpublished article draft until the dependency fix is released. Keep the exact-date assertion; do not substitute the shifted date.

## TypeScript

Tested: Node.js 24.11.1, PostgreSQL 17, TypeScript 6.0.2, Subscrio 0.5.0, Stripe 22.5.0. From the typescript folder:

```shell
cp .env.example .env
# Set DATABASE_URL to a local development role with CREATEDB permission.
npm ci
npm test
```

## C#

Tested: .NET SDK 10.0.302, Stripe.net 52.3.0, and SQL Server Express LocalDB on Windows with integrated authentication. Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated name>;Integrated Security=true;TrustServerCertificate=true`. Install LocalDB through SQL Server Express.

The C# source and local-project verification remain with the unpublished article draft. A runnable public project and setup commands will be added after the dependency fix is released and pinned.

## Verified lifecycle output

C# output below uses the pending library fix. TypeScript passes the same checks with public dependencies, including milliseconds in ISO timestamps.

```text
Invalid signature HTTP: 400
Receipts after invalid signature: 0
Successful renewal HTTP: 200
Successful renewal access: true
Successful renewal period end: "2026-11-23T16:10:51Z"
Successful renewal replay: "duplicate"
Subscription update HTTP: 200
Subscription update access: true
Subscription update period end: "2026-11-23T16:10:51Z"
Subscription update replay: "duplicate"
Failed renewal HTTP: 200
Failed renewal access: false
Failed renewal period end: "2026-12-23T16:10:51Z"
Failed renewal replay: "duplicate"
Payment recovery HTTP: 200
Payment recovery access: true
Payment recovery period end: "2026-12-23T16:10:51Z"
Payment recovery replay: "duplicate"
Cancellation HTTP: 200
Cancellation access: false
Cancellation period end: "2026-12-23T16:10:51Z"
Cancellation replay: "duplicate"
Linked agreement count: 1
Replay after worker restart: "duplicate"
Delayed event HTTP: 200
Delayed event preserves cancellation: false
price failure HTTP: 500
price repair HTTP: 200
customer failure HTTP: 500
customer repair HTTP: 200
Recovery after processing repair: true
PASS: MooringDesk renewal behavior verified.
```

## Optional sandbox capture

Fixtures are reduced responses from a real Stripe test-clock run on September 23, 2026. Provider IDs are fictional replacements. Runners generate local test signatures, not captured Stripe signatures. No Stripe key or charge is required for the default tests.

Install the TypeScript dependencies, set STRIPE_SECRET_KEY to a sandbox key in your shell, then run from this folder:

```shell
node capture-sandbox.cjs
```

The script rejects live keys. It creates a fictional customer, test clock, monthly price and subscription; exercises paid renewal, failure, recovery, cancellation; deletes the clock; and archives its price and product. It updates fixtures.json with reduced responses. The raw .sandbox-capture.json is ignored by Git. Pinned Stripe API: 2026-07-29.dahlia. Dependencies are locked in each project.

## Reruns and processing

Each runner creates a disposable database, starts a loopback server on an ephemeral port, sends its own HTTP requests, shuts down, and drops only its generated database. Assertions return a nonzero exit code on failure.

The worker verifies raw-body signatures, checks durable receipts, retrieves current billing state, forwards matching paid invoices, and reconciles the current subscription. Payment eligibility and the receipt commit together after processing. Failures remain retriable; worker restart preserves receipt checks within the same database.

Multiple worker processes need a distributed per-subscription lock covering retrieval through receipt commit. External effects are not covered. MooringDesk denies new work while the latest invoice is unpaid; core does not handle invoice.payment_failed or implement a grace period. Live retrieval adapters are included; default tests inject recorded sandbox responses.
