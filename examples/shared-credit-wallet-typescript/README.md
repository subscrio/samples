# How to share a credit wallet across products with Subscrio in TypeScript

SpriteMint sells two tools to game artists: a texture generator and a sprite animator. Customers subscribe to both products but spend one pool of asset credits. Generating a texture costs three credits, and animating a sprite costs eleven.

In this guide we will share a credit wallet across products in TypeScript, fund it through one subscription, and keep each product's access decision separate from the shared balance.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Node.js 24.11.1 and PostgreSQL 17. The development role must have CREATEDB permission. Copy .env.example to .env and enter a local development connection; do not use production credentials.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```shell
git clone https://github.com/subscrio/samples.git
cd samples/examples/shared-credit-wallet-typescript
cp .env.example .env
# Set DATABASE_URL in .env before running.
npm ci
npm test
```

## Expected results

```text
Shared opening balance: 60
Texture job: "accepted"
Animation job: "accepted"
Shared closing balance: 46
Customer wallets: 1
Animation after access ends: "not_included"
Preserved wallet: 46
PASS: SpriteMint behavior verified.
```

Texture generation and animation leave one wallet at 46 credits. Neither the second product nor its subscription creates another funded balance. Each tool resolves its own entitlement before spending, so customers can retain credits while losing access to a particular product.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.
