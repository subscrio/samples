# Retire a subscription add-on while preserving existing access in C#

CurtainPass stops selling its old two-lane scanning pack. A theatre that already bought two packs still expects five lanes to work: one from the venue plan and four from the old purchase. Removing the contribution would turn catalog cleanup into an access outage.

In this guide we will retire a subscription add-on in C#, preserve an existing venue's capacity, and reject a new purchase of the archived package.

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
Existing venue before retirement: 5
Existing venue after retirement: 5
New purchase rejected: true
New venue allowance: 1
After explicit detachment: 1
Delete with history rejected: true
PASS: CurtainPass behavior verified.
```

Catalog retirement leaves the existing venue at five lanes and blocks the attempted new purchase. Explicitly detaching the existing purchase reduces that venue to one lane. The final deletion attempt is rejected because attachment history still refers to the package.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.
