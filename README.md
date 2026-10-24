# stardew

.NET monorepo + agent skills.

## Layout

| Path | Purpose |
| --- | --- |
| `apps/` | Runnable applications |
| `packages/` | Reusable libraries |
| `tooling/` | Repo automation and verification |
| `tests/` | Cross-package test suites |
| `.agents/skills/` | Agent skills |
| `.agents/references/` | Read-only third-party clones (gitignored) |

## Packages

<!-- none yet -->

## Verification

```bash
dotnet restore
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build --no-restore
```

Or run the gate: `pwsh ./tooling/verify.ps1` (Windows: `tooling\verify.cmd`).

## Skills

Skills live in `.agents/skills/`. They are discovered by scanning the directory; no index to maintain.
