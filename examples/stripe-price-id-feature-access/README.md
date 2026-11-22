# Keep feature access stable when Stripe prices change

This pair of examples maps two Stripe Price IDs to separate Subscrio billing cycles under one Studio plan. Both Studio customers can export CSV, while a Free customer is denied. The TypeScript version uses a Next.js App Router Route Handler; the C# version uses an ASP.NET Core Minimal API.

The Stripe IDs are placeholders. The samples do not contact Stripe, create a Checkout Session, or process webhooks. They demonstrate the Subscrio catalog mapping and the application feature check. The [Stripe renewal sample](../stripe-subscription-renewals) demonstrates Stripe event processing.

## Prerequisites

- Node.js 22 or newer for the Next.js sample.
- .NET SDK 10 for the ASP.NET Core sample.
- PostgreSQL 17 or newer with permission to create tables in a dedicated, empty database.

Create a separate database for each language so both demos can use the same stable sample keys without colliding. Do not use a production database.

## TypeScript with Next.js

From `examples/stripe-price-id-feature-access/typescript`:

```bash
npm ci
cp .env.example .env
```

Set `DATABASE_URL` in `.env` to the dedicated PostgreSQL database. Then run:

```bash
npm run seed
npm run dev
```

Open these fixed demo endpoints:

```text
http://localhost:3000/api/demo/export/legacy-team
http://localhost:3000/api/demo/export/current-team
http://localhost:3000/api/demo/export/free-team
```

The two Studio routes return CSV with HTTP 200. The Free route returns `export_not_included` with HTTP 403.
A key outside the three fixed demo customers returns HTTP 404.

## C# with ASP.NET Core Minimal API

From `examples/stripe-price-id-feature-access/dotnet`, set `DATABASE_URL` to the separate database. For PowerShell:

```powershell
$env:DATABASE_URL = 'Host=localhost;Port=5432;Database=feedbackdesk_dotnet;Username=postgres;Password=local-only'
dotnet restore --locked-mode
dotnet run --no-restore -- --verify
```

The verification starts the Minimal API and checks the real HTTP responses for the legacy, current, and Free customers. To run the API interactively, use `dotnet run --no-restore` and visit `http://127.0.0.1:5079/api/demo/export/legacy-team`.

Expected verification output:

```text
legacy-team: HTTP 200
current-team: HTTP 200
free-team: HTTP 403
not-a-demo-customer: HTTP 404
PASS: legacy and current Stripe price mappings allow export; Free is denied.
```

## Repeat runs and production boundaries

Seeding is idempotent for the sample's fixed keys. The .NET `--verify` path checks its HTTP responses and exits. The Next.js route is built by `npm test`; use the three URLs above to check its responses after seeding.

The demo route accepts fixed customer keys only to make the checks easy to reproduce. In a deployed application, authenticate the caller and derive the Subscrio customer key from trusted account membership before checking a feature. The examples do not define cancellation, refunds, failed-payment grace periods, or account authorization policy. Creating a local subscription in Subscrio does not charge a customer.
