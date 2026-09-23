# How to combine entitlements from multiple subscriptions with Subscrio in TypeScript

ExhibitLoop manages screens for a museum with two independently funded exhibitions. One agreement includes four display slots and another includes seven. The museum expects eleven usable slots, but that result depends on how values from multiple subscriptions are combined.

In this guide we will compare additive and most-generous entitlement resolution in TypeScript using the same two agreements, then inspect where the final value came from.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. The development role must have CREATEDB permission. Copy .env.example to .env and enter a local development connection; do not use production credentials.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```shell
git clone https://github.com/subscrio/samples.git
cd samples/examples/combine-subscription-entitlements-typescript
cp .env.example .env
# Set DATABASE_URL in .env before running.
npm ci
npm test
```

## Expected results

```text
Most generous display slots: 7
Combined display slots: 11
Explained value: "11"
Contributing values: ["4","7"]
PASS: ExhibitLoop behavior verified.
```

The same four and seven slots can legitimately resolve to seven or eleven. ExhibitLoop makes that choice explicit on the product-feature association, then uses the explanation to show the contribution from each agreement. Choose the rule that matches what the customer purchased before relying on the final number.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.
