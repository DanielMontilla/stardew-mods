# stardew

## Language

C# / .NET 10. Nullable enabled, warnings as errors, central package management.

## Package Manager

NuGet — `Directory.Packages.props` is source of truth.

## Commands

- restore: `dotnet restore`
- build: `dotnet build`
- test: `dotnet test`
- format: `dotnet format` / `dotnet format --verify-no-changes`
- verify (full gate): `pwsh ./tooling/verify.ps1` (Windows: `tooling\verify.cmd`)
- verify (fast, no test suite): same with `-Fast`

## Verification

- Verify via the commands above. Never invoke analyzer, formatter, or test runner binaries by hand.
- Always scope verification to the working area. Never run repo-wide checks when you touched one project:
  - project scope: `dotnet build <project>`, `dotnet test <project>`, `dotnet format <project>`
  - restore is repo-wide by nature; run it once, then use `--no-restore`
- `pwsh ./tooling/verify.ps1` is the repo-wide gate. CI calls the same script, so the two cannot drift.

## Worktrees

- Bare repo: `../.bare`. Worktrees are siblings: `main/`, `<feature>/`.
- Add: `git -C ../.bare worktree add ../<name> -b <name>`
- List: `git -C ../.bare worktree list`

## Skills

- `.agents/skills/` — discovered by scanning this directory
- `.agents/references/` — local read-only clones (gitignored); see `finding-references`
