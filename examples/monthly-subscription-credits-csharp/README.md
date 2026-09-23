# How to grant monthly subscription credits with Subscrio in C#

FolioWorks helps archives turn scanned collections into usable digital material. Its monthly plan includes 1,200 credits: OCR costs two credits per page, while restoring a photograph costs eight. A simple request counter cannot express those different prices.

In this guide we will grant monthly subscription credits in C#, price both operations, and check the balance after a mixed batch of archive work.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, SQL Server Express LocalDB, and the default MSSQLLocalDB instance. Install LocalDB with SQL Server Express. The connection uses Windows integrated authentication; no SQL password is needed.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated name>;Integrated Security=true;TrustServerCertificate=true`.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```powershell
git clone https://github.com/subscrio/samples.git
cd samples/examples/monthly-subscription-credits-csharp
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore
```

## Expected results

```text
Monthly credits: 1200
Repeated grants: 0
Credits after archive work: 1164
Restoration denied: true
Unspent credits preserved: 1164
Credits after next monthly grant: 2364
PASS: FolioWorks behavior verified.
```

The mixed batch costs 36 credits, leaving 1,164. A later monthly grant adds another 1,200 because this plan retains unused credits. Restoration can still be denied while that wallet is funded: a credit balance pays for an operation but does not decide which features the customer owns.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.
