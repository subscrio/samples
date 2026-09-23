# How to sell your entire TypeScript app for a one-time payment with Subscrio

RidgeAtlas sells its complete hiking planner for one payment. A purchaser can draw routes, inspect elevation profiles, and build itineraries. When that customer moves from a laptop to a phone, the purchase should follow them instead of prompting for another payment.

In this guide we will model a one-time app purchase in TypeScript and use one customer entitlement for every planning tool, including a later visit from another device.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. The development role must have CREATEDB permission. Copy .env.example to .env and enter a local development connection; do not use production credentials.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```shell
git clone https://github.com/subscrio/samples.git
cd samples/examples/one-time-app-purchase-typescript
cp .env.example .env
# Set DATABASE_URL in .env before running.
npm ci
npm test
```

## Expected results

```text
Laptop tools: ["route","elevation","itinerary"]
Visitor tools: []
Phone tools on later visit: ["route","elevation","itinerary"]
Original purchase customer: "customer"
PASS: RidgeAtlas behavior verified.
```

The purchase belongs to the customer, so moving to another device does not change which tools are available. The original agreement still provides route drawing, elevation profiles, and itineraries on the later visit. There is no recurring credit allocation or region-specific add-on to maintain.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.
