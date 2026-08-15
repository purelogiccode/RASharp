# Build and maintain the docs site

This documentation is built with [MkDocs Material](https://squidfunk.github.io/mkdocs-material/).
The site is hosted on GitHub Pages: every push that touches `docs/`,
`mkdocs.yml`, or the pages workflow triggers
[`.github/workflows/pages.yml`](../../.github/workflows/pages.yml), which
builds the site and deploys it with `actions/deploy-pages`.

The Material theme's **left navigation menu** is driven by the `nav:`
section of [`mkdocs.yml`](../../mkdocs.yml) — add new pages there so they
appear in the sidebar.

## Local preview

```bash
pip install mkdocs-material
mkdocs serve          # http://127.0.0.1:8000
```

## Build

```bash
mkdocs build          # outputs ./site
```

## Publishing to GitHub Pages

Automated — no manual steps:

- Push to `master` with changes under `docs/` or to `mkdocs.yml`; the
  `Deploy docs to GitHub Pages` workflow builds and deploys
  (`.github/workflows/pages.yml`).
- Forced rebuild: run the workflow from the Actions tab
  (`workflow_dispatch`).
- The site is available at <https://purelogiccode.github.io/RetroAchievementsSharp/>.

The wiki (same content, hand-maintained) lives at
<https://github.com/purelogiccode/RetroAchievementsSharp/wiki>.

## Conventions

- One page per concern; cross-link with relative markdown links
  (`[usage](getting-started/usage.md)`).
- Admonitions (`!!! note "..."`) for callouts; fenced code blocks with
  language hints; tables for reference data.
- The console table is generated from `RetroAchievementsSharp.Cli/Consoles.cs` — see the
  comment at the top of `reference/console-table.md` for the regeneration
  snippet.
- Keep parity counts (`90/90`, `326/326`) in sync with the actual suite —
  update `reference/parity-evidence.md` and `index.md` when they change.
