# Contributing samples

Each article has one dedicated folder listed in samples.json. Keep its case study independent from the other articles. Use a small C# console app or TypeScript Node app unless the lesson needs a real HTTP endpoint or worker.

## Required files and behavior

- Complete source, dependency declarations, and lock files where supported.
- A README naming the article ID, commercial model, prerequisites, tested versions, setup commands, run command, expected output, and failure-case verification.
- An environment configuration example containing placeholders only. Never commit credentials, database exports, customer records, or Stripe live-mode data.
- A reproducible catalog and customer setup with isolated sample keys, documented rerun behavior, and a disposable database requirement.
- A demonstrated success path and the article's assigned failure path. Assertions must fail the process when expected behavior is violated.
- Public, pinned dependencies. Do not depend on a sibling private checkout or an absolute developer-machine path.
- Exact setup and execution commands in fenced code blocks. C# examples use csharp fences and TypeScript examples use typescript fences; commands and output have their own fences.
- Matching article snippets copied from the verified source. Explain omitted surrounding code instead of presenting partial snippets as a complete app.

## Standard README structure

1. What the example demonstrates and which article it accompanies.
2. Prerequisites and exact tested versions.
3. Configuration with placeholder values.
4. Dependency installation and database setup.
5. Run command and expected output.
6. Failure-case verification, retry behavior where applicable, and rerun/cleanup instructions.
7. Responsibilities left to the application, including authorization, external effects, and payment confirmation when relevant.

Prefer dotnet run for C# and npm ci followed by npm run demo for TypeScript. If an example requires a server, document both startup and the commands that exercise it. Avoid interface scaffolding unrelated to the lesson.

## Before marking a sample ready

Run it from a clean checkout with a disposable database, verify its normal and failure cases, and record the tested dependency versions and commit. Add appropriate CI checks for the runnable sample. Update samples.json with status ready, its published article URL when available, and the verified commit. Do not mark a placeholder or compile-only example ready.

The Stripe renewal article owns one folder with dotnet and typescript subfolders. Test both against Stripe test-mode fixtures and verify raw-body webhook signature handling. Never require live charges to run a sample.
