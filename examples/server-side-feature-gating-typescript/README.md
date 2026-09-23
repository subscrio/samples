# Enforce paid feature access on the server in TypeScript

BracketCamp helps tournament organizers manage matches and share results. An organizer has hidden the Publish live bracket button for BracketCamp's Club plan. That makes the screen clearer, but a direct request can still reach the server. If the handler publishes without checking the purchase, replaying the request bypasses the restriction.

In this guide we will enforce the live-bracket entitlement in the TypeScript action that accepts publication. We will send real HTTP requests to a local test server and compare Club with Broadcast.

Article status: unpublished draft. The public article URL will be added after publication.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. Copy .env.example to .env and replace the placeholders with your local development connection. The role needs CREATEDB permission. Never use a production connection.

Subscrio dependency: 0.4.0, pinned in the project and lock file.

## Run

```shell
cp .env.example .env
# Edit DATABASE_URL in .env.
npm ci
npm test
```

## Verified output

```text
local-club: HTTP 403 {"error":"live_brackets_not_included"}
regional-open: HTTP 201 {"status":"published"}
PASS: BracketCamp behavior verified.
```

The direct Club request reaches the handler and receives HTTP 403. Broadcast receives HTTP 201, and only that customer appears in the accepted-publications array. The check runs at the action boundary even when the caller skips the screen.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.
