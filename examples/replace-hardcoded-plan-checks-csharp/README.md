# Replace hardcoded plan checks with feature entitlements in C#

SeedShelf's CSV export works for Professional customers. Then a seed library consortium buys an Institutional plan. It includes the same capability, but the endpoint still checks whether the plan name is professional. A paying customer receives a denial because the offer has a new name.

In this guide we will replace that condition with a CSV export entitlement in C#. Both paid offerings will grant export, while Community continues to deny it.

Article status: unpublished draft. The public article URL will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, and SQL Server Express LocalDB. Install LocalDB through the SQL Server Express installer, then start MSSQLLocalDB. The sample uses Windows integrated authentication; no SQL login or password is required.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated identifier>;Integrated Security=true;TrustServerCertificate=true`. The database name is generated for each run.

Subscrio dependency: 0.5.1, pinned in the project and lock file.

## Run

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore
dotnet run --no-restore
```

## Verified output

```text
Old professional: true
Old institutional: false
Community export: "export_not_included"
Professional export: "seed,quantity\nBean,12"
Institutional export: "seed,quantity\nBean,12"
PASS: SeedShelf behavior verified.
```

The Institutional export now returns the same CSV content as Professional. Community still returns export_not_included. Adding another qualifying plan means assigning its csv-export value; the export action can stay unchanged.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.
