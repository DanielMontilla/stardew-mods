## Phase 1: Entry Correctness

- [ ] Entry placed under correct version heading (Unreleased, new, or existing)
- [ ] Entry uses one of 6 standard types (Added, Changed, Deprecated, Removed, Fixed, Security)
- [ ] Entry body compressed per caveman-compression rules — no articles, aux verbs, redundant prepositions, pronouns, intensifiers
- [ ] Entry does NOT re-prefix the action verb already conveyed by its type heading (unless dropping it loses meaning)
- [ ] Entry retains all semantic meaning after compression
- [ ] Entry body only compressed — header boilerplate, version headings, dates, type headings untouched
- [ ] Single entry per bullet, no sub-bullets
- [ ] Date format is ISO 8601 (YYYY-MM-DD)
- [ ] New version obeys SemVer relative to last version
- [ ] Pre-release identifiers are dot-separated SemVer (e.g., `-alpha`, `-beta.1`, `-rc.2`)

## Phase 2: File Integrity

- [ ] CHANGELOG.md exists at project root
- [ ] Version headings follow `## [X.Y.Z] - YYYY-MM-DD` (optionally followed by ` [YANKED]`), or `## [X.Y.Z] [YANKED]` for date-less pre-releases, or `## [Unreleased]`
- [ ] `## [Unreleased]` is the top heading (Keep a Changelog ordering)
- [ ] Released versions ordered newest-first below `## [Unreleased]`
- [ ] Type headings within each version appear in canonical order: Added → Changed → Deprecated → Removed → Fixed → Security
- [ ] Keep a Changelog + SemVer reference lines present verbatim (if file was created)
- [ ] Empty `## [Unreleased]` heading present after release (if release was performed)
- [ ] No duplicate version headings
- [ ] Yanked versions use `## [X.Y.Z] - YYYY-MM-DD [YANKED]` (suffix after date), or `## [X.Y.Z] [YANKED]` for date-less pre-releases

## Phase 3: Output Style

- [ ] First response line is a reader-actionable next step (per i-have-adhd rule 1) — not a status report
- [ ] Response ends with ONE concrete next action the reader can do in under two minutes (per i-have-adhd rule 3)
- [ ] Exact entry/heading text shown verbatim in response (not "I added an entry")
- [ ] No preamble, recap, or closing pleasantries
