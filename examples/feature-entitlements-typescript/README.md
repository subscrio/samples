# Feature entitlements in TypeScript

CastCoop is a fictional community-podcast application. This Node demo uses Subscrio and PostgreSQL to make three separate decisions: accept a scheduled episode, allow another show, and choose an editorial-support queue.

| Plan | Scheduled publishing | Shows | Editorial route |
| --- | --- | --- | --- |
| Free | Disabled | 1 | standard |
| Collective | Enabled | 6 | priority |
| No subscription | Disabled | 0 | none |

Companion article: "How to add feature entitlements to a TypeScript app with Subscrio" (website draft; not yet published).

## Prerequisites and configuration

Install Node.js and PostgreSQL. Create a dedicated, empty database called `castcoop_demo`, owned by the database user you will connect as. The example installs Subscrio's schema and creates its catalog in this database. It never drops the database or schema. Use it only with a disposable development database.

From this folder, install dependencies and copy the environment template:

```bash
npm ci
cp .env.example .env
```

In PowerShell, use `Copy-Item .env.example .env` for the copy command. Edit `.env` and replace the placeholders with your local database connection details:

```dotenv
DATABASE_URL=postgresql://USERNAME:PASSWORD@localhost:5432/castcoop_demo
```

The connection stays in the Node process. `.env` is ignored by Git. Dependency pins and the lockfile define the tested package set.

## Run the example

```bash
npm run demo
```

The runner installs the schema if it is absent, seeds the example catalog, calls the three backend actions, and checks their results. It exits with an error if an assertion fails.

```text
neighborhood-radio: {"scheduled":false,"limit":1,"route":"standard"}
harbor-network: {"scheduled":true,"limit":6,"route":"priority"}
new-publisher: {"scheduled":false,"limit":0,"route":"none"}
Free scheduling: {"error":"scheduled_publishing_not_included"}
Collective scheduling: {"status":"queued","episodeKey":"harbor-stories-12","publishAt":"2030-04-06T08:00:00Z"}
Free second show: {"error":"show_limit_reached","limit":1,"used":1}
neighborhood-radio editorial: {"status":"queued","queue":"standard","subject":"Review our trailer"}
harbor-network editorial: {"status":"queued","queue":"priority","subject":"Review our trailer"}
PASS: toggle, numeric and text results; show boundaries; no-subscription defaults.
```

The timestamp is a fixed demonstration input. Queued entries live in memory; no episode is actually published and no support request is sent.

## Checks and source

```bash
npm run check
npm run demo
```

- `catalog.ts` defines the product, features, plans, cycles, customers, and subscriptions. Running it again reuses existing records and restores the example plan values.
- `actions.ts` contains the scheduling, show-creation, and support-routing methods. Each reads the customer's entitlement before accepting work.
- `demo.ts` verifies exact return values and types, the first and excess show under each plan, no-subscription denials, and the resulting in-memory records.

The numeric fallback `0` makes the numeric getter return a number. The text getter returns the route string; the application explicitly recognizes `standard` and `priority`. It denies an unknown route instead of treating arbitrary text as a valid queue.

## Reruns and limits

Subscrio records persist in PostgreSQL. Show counts and queues reset with the process, so repeated runs produce the same output. The example does not migrate an older schema automatically; start with an empty sample database or follow Subscrio's migration documentation.

The arrays demonstrate server-side decisions in one process; they are not persistent queues, a scheduling worker, or a production show store. Billing cycles describe agreements and do not collect payments.

The verification workflow runs on Node.js 22 with PostgreSQL 17. The sample uses the public `subscrio` package pinned in `package.json`; package changes require rerunning the checks.

