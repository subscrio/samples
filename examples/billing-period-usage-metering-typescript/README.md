# How to meter usage by subscription billing period with Subscrio in TypeScript

SubtitleDock processes subtitle tracks for independent film distributors. A customer buys 600 minutes of processing for each subscription billing period, which can start halfway through a calendar month. Counting against the first day of the month would reset the allowance on the wrong date.

In this guide we will meter processing minutes in TypeScript using explicit subscription period dates, then handle the boundary where those dates need to be refreshed.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. The development role must have CREATEDB permission. Copy .env.example to .env and enter a local development connection; do not use production credentials.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```shell
git clone https://github.com/subscrio/samples.git
cd samples/examples/billing-period-usage-metering-typescript
cp .env.example .env
# Set DATABASE_URL in .env before running.
npm ci
npm test
```

## Expected results

```text
Charged minutes: 17
Consumed minutes: 17
Minutes remaining: 583
Stale period rejected: true
Renewed consumption: 0
Renewed allowance: 600
Original job snapshot: 17
Current period still unused: 0
PASS: SubtitleDock behavior verified.
```

The first job spends seventeen minutes from its subscription period. An expired period definition raises an error until the billing dates are updated. After renewal, the current bucket is empty even though a retry can still return the original job's seventeen-minute snapshot. Use a fresh usage read for the current balance.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.
