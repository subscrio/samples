# How to enforce monthly usage quotas with Subscrio in C#

BeamCheck validates structural models before engineers submit them for review. Its Studio subscription includes 250 validations each calendar month. An allowance of 250 is only useful if the application counts accepted jobs and refuses the next one, including when a caller retries an earlier request.

In this guide we will enforce a monthly usage quota in C#, report each validation with a stable job key, and inspect the usage that Subscrio returns.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, SQL Server Express LocalDB, and the default MSSQLLocalDB instance. Install LocalDB with SQL Server Express. The connection uses Windows integrated authentication; no SQL password is needed.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated name>;Integrated Security=true;TrustServerCertificate=true`.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```powershell
git clone https://github.com/subscrio/samples.git
cd samples/examples/monthly-usage-quotas-csharp
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore
```

## Expected results

```text
Monthly limit: 250
Initially consumed: 0
Initially remaining: 250
Accepted validations: 250
Remaining validations: 0
Consumed after retry: 250
Next validation rejected: true
Next month consumed: 0
Next month remaining: 250
PASS: BeamCheck behavior verified.
```

The allowance now answers two separate questions: how much the customer has used, and whether the next validation can be admitted. The 251st new job fails, an existing job key does not spend twice, and the next calendar month starts with a fresh bucket. Keep the stable job key when connecting this admission step to the real validator.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.
