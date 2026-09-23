# How to grant feature overrides that expire automatically with Subscrio in C#

BadgeHarbor runs accreditation desks for a film festival. The venue normally has 12 desks and an eight-desk add-on, but opening weekend needs room for 80. The temporary increase should end at the agreed closing time without someone remembering to remove it.

In this guide we will apply a time-limited feature override in C#, test its expiry boundary, and confirm that the ordinary plan and add-on allowance returns.

Article status: unpublished website draft. The public article link will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, SQL Server Express LocalDB, and the default MSSQLLocalDB instance. Install LocalDB with SQL Server Express. The connection uses Windows integrated authentication; no SQL password is needed.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated name>;Integrated Security=true;TrustServerCertificate=true`.

Subscrio 0.5.0 is pinned in the project and dependency lock file.

## Run

```powershell
git clone https://github.com/subscrio/samples.git
cd samples/examples/timed-feature-overrides-csharp
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore
```

## Expected results

```text
Normal desks: 20
Festival desks: 80
Desks before closing: 80
Desks at closing: 20
Retained override records: 1
PASS: BadgeHarbor behavior verified.
```

At the closing instant, the allowance returns to the plan's twelve desks plus the eight-desk pack. The expired override remains stored, but it no longer contributes to the decision. This lets BadgeHarbor promise a precise end time without making the access change depend on cleanup.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup validates that generated name before removing the database. The assertions exit with an error when a result differs. Run the command again to repeat the checks.

Creating a Subscrio billing cycle or agreement does not collect a payment. The application remains responsible for the external work initiated after an accepted entitlement or credit decision.

To use an existing SQL Server instance instead of LocalDB, set `SUBSCRIO_SAMPLE_SQLSERVER` (for example, `localhost`). The sample uses Windows authentication and needs permission to create its sample database.
