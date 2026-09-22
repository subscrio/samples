# Feature entitlements in ASP.NET Core

KilnBook is a fictional pottery-studio booking app. This example uses Subscrio to decide whether an account can create recurring bookings and how many active reservations it can hold.

| Plan | Active reservations | Recurring bookings |
| --- | ---: | --- |
| Basic | 2 | No |
| Studio | 8 | Yes |

The application checks user permissions before checking customer entitlements. It then uses a SQL Server transaction to keep concurrent requests from exceeding the reservation allowance.

Companion article: "How to add feature entitlements to an ASP.NET Core app with Subscrio" (in editorial review).

## Prerequisites

- Windows with the .NET 10 SDK.
- Microsoft SQL Server Express LocalDB, with an instance named `MSSQLLocalDB`. Install LocalDB through the SQL Server Express installer or the Visual Studio Installer's individual components.
- Internet access for the first NuGet restore.

The sample pins `Subscrio.Core` to 0.4.0 and includes `packages.lock.json`. It uses Windows integrated authentication, so no database password is needed. The connection-string format is shown in `appsettings.example.json`; the executable builds it in `LocalDatabase.cs` and does not read that reference file.

```powershell
sqllocaldb info
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode --source https://api.nuget.org/v3/index.json
```

If the instance is missing, create it once with `sqllocaldb create MSSQLLocalDB`.

## Run the complete verification

From this folder:

```powershell
dotnet run --no-restore -- --verify
```

This starts a real HTTP server on a random loopback port, creates an isolated LocalDB database, makes requests, checks the stored reservation counts, and removes that generated database afterward. A failed assertion exits with an error. It does not remove the database used for manual testing.

Expected output:

```text
PASS Unauthenticated request: 401
PASS Studio viewer lacks booking permission: 403
PASS No subscription has zero allowance: 403
PASS Basic recurring booking denied: 403
PASS Basic first reservation: 201
PASS Two requests for one remaining Basic slot: 201 + 409; stored count 2
PASS Basic full capacity: 409
PASS Studio recurring booking allowed: 201
PASS Studio ninth reservation denied: 409
PASS Stored counts: Basic 2, Studio 8, no-subscription account 0
All HTTP and database checks passed.
```

## Try the API manually

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --no-restore
```

The server listens on `http://127.0.0.1:5078` and uses the `KilnBookEntitlements` LocalDB database. It seeds the catalog and accounts if they are missing. Reservations persist between manual runs.

In another PowerShell window, make an allowed request:

```powershell
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5078/reservations `
  -Headers @{ Authorization = 'Bearer studio-owner' } `
  -ContentType application/json -Body '{"recurring":true}'
```

Change the identity to `basic-owner` and the same recurring request returns 403. These fixed bearer strings are test identities, not JWTs or production credentials.

| Test identity | Customer | User permission |
| --- | --- | --- |
| `basic-owner` | `clay-room`, Basic plan | Create reservations |
| `studio-owner` | `river-studio`, Studio plan | Create reservations |
| `studio-viewer` | `river-studio`, Studio plan | Read-only user; booking denied |
| `visitor-owner` | `visitor`, no subscription | Create permission, but no reservation allowance |

The request body contains only `recurring`. The customer key comes from the authenticated identity fixture. A real application should obtain that claim from its existing trusted identity and account-membership system. The sample refuses to start outside Development and binds only to loopback.

## Read the code

- `Catalog.cs` defines features, plans, billing cycles, customers, and subscriptions.
- `Program.cs` registers Subscrio with the SQL Server provider and protects the booking endpoint.
- `ReservationStore.cs` locks an account row, counts active reservations, and inserts within one transaction.
- `DemoAuthenticationHandler.cs` supplies the local test identities.
- `SelfCheck.cs` exercises HTTP status codes and persisted counts, including competing requests for one remaining slot.
- `LocalDatabase.cs` creates the sample database and restricts automatic cleanup to generated verification databases.

Subscrio stores its records in the `subscrio` schema. The application owns the `kilnbook` schema. Every reservation-writing path must take the same account lock for the capacity rule to hold. The sample evaluates the allowance for each request; it does not coordinate a simultaneous plan change with an in-flight reservation transaction.

## Reruns and limits

Verification uses a fresh database on every run. Manual testing retains reservations, so repeated requests eventually fill the plan. To reset manual testing, stop the app and delete only the `KilnBookEntitlements` database through SQL Server Object Explorer or SSMS; the next run recreates it.

A recurring reservation occupies one active slot in this example. There is no recurrence scheduler, date-conflict detection, payment integration, or booking-cancellation UI. Monthly billing cycles describe the sample plans; assigning one does not charge a customer or keep a billing provider synchronized.

The runnable example targets the entitlement decision and the concurrency boundary. Replace the fixture authentication before adapting it to a deployed application.
