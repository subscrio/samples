# How to sell optional feature bundles with Subscrio in TypeScript

A recurring subscription includes 20 galleries. An optional Presentation bundle enables branding and private selections without changing that allowance.

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
Before purchase: {"branding":false,"selections":false,"galleries":20}
Bundle attached: {"branding":true,"selections":true,"galleries":20}
Bundle detached: {"branding":false,"selections":false,"galleries":20}
PASS: VowGallery behavior verified.
```

The assertions exit with a nonzero status if any result differs. Each run creates a uniquely named disposable database and removes only that database on completion. You can rerun the commands without resetting your existing databases.

Purchases are explicit test fixtures. Your application must confirm payment before attaching the bundle. The runner verifies both feature toggles before purchase, after attachment, and after removal, including the unchanged gallery allowance.
