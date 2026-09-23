# How to sell prepaid credit packs with Subscrio in C#

SliceFoundry prepares 3D models for printing. Customers can use the application without a recurring paid plan, then buy a pack of 500 credits when they need slicing work. Each slice costs five credits. A repeated payment notification must not grant another pack.

In this guide we will sell prepaid credit packs in C#, record one confirmed purchase, and spend credits with separate keys for the order and the slicing job.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, SQL Server Express LocalDB, and the default MSSQLLocalDB instance. Install LocalDB with SQL Server Express. The connection uses Windows integrated authentication; no SQL password is needed.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated name>;Integrated Security=true;TrustServerCertificate=true`.

Subscrio 0.5.1 is pinned in the project and dependency lock file.

## Run

```powershell
git clone https://github.com/subscrio/samples.git
cd samples/examples/prepaid-credit-packs-csharp
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore
```

## Expected results

```text
Credits after payment replay: 500
Credits after slicing: 495
Credits after job retry: 495
PASS: SliceFoundry behavior verified.
```

The order key protects the 500-credit grant; the job key protects the five-credit charge. They remain separate because buying capacity and spending it are separate operations. SliceFoundry ends with 495 credits after both retries, with no paid recurring subscription involved.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The paid order is a fixture. Verify real payment confirmation before granting purchased credits. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.
