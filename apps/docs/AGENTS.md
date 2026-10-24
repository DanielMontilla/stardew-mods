# apps/docs

Astro + Starlight static site. Mod pages are generated from `../../mods`; never copy mod data into
this app.

- Dev server: `npm run docs:dev` from the repo root.
- Type check: `npm run docs:check`.
- Build: `npm run docs:build` (output in `apps/docs/dist`).
- Mod/page discovery: `src/lib/mods.ts` (README overview + `docs/*.md` pages).
- Values and difficulty variants: `src/lib/mod-data.ts`.
- C# literal extraction: `src/lib/csharp.ts`.
- Token/table rendering: `src/lib/markdown.ts`.
- Images: `src/lib/mod-images.ts` (asset gallery + `docs/` images used in Markdown).
- Mode switch: `src/components/ModeSwitcher.astro`, injected via the `ThemeSelect` override in
  `astro.config.mjs`; behavior lives in `public/mode-switcher.js`.
- Images: `src/lib/mod-images.ts` (`import.meta.glob` over `mods/*/assets`).
- Starlight docs: https://starlight.astro.build

Markdown tokens (`{{config.X}}`, `{{spot.Desert.Unlock.Gold}}`, `{{table:spots}}`) are resolved at
build time and fail the build when a path is missing. Bare `{{spot}}`-style SMAPI tokens are left
untouched.
