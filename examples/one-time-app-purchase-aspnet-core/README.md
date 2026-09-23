# How to sell your entire ASP.NET Core app for a one-time payment with Subscrio

ThreadDraft is an embroidery-design application sold for a single payment. Buying it unlocks pattern creation, editing, and export together. There is no monthly plan to renew and no separate export upgrade.

In this guide we will represent a one-time app purchase in ASP.NET Core, fulfill a confirmed order once, and check the whole-app entitlement in each tool endpoint.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, SQL Server Express LocalDB, and the default MSSQLLocalDB instance. Install LocalDB with SQL Server Express. The connection uses Windows integrated authentication; no SQL password is needed.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated name>;Integrated Security=true;TrustServerCertificate=true`.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```powershell
git clone https://github.com/subscrio/samples.git
cd samples/examples/one-time-app-purchase-aspnet-core
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore
```

## Expected results

```text
Purchase replay keeps agreement: "purchase-order-thread-101"
Whole-app agreements: 1
design purchaser HTTP: 200
design visitor HTTP: 403
edit purchaser HTTP: 200
edit visitor HTTP: 403
export purchaser HTTP: 200
export visitor HTTP: 403
Access on a later visit: true
PASS: ThreadDraft behavior verified.
```

A confirmed order creates one ongoing agreement. Replaying that order keeps the same agreement, all three paid tools return HTTP 200 for the purchaser, and a customer without the purchase receives HTTP 403. A later visit uses the same entitlement without a renewal event.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The HTTP server binds to an ephemeral loopback port, sends its own paid and unpaid requests, then shuts down. Its customer routes are a local test fixture. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.
