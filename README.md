# Subscrio samples

Runnable C# and TypeScript examples for Subscrio how-to articles.

Start with the [ASP.NET Core feature-entitlement example](examples/feature-entitlements-aspnet-core). It includes SQL Server LocalDB setup and an executable check of the real HTTP endpoint. The [sample index](samples.json) tracks ready examples and the remaining planned articles.

For Node.js, run the [TypeScript feature-entitlement example](examples/feature-entitlements-typescript). It demonstrates boolean access checks, numeric allowances, and text-based support routing against PostgreSQL.

## Repository layout

Each article gets its own folder under `examples/`, named for the implementation it teaches and the language or framework it uses. Most examples will be small .NET console or TypeScript Node applications. Samples that demonstrate an HTTP boundary include a minimal server; the Stripe renewal example includes the webhook endpoints it needs.

C# examples use Microsoft SQL Server LocalDB on Windows. TypeScript examples use PostgreSQL. Each sample documents its own prerequisites and connection setup.

```text
examples/
  feature-entitlements-aspnet-core/
  feature-entitlements-typescript/
  ...
  stripe-subscription-renewals/
    dotnet/
    typescript/
```

These paths are reserved in the index and will be created as their samples are implemented. Each sample will include its own README, complete source, dependency declarations, configuration example, verification commands, and expected output. Samples must run from a clone of this repository without private workspace dependencies.

There are 32 single-language article samples and one Stripe renewal article with both languages: 33 article folders and 34 runnable applications when the series is complete.

## Start here

Choose an entry marked `ready` in [samples.json](samples.json), open its folder, and follow that folder's README. Each README will identify the matching article and the versions tested. Until an entry is ready, its folder may not exist.

See [CONTRIBUTING.md](CONTRIBUTING.md) for sample requirements.

## Subscrio

- [Subscrio website](https://subscrio.com)
- [Documentation](https://docs.subscrio.com)
- [TypeScript library](https://github.com/subscrio/subscrio-typescript)
- [.NET library](https://github.com/subscrio/subscrio-dotnet)

## License

MIT. See [LICENSE](LICENSE).

## More runnable examples

- [Replace hardcoded plan checks in C#](examples/replace-hardcoded-plan-checks-csharp). A new plan name keeps its purchased CSV export access.
- [Enforce feature access in TypeScript](examples/server-side-feature-gating-typescript). Direct HTTP requests receive the correct publication decision.
- [Transition a C# trial to read-only access](examples/trial-to-free-subscriptions-csharp). An explicit lifecycle job replaces an expired trial.

## Quotas, credits, and purchases

The ready entries in [samples.json](samples.json) now include monthly quotas, billing-period metering, monthly credit grants, shared wallets, timed overrides, subscription composition, whole-app one-time purchases, and prepaid jobs. Each points to its dedicated runnable folder.

The [Stripe renewal examples](examples/stripe-subscription-renewals) include a passing TypeScript runner and sandbox lifecycle fixtures. The C# runner remains unpublished. The SQL Server UTC mapping fix is available in Subscrio.Core 0.5.1; the runner still needs to be integrated and verified against that public package.
