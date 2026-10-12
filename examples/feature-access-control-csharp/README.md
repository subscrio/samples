# Adding feature access control to your C# web app

ReleaseBoard is a fictional release-notes app. This ASP.NET Core example checks whether a customer can download notes before generating a Markdown file.

| Customer | Download result |
| --- | --- |
| Starter | 403, no file generated |
| Team | 200, Markdown attachment |
| No subscription | 403, no file generated |

The handler asks about the `download-notes` feature rather than comparing plan names. `SelfCheck.cs` starts a real HTTP server and checks response bodies, download headers, file content and generator invocations. It also changes the Team plan value to false and confirms the next request is denied without another generation.

## Prerequisites

Windows, the .NET 10 SDK, SQL Server Express LocalDB and an Internet connection for the initial restore. The project pins the released Subscrio.Core 0.5.1 package and includes its lock file. LocalDB uses Windows integrated authentication.

## Verify

Run from this directory:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore --locked-mode
dotnet run --no-restore -- --verify
```

If the instance is missing, create it once with `sqllocaldb create MSSQLLocalDB`.

Captured output:

```text
PASS Starter direct request: 403; files generated: 0
PASS No subscription: 403; files generated: 0
PASS Team direct request: 200; release-notes.md; files generated: 1
PASS Team feature disabled: 403; files generated still: 1
All HTTP feature-access checks passed.
```

Each run creates a database named `ReleaseBoardDemo_<generated GUID>` and removes only that database on normal shutdown. A failed assertion exits with an error. Repeat the command for a fresh run.

## Try the download

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --no-restore
```

Open http://127.0.0.1:5086/demo/team/notes.md for the allowed download. Compare it with http://127.0.0.1:5086/demo/starter/notes.md and http://127.0.0.1:5086/demo/unassigned/notes.md. Stop with Ctrl+C when finished.

## Read the implementation

- `Program.cs`: web host, scoped Subscrio registration and fixed local demo routes.
- `Catalog.cs`: product, feature, plans, billing cycles, customers and subscriptions.
- `NotesDownload.cs`: feature check, denied response and file generation.
- `SelfCheck.cs`: HTTP requests and assertions, including proof that denied requests don't generate files.
- `LocalDatabase.cs`: isolated database creation and cleanup.

## Scope and application responsibilities

The demo binds to loopback and refuses to run outside Development. The three fixed-customer routes are local test fixtures. A deployed endpoint must obtain the current customer's key from the application's trusted identity and customer-membership flow; never let a request choose another customer's key. This example deliberately does not implement login or user-role rules.

The Markdown content is fictional and fixed. `NotesWriter` counts invocations for test evidence; it is not a production document store or telemetry system. Subscrio provides the feature decision, while application code generates and returns the file.

Billing cycles and manually assigned subscriptions do not collect payment. The last verification step changes the Team plan for all of its customers inside the disposable database. It is not a per-customer downgrade API.

Access is checked once before file generation. An admitted download is not cancelled if the plan changes afterward. The sample doesn't cache access decisions. Operational errors propagate; they are not converted into false feature values.

AI tools assisted with this tutorial sample. Its behavior was checked by executing the included HTTP tests.
