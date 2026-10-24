# stardew

.NET monorepo + agent skills.

## Layout

| Path | Purpose |
| --- | --- |
| `apps/` | Runnable applications |
| `packages/` | Reusable libraries |
| `mods/` | SMAPI mods, one folder per mod |
| `tooling/` | Repo automation and verification |
| `tests/` | Cross-package test suites |
| `.agents/skills/` | Agent skills |
| `.agents/references/` | Read-only third-party clones (gitignored) |

## Packages

<!-- none yet -->

## Mods

- [`ImmersiveMinecarts`](mods/ImmersiveMinecarts) — per-player minecart destinations unlocked with gold and regional resources.

## Docs

Static site in [`apps/docs`](apps/docs) (Astro + Starlight), deployed to GitHub Pages. Mod pages and
wiki pages are generated at build time from each mod's `manifest.json`, `README.md`, `docs/*.md`,
`assets/`, and `i18n/`; values come from the C# config/constants and assets, and a header control
switches difficulty. There is no duplicated content to keep in sync.

```bash
npm install
npm run docs:dev     # local dev server
npm run docs:build   # static output in apps/docs/dist
npm run docs:check   # type check
```

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
