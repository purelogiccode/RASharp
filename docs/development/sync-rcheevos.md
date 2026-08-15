# Release-sync playbook

rcheevos is the single source of truth. When a new rcheevos release lands,
the port is synced mechanically in ~2–3 days using this procedure (Part II,
phase E6 of the conversion plan).

## Scope — no net-new hashing algorithms

RetroAchievementsSharp only implements what rcheevos defines. Formats or consoles that
rcheevos does not support — e.g. RVZ (Wii/GameCube), WUX/WUD (Wii U, id 20
has no hasher) — are out of scope by policy, even when users request them:
a custom algorithm would have no upstream reference and no oracle to
validate against. Extensions to the CLI and test suite (such as the
real-ROM parity tests) are welcome as long as they only exercise
rcheevos-defined hashing. See
[Known quirks](../reference/known-quirks.md) for the exact fallback
behavior on unsupported formats.

## Step 1 — Get the new tree

Download the release source (or `git clone --depth 1 --branch vX.Y.Z`) into
`References/rcheevos-<ver>/`. Keep the previous tree for diffing.

## Step 2 — Diff the hashing surface

```bash
diff -r References/rcheevos-<prev>/src/rhash References/rcheevos-<new>/src/rhash
diff -r References/rcheevos-<prev>/include/rc_hash.h References/rcheevos-<new>/include/rc_hash.h
diff -r References/rcheevos-<prev>/include/rc_consoles.h References/rcheevos-<new>/include/rc_consoles.h
diff -r References/rcheevos-<prev>/test/rhash References/rcheevos-<new>/test/rhash
```

Also read the release's `CHANGELOG.md` hashing entries.

## Step 3 — Triage every diff hunk

Classify each change into one of:

| Category | Meaning | Action |
|---|---|---|
| **new algorithm** | new console/format function (e.g. `.neo` in 12.4.0) | port 1:1 + vectors + corpus cases |
| **guard/bugfix** | behavior-preserving bounds checks or a bug fix (e.g. `merge_callbacks` in 12.4.0) | port the fix; add a unit test per guard |
| **API change** | `rc_hash.h` / `rc_consoles.h` deltas | update `RcHash.cs` / `ConsoleIds.cs` / `HashIterator.cs` |
| **test delta** | new/updated vectors in `test/rhash/` | port to xUnit with expected MD5s verbatim |
| **cosmetic** | comments, whitespace, `RC_CCONV` | nothing (note it) |

The 12.2.1 → 12.4.0 audit is the reference example: 1 new algorithm, 2
extension-table entries, 1 bugfix, 6 guards — everything else identical.

## Step 4 — Port

Follow the [porting guide](porting-guide.md): origin headers, 1:1
translation, vectors before merges.

## Step 5 — Rebuild the oracle

Update `References/rcheevos-<new>/Makefile.oracle` if the file layout
changed, then rebuild (`make -f Makefile.oracle CC=./ccw-gcc.sh
CXX=./ccw-g++.sh` — see [oracles](oracles.md)). The harness prefers the
newest oracle automatically by search order; bump the order in
`ParityHarness.FindOracle` if needed.

## Step 6 — Baseline + extend

1. Run the full parity suite against the **new oracle** — every existing
   case must still pass byte-identically (this catches anything the audit
   missed).
2. Extend the corpus for new algorithms/formats (see
   [the parity harness](../architecture/parity-harness.md)).
3. `dotnet test` in Debug and Release — 100 % green.

## Step 7 — Document

Update `ConversionPlan.md` (Part II audit table + phase checkboxes),
`docs/` (hashing semantics, console table, known quirks), and `README.md`
(parity counts).

## Checklist

- [ ] `diff -r` produced; every hunk triaged into the table above
- [ ] new algorithms ported with upstream vectors + corpus cases
- [ ] guards/bugfixes ported with unit tests
- [ ] oracle rebuilt from the new tree; baseline corpus green
- [ ] extended corpus green (100 % identical output)
- [ ] Debug + Release suites green
- [ ] docs + plan updated
