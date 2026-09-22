# Subscrio samples

Runnable C# and TypeScript examples for Subscrio how-to articles.

This repository is being prepared. The [sample index](samples.json) lists 33 planned article examples; no runnable samples have been published yet. Entries become available only after their code and documented setup have been verified.

## Repository layout

Each article gets its own folder under `examples/`, named for the implementation it teaches and the language or framework it uses. Most examples will be small .NET console or TypeScript Node applications. Samples that demonstrate an HTTP boundary include a minimal server; the Stripe renewal example includes the webhook endpoints it needs.

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
