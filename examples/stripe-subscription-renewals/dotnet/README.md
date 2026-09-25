# C# Stripe subscription renewals

This .NET application uses SQL Server Express LocalDB and the published Subscrio.Core package. See the [complete setup and expected output](../README.md), including the optional Stripe CLI tunnel.

From this folder on Windows:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode --source https://api.nuget.org/v3/index.json
dotnet run --no-restore
```

The default run uses recorded sandbox fixtures and requires no Stripe credentials. It checks successful renewal, failed payment, recovery, cancellation, signature rejection, duplicates, delayed events, repaired mappings, and exact persisted UTC timestamps. It creates and removes its own LocalDB database.
