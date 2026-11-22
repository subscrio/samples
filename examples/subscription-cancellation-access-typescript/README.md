# Handle subscription cancellation without ambiguous access rules in TypeScript

PairClass promises an instructor that cancellation stops the next renewal while the current course can continue until Friday. Two operations are involved: opening the live classroom and consuming a lesson allowance. A future cancellation date does not necessarily affect them in the same way.

In this guide we will compare those TypeScript operations, then configure an explicit classroom access policy that matches the promise. We will test before and at the cancellation timestamp.

Article status: unpublished draft. This sample uses the released package and was verified locally; publication and remote CI for this revision are pending.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. Copy .env.example to .env and replace the placeholders with your local development connection. The role needs CREATEDB permission. Never use a production connection.

Dependency: subscrio 0.5.0, pinned in the project and lock file.

## Run

```shell
cp .env.example .env
# Edit DATABASE_URL in .env.
npm ci

npm test
```


## Verified output

```text
Default classroom check: false
Lesson accepted before cancellation: true
Lessons consumed: 1
Explicit classroom check: true
Classroom at cancellation: false
Accounting access at cancellation: false
New lesson after cancellation: rejected
PASS: PairClass behavior verified.
```

With the explicit classroom policy, the instructor can keep teaching before the scheduled cancellation. At the timestamp, the classroom check and the metering access result are both false, and a new usage report is rejected. The hasAccess field describes eligibility; it is not a count of remaining lessons.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.
