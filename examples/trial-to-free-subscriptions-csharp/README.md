# How to build a trial-to-free subscription workflow with Subscrio in C#

A calibration laboratory uses GaugeBench to generate certificates during a fourteen-day trial. Afterward, it should still be able to look up stored certificates, but it should no longer generate new ones. An expired agreement and a read-only replacement are two separate pieces of that behavior.

In this guide we will build a trial-to-free workflow in C#. We will configure the destination plan, run the required lifecycle processor, and inspect the resulting access.

Article status: unpublished draft. The public article URL will be added after publication.

## Prerequisites

Windows, .NET SDK 10.0.302, and SQL Server Express LocalDB. Install LocalDB through the SQL Server Express installer, then start MSSQLLocalDB. The sample uses Windows integrated authentication; no SQL login or password is required.

Connection template: `Server=(localdb)\MSSQLLocalDB;Database=SubscrioBlog_<generated identifier>;Integrated Security=true;TrustServerCertificate=true`. The database name is generated for each run.

Subscrio dependency: 0.4.0, pinned in the project and lock file.

## Run

```powershell
sqllocaldb start MSSQLLocalDB
dotnet restore
dotnet run --no-restore
```

## Verified output

```text
During trial: generate: true
Expired: generate: false
Transitions: 1
Transition errors: 0
Old agreement archived: true
Read-only: generate: false
Read-only: read: true
Second run transitions: 0
PASS: GaugeBench behavior verified.
```

Expiration first removes certificate-generation access. The processor then creates the read-only replacement and archives the old agreement. Its report records one transition; a second run records zero. Schedule this processor in your application for the downgrade to occur without a manual call. Subscrio does not start a background scheduler for you.

## Reruns and responsibilities

Every run creates a uniquely named disposable database. Cleanup checks the generated name and drops only that database after the client closes. Run the same command again to repeat the checks without catalog collisions. Assertions terminate the process on an incorrect result.

The sample teaches entitlement state and the application decisions shown above. Creating a billing cycle or subscription does not collect payment. External work and payment-provider integration remain application responsibilities.
