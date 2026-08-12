# Build and deploy the docs site

This documentation is built with [MkDocs Material](https://squidfunk.github.io/mkdocs-material/)
and deploys to **GitHub Pages** automatically.

## Local preview

```bash
pip install mkdocs-material
mkdocs serve          # http://127.0.0.1:8000
```

## Build

```bash
mkdocs build          # outputs ./site
```

## GitHub Pages deployment

`.github/workflows/docs.yml` builds the site and deploys it with GitHub
Pages on every push that touches `docs/` or `mkdocs.yml`. To enable:

1. Repository **Settings → Pages → Source**: choose **GitHub Actions**.
2. Push to `master` — the workflow builds and deploys automatically.
3. The site appears at `https://<owner>.github.io/RASharp/`.

## Conventions

- One page per concern; cross-link with relative markdown links
  (`[usage](getting-started/usage.md)`).
- Admonitions (`!!! note "..."`) for callouts; fenced code blocks with
  language hints; tables for reference data.
- The console table is generated from `RASharp.Cli/Consoles.cs` — see the
  comment at the top of `reference/console-table.md` for the regeneration
  snippet.
- Keep parity counts (`90/90`, `326/326`) in sync with the actual suite —
  update `reference/parity-evidence.md` and `index.md` when they change.
