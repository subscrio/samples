# How to schedule subscription activation with Subscrio in TypeScript

A manually entered agency contract starts in the future. Onboarding remains available, while the subscription permits job submission only at the activation instant.

The matching website article is an unpublished draft; its URL will be added after publication.

## Run the sample

Install Node.js and PostgreSQL. The database role in `DATABASE_URL` must have permission to create databases.

Tested with Node.js 24.11.1, PostgreSQL 17, TypeScript 6.0.2, Subscrio 0.5.0. Dependencies are pinned in the lock file. From this folder:

```shell
cp .env.example .env
# Set DATABASE_URL in .env to your local PostgreSQL connection.
npm ci
npm test
```

## Expected output

```text
Before start: onboarding: "ready"
One millisecond before: "subscription_not_started"
At activation: "accepted"
PASS: LinguaDesk behavior verified.
```

The assertions exit with a nonzero status if any result differs. Each run creates a uniquely named disposable database and removes only that database on completion. You can rerun the commands without resetting your existing databases.

The test moves a controlled clock across the exact activation boundary. It does not charge a customer or wait for real time to pass.
