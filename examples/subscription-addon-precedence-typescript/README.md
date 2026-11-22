# Resolve competing subscription add-ons in TypeScript

OrbitTile delivers satellite imagery to environmental analysis teams. An OrbitTile customer buys a TIFF export package and a scientific-data package. Both provide an export format. Joining their strings makes no sense, and selecting whichever was attached last makes the result depend on purchase order.

In this guide we will resolve competing TypeScript add-ons with explicit replacement priorities. Scientific export will win while both packages are present, then TIFF will return after that attachment is removed.

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
Base format: "PNG"
TIFF attached: "TIFF"
Both attached: "NetCDF"
Replacement quantity two: rejected
Scientific detached: "TIFF"
All detached: "PNG"
Scientific first, TIFF second: NetCDF still selected
PASS: OrbitTile behavior verified.
```

The final getter follows the remaining sources: PNG, then TIFF, then NetCDF, back to TIFF, and finally PNG. The lower priority number wins between the two replacement packages. Quantity two is rejected because a text setting cannot be multiplied.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.
