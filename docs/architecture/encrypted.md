# 3DS encrypted hashing (`HashEncrypted.cs`, `AesHelper.cs`, `Hash3DS.cs`)

The 3DS formats are encrypted containers; RetroAchievements hashes the
*decrypted* content, so the engine performs an AES-128-CBC decrypt-then-hash
choreography.

## Formats

| Format | Handling |
|---|---|
| `.cia` | parse the CIA header (cert/ticket/TMD/content offsets), decrypt the content with the CIA normal key, hash the decrypted content |
| `.3ds` / NCCH | parse the NCCH header, decrypt ExeFS/RomFS regions with per-sector IVs |
| `.3dsx` | plaintext homebrew — whole-file hash |
| ELF | whole-file hash |

## Crypto (`AesHelper.cs`)

The port uses `System.Security.Cryptography.Aes` (CBC, `PaddingMode.None`)
but reproduces the **C call pattern** exactly:

- the sector IV is derived from the sector counter and the key,
- manual XOR where the C code does it,
- partial-block / unaligned-section handling for the final block.

The AES KATs (NIST SP 800-38A) in the test suite pin the primitive; the
decrypt-then-hash property (encrypted CIA hash == plaintext NCCH hash) pins
the choreography.

## Keys (`Hash3DS.cs`)

Keys come from the system directory passed via `-s`:

- **`aes_keys.txt`** — lines `common%u=`, `slot0x3DKeyX=`, `slot0x2CKeyX=`,
  `slot0x%02XKeyX=`.
- **`seeddb.bin`** — little-endian u32 count, 12 bytes padding, then
  (8-byte programId, 16-byte seed, 8-byte pad) records.

Key normalization (128-bit ROL/XOR/ADD loops, as in the C):

```
keyN  = ROL(keyX, 2) XOR keyY + generator_constant(0x1F F9 E9 AA C5 FE 04 08 02 45 91 DC 5D 52 76 8A)
keyN  = ROL(keyN, 87)
```

The NCCH secondary key Y is `SHA256(primaryKeyY ‖ seed)[0..16]` (BCL
SHA-256).

The two 3DS key callbacks (`Get3DsCiaNormalKey`, `Get3DsNcchNormalKeys`)
plug into the iterator via `RcHash.Init3DsGetCiaNormalKeyFunc` /
`Init3DsGetNcchNormalKeysFunc`, mirroring `rc_hash_init_3ds_*`.

## Key material policy

`aes_keys.txt` and `seeddb.bin` are **user-supplied and never
redistributed** — both are git-ignored. The test suite uses **synthetic key
material** (generated fixtures with known keys), and the parity harness
generates its own `aes_keys.txt`/`seeddb.bin` in the corpus. Real retail CIA
parity remains a documented follow-up requiring the user's key files.

## Error paths

- junk file → `Could not read 3DS ROM header`-style errors (byte-identical
  to the oracle);
- missing `aes_keys.txt` → key-lookup errors; note the C *silently* falls
  back to hashing plaintext when keys are unavailable for some paths — the
  port reproduces this faithfully.
