# Test subscription access at expiration boundaries in C#

InspectRoute is an app for inspection firms to prepare field reports. An InspectRoute customer's report-generation contract ends at noon. A request just before noon should pass; a request at noon should fail. Testing a whole day before and after the deadline can miss the equality bug that matters.

In this guide we will test subscription expiration boundaries in C#. The customer's existing reports stay in application storage, while a separate entitlement controls creation of new reports.

Article status: unpublished draft. This sample uses the released package and was verified locally; publication and remote CI for this revision are pending.

## Prerequisites

Windows, .NET SDK 10.0.302, and SQL Server Express LocalDB. Install LocalDB through the SQL Server Express installer, then start MSSQLLocalDB. The sample uses Windows integrated authentication; no SQL login or password is required.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated identifier>;Integrated Security=true;TrustServerCertificate=true`. The database name is generated for each run.

Dependency: Subscrio.Core 0.5.1, pinned in the project and lock file.

## Run

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore
```


## Verified output

```text
Before expiry: true
At expiry: false
After expiry: false
Archived reports retained: 1
Subscription value alone: true
PASS: InspectRoute behavior verified.
```

The customer-level check changes from true to false exactly at expiration. The last subscription-level getter still returns true because it reads the value attached to that subscription rather than proving current customer eligibility. Use the customer-level decision when admitting a new report.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.
