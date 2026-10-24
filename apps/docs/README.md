# Docs

Static documentation site for the mods in this repo, built with Astro + Starlight.

## How it works

Mod pages are generated at build time from the repo's `mods/` folder — nothing is duplicated here.

| Source | Used for |
| --- | --- |
| `mods/<Mod>/manifest.json` | name, author, description, unique ID |
| `mods/<Mod>/*.csproj` | version (the manifest stores a `%ProjectVersion%` token) |
| `mods/<Mod>/README.md` | overview page body |
| `mods/<Mod>/docs/*.md` | wiki pages (frontmatter: `title`, `description`, `order`) |
| `mods/<Mod>/assets/**/*.json` | data tables and `{{table:...}}` directives |
| `mods/<Mod>/assets/**/*.{png,jpg,jpeg,webp,gif}` | asset gallery |
| `mods/<Mod>/i18n/default.json` | display names for translation keys |
| `mods/<Mod>/ModConfig.cs` | configuration table and config defaults |
| `mods/<Mod>/**/*.cs` | `const` values for `{{const.Name}}` |

`src/lib/mods.ts` discovers mods and pages. `src/lib/mod-data.ts` builds the value context,
`src/lib/csharp.ts` reads config/const literals, and `src/lib/markdown.ts` renders pages.
Files starting with `_` in `docs/` are treated as drafts and skipped.

## Values in markdown

Plain Markdown can reference the mod's real data. Tokens are resolved at build time:

```md
The Desert survey costs {{spot.Desert.Unlock.Steps[0].Gold}}g.

Base config default: {{config.Difficulty}}
Unlock flag prefix: {{const.MailIdPrefix}}
Version: {{version}}
Translation: {{i18n:spot.desert.name}}
```

Lookups are case-insensitive, arrays accept `[0]`, and rows in indexed collections can be reached
by `Id` (`spot.Desert`, `difficulty.Realistic`).

Bare tokens such as `{{spot}}` or `{{gold}}` are left alone so SMAPI translation strings survive.
Tokens inside fenced code blocks and inline code are also skipped. Any dotted token that doesn't
resolve fails the build with the file name, so typos can't silently ship.

Generated tables use the same syntax:

```md
{{table:config}}         Configuration options, defaults, and XML doc summaries.
{{table:difficulties}}   Difficulty presets and multipliers.
{{table:spots}}          Route table with unlock stages.
{{table:<asset-name>}}   Any other asset file, by base name.
```

Route detail pages can use a full requirements card:

```md
{{requirements:Desert}}
```

It renders the destination, ride price, discovery condition, and one card per unlock stage, with
translated stage names and descriptions.

## Images

Use normal Markdown images with a path relative to the page:

```md
![The three Desert stages](../images/desert-route.svg)
```

Images are resolved from `mods/<Mod>/assets/**` or `mods/<Mod>/docs/**` and bundled at build time.
A missing file fails the build. Root-relative links (`/mods/...`) are automatically prefixed with
the deployment base path.

## Site preferences

**Mode** (Easy / Realistic / Hard) sits in the header next to the theme picker, and in the mobile menu.
It is stored in `localStorage` under `stardew-docs-mode`. The icons are heart, star, and rocket; the
label is hidden in the header below 90rem but always shown in the mobile menu.

The color theme is Starlight's built-in Light / Dark / Auto picker (stored under `starlight-theme`).

There is no flash on load:

- Starlight's inline theme script sets `data-theme` before the first paint.
- The mode script sets `data-mode` in `<head>`, and visibility is pure CSS
  (`html[data-mode="easy"] [data-mode-text="easy"]`), so no text is swapped after paint.

Every difficulty-scaled value is precomputed per preset at build time, so the static site can switch
modes without shipping the mod's logic to the browser.

Scaling mirrors `MinecartSpotRepository.ScaleGold` and `ScaleCount`: gold rounds to the nearest
integer, item counts round up with a minimum of one, and `RequireDiscovery: false` hides discovery
conditions. If those rules change in C#, update `scaleGold`/`scaleItems` in `src/lib/mod-data.ts`.

Mods without a `difficulties.json` render static values and the control falls back to the page's
default mode.

## Commands

Run from the repo root:

- `npm install`
- `npm run docs:dev`
- `npm run docs:check`
- `npm run docs:build`

Output lands in `apps/docs/dist`. Set `SITE_URL` and `BASE_PATH` for a non-root deployment; the
GitHub Pages workflow fills them from `actions/configure-pages`.

## Content pages

Hand-written guides live in `src/content/docs/` and use Starlight frontmatter.
