# Zip hashing (`HashZip.cs` — port of `hash_zip.c`)

`HashZip` implements the rc_hash zip semantics **without**
`System.IO.Compression` — it is a byte-level parser that reproduces the C's
miniz-based behavior exactly, because the hash depends on raw archive bytes
(record order, name normalization, CRC + decompressed size), not on the
decompressed content.

## Central-directory walk

1. **EOCD scan** — locate the End Of Central Directory record (with the
   C's exact scan bounds and the Zip64 extension handling).
2. **Entry iteration** — walk central directory headers; skip directory
   records (trailing `/` or `\`, the `0x10` external-attribute bit, and —
   since 12.4.0 — zero-length names).
3. **Record hashing** — for each file entry, hash (in byte-sorted order):
   - the normalized filename (backslash → slash, upper → lower),
   - the CRC32,
   - the decompressed size.
4. The resulting MD5 identifies the archive *layout* — this is what
   RetroAchievements uses for romsets.

## Consumers

| Consumer | Behavior |
|---|---|
| `RcHashArduboyFx` | `.arduboy` zip: filter to `interp_s2_ArduboyFX.hex` + `.bin` entries |
| `RcHashMsDos` | DOSZ/DOSC: hashes the zip records, then the sibling `.dosc`, then the parent chain via `*.parent` entries (recursive) |
| CLI zip pre-load | `.zip` + console ≤ 90 → first entry loaded and hashed via the buffer API (exactly `util::loadZippedFile` + `rc_hash_generate_from_buffer`) |

## Test vectors

The mock zip builder in the test project reproduces the C's
`mock_zip_add_file`/`mock_zip_finalize` **byte-for-byte**, including the
Zip64 variant, so the ported `test_hash_zip.c` vectors are exact: Arduboy FX,
DOSZ, DOSZ-Zip64, DOSC sibling, and the DOSZ parent chain (5 vectors).

!!! note "Parent chains"
    The real-file behavior of DOSZ parent resolution differs subtly from the
    mock-based vectors (the mock's directory semantics do not match real
    path building). The parity suite therefore asserts oracle==CLI equality
    on parent-chain cases (byte-identical output) while the mock vectors
    stay covered by the unit tests.
