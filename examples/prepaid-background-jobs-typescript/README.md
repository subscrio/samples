# How to charge prepaid credits for background jobs with Subscrio in TypeScript

RoomRaster turns floor plans into furnished room images. Customers prepay for rendering credits, and each image costs twelve. Near the end of a pack, two background workers may reach the same customer at once. Both can see a promising balance, but only one should spend the last twelve credits.

In this guide we will charge prepaid credits for background jobs in TypeScript, test competing debits, and make an explicit credit correction when an accepted render fails.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. The development role must have CREATEDB permission. Copy .env.example to .env and enter a local development connection; do not use production credentials.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```shell
git clone https://github.com/subscrio/samples.git
cd samples/examples/prepaid-background-jobs-typescript
cp .env.example .env
# Set DATABASE_URL in .env before running.
npm ci
npm test
```

## Expected results

```text
Credits before competing jobs: 12
Accepted jobs: 1
Rejected jobs: 1
Remaining credits: 0
Credits after repeated correction: 12
PASS: RoomRaster behavior verified.
```

Only one worker spends the last twelve credits. When that accepted render fails, one explicit adjustment restores them, and retrying the correction leaves the balance unchanged. Keep the debit, external render, and correction as distinct recorded steps in the job lifecycle.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The test uses concurrent debit calls and an explicit correction; it does not contact a renderer. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.
