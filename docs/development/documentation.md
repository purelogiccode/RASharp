# Build and maintain the docs site

This documentation is built with [MkDocs Material](https://squidfunk.github.io/mkdocs-material/).
The repository intentionally has **no CI** — the site is built and published
manually.

## Local preview

```bash
pip install mkdocs-material
mkdocs serve          # http://127.0.0.1:8000
```

## Build

```bash
mkdocs build          # outputs ./site
```

## Publishing to GitHub Pages (manual)

If you want the site hosted, the simplest manual routes are:

```bash
# option 1: push the built site to the gh-pages branch
mkdocs gh-deploy

# option 2: build locally and publish ./site from any static host
mkdocs build
```

(For Pages from a branch: repo Settings → Pages → Source → `gh-pages`.)

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
