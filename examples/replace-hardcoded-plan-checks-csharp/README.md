# Replace hardcoded plan checks with feature entitlements in C#

FeedbackDesk helps software teams collect and prioritize customer feedback. CSV export works for Pro customers. When a larger team buys the new Enterprise plan, its product manager cannot export feedback for a planning meeting: the export method still checks whether the plan name is pro, even though Enterprise includes exports.

In this guide we will replace that condition with a CSV export entitlement in C#. Both paid offerings will grant export, while Free continues to deny it.

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
Before the fix, Pro export is allowed.
Before the fix, Enterprise export is denied.
Free customer: export denied, as expected.
Pro customer: feedback CSV returned, as expected.
Enterprise customer: feedback CSV returned, as expected.
PASS: FeedbackDesk export access verified.
```

The Enterprise export now returns the same CSV content as Pro. Free still returns export_not_included. Adding another qualifying plan means assigning its csv-export value; the export action can stay unchanged.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.

