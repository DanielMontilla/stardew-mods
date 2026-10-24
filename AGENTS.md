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

## Mods

- Live in `mods/<ModName>/`, one folder per mod, with `manifest.json` at its root. `EntryDll` must match the assembly name.
- Target `net6.0` (SMAPI 4.x / game runtime). `mods/Directory.Build.props` overrides the repo default; never set `net10.0` on a mod.
- Every mod project references `Pathoschild.Stardew.ModBuildConfig` (version pinned in `Directory.Packages.props`). It locates the game, adds SMAPI/game references, bundles `manifest.json` and assets, and copies build output to the game's `Mods/` folder. Set `GamePath` if auto-detection fails.
- Build a single mod with `dotnet build mods/<ModName>`.
- Add each new mod's `.csproj` to `stardew.slnx`, otherwise the repo-wide gate skips it.
- Read SMAPI docs at `.agents/references/smapi/docs`; read game internals at `.agents/references/stardew-valley-decompiled`.

## Worktrees

- Bare repo: `../.bare`. Worktrees are siblings: `main/`, `<feature>/`.
- Add: `git -C ../.bare worktree add ../<name> -b <name>`
- List: `git -C ../.bare worktree list`

## Skills

- `.agents/skills/` — discovered by scanning this directory
- `.agents/references/` — local read-only clones (gitignored); see `finding-references`
