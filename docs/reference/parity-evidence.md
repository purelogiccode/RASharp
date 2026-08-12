# Parity evidence

The claim — *"RASharp produces 100 % identical output to the original"* —
is an engineering claim with receipts. This page lists the evidence and how
to reproduce it.

## Current status

| Metric | Value |
|---|---|
| Full test suite | **326/326 green** (Debug and Release) |
| Tier-2 parity cases | **90/90 byte-identical** vs the rcheevos **12.4.0**-built oracle |
| Tier-1 ported vectors | all upstream `test/rhash` vectors green |
| CLI output | byte-identical (stdout + stderr + exit codes), including verbose mode and error paths |
| Platforms | portable `net10.0`; publishes win-x64/arm64 + linux-x64/arm64 |

## The three tiers

1. **Tier 1 — ported vectors**: every upstream test from `test/rhash/`
   ported with expected MD5s verbatim (deterministic, offline).
2. **Tier 2 — parity harness**: both executables run with identical
   arguments over a generated corpus; stdout/stderr compared **raw byte for
   byte** (`\r\n` included); exit codes must match. See
   [the parity harness](../architecture/parity-harness.md).
3. **Tier 3 — published hashes**: spot checks against retroachievements.org
   game pages and the
   [game-identification docs](https://docs.retroachievements.org/developer-docs/game-identification.html)
   (in progress — needs real ROMs).

## What the harness caught (evidence the harness works)

The Tier-2 suite is not decorative — it found real port bugs in Part I:

1. **Arg-count guard off-by-one** — `RASharp.exe 4` crashed with an
   unhandled exception instead of printing usage (fixed).
2. **Wildcard path construction** diverged from `FindFirstFile`/`util::directory`
   semantics (fixed, incl. the `dir/*.bin` quirk reproduction).
3. **Usage banner** had an extra blank line vs. the C (fixed).

And in Part II, the audit found the port had inherited the 12.2.1
`merge_callbacks` bug (fixed with 12.4.0).

## Oracle matrix

| Oracle | Source | Used for |
|---|---|---|
| `References\rcheevos-12.4.0\bin64\RAHasher.exe` | rcheevos 12.4.0 + RAHasher CLI stack + libchdr v0.3.0 | current default (Part II) |
| `References\RAHasher-1.8.3\bin64\RAHasher.exe` | pinned 1.8.3 sources (12.2.1) | Part I evidence |
| `References\RAHasher.exe` | legacy 1.8.3 binary (numeric ids only) | fallback |

All are GPL-3.0-built, local-only, git-ignored. Building them:
[oracles](../development/oracles.md).

## Reproduce

```bash
# build + full suite (Debug and Release)
dotnet test RASharp.sln -c Debug
dotnet test RASharp.sln -c Release

# parity only
dotnet test --filter FullyQualifiedName~Parity

# force a specific oracle
RASHARP_ORACLE=C:\path\to\RAHasher.exe dotnet test --filter FullyQualifiedName~Parity
```

Any mismatch in the parity suite is a **port bug** — the project never
"accepts" a difference.

## Historical record (Part I)

- Phase 3: real-file parity vs. the C oracle — PSX cue
  `db433fb038cde4fb15c144e8c7dea6e3`, 3DO bin
  `257d1d19365a864266b236214dbea29c`.
- Phase 4: real PS2 ISO parity — `01a517e4ad72c6c2654d1b839be7579d`.
- Phase 5: CHD parity — PSX CHD `db433fb038cde4fb15c144e8c7dea6e3`, PSP CHD
  `a7070bf07f5c1a0afb2b2d202d7e3893` (byte-for-byte vs RAHasher 1.8.3).
- Phase 6: 3DS synthetic-fixture parity 10/10 (byte-identical verbose).
- Phase 8: 82/82 corpus cases vs the source-built 1.8.3 oracle.

## Known gaps (documented, not hidden)

- 3DS real retail CIAs — requires user-supplied `aes_keys.txt`/`seeddb.bin`.
- Tier 3 spot checks — requires real ROMs (external corpus).
- Saturn/Dreamcast real CHDs — not available locally; covered by the
  synthetic corpus + upstream vectors.
