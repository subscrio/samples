# How to add subscription capacity packs with Subscrio in C#

A recurring warehouse subscription includes four docks. Each purchased capacity pack adds two docks; replacing the quantity and removing the packs changes the resolved allowance.

The matching website article is an unpublished draft; its URL will be added after publication.

## Run the sample

Install the .NET SDK and SQL Server Express LocalDB on Windows. The sample uses Windows integrated authentication and the MSSQLLocalDB instance.

Tested with .NET SDK 10.0.302, SQL Server Express LocalDB, Subscrio.Core 0.5.1. Dependencies are pinned in the lock file. From this folder:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode --source https://api.nuget.org/v3/index.json
dotnet run --no-restore
```

## Expected output

```text
Base docks: 4
Three packs: 10
One pack: 6
One pack repeated: 6
Detached: 4
PASS: DockFlow behavior verified.
```

The assertions exit with a nonzero status if any result differs. Each run creates a uniquely named disposable database and removes only that database on completion. You can rerun the commands without resetting your existing databases.

Purchases are explicit test fixtures. Your application must confirm payment before attaching purchased add-ons. The runner verifies attachment and removal; repeated quantity writes must preserve the expected allowance.
