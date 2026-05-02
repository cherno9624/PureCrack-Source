# Refreshing the Inner Source Bundle

Use this recipe when PureCrack ships a new `data.pak` (e.g. they update for a
newer PureRAT panel version) and you need to extract the new inner sources to
swap into `inner/`.

---

## Prerequisites

- A running, activated `PureServer.exe` from the new PureCrack distribution.
- `procdump.exe` (Sysinternals) — comes with `winget install Microsoft.Sysinternals.ProcessDump`.
- Python 3 with no extra deps (just `zipfile`, `re`).

## Recipe

### 1. Activate PureServer normally

The activation gate is the only thing protecting `data.pak`. Run PureServer
with a valid license, let it complete the initial /activate exchange. Once the
console shows the "Ready" state, the decrypted `data.pak` contents are sitting
in PureServer's heap.

### 2. Get its PID

```powershell
Get-Process PureServer | Select-Object Id
```

### 3. Full memory dump

```
procdump.exe -accepteula -ma <pid> pureserver_live.dmp
```

Takes 0.5-2 seconds. Produces a ~50 MB dump file.

### 4. Find the source ZIP inside the dump

```python
from pathlib import Path
import re, zipfile, io, struct

dump = Path("pureserver_live.dmp").read_bytes()

# Locate ZIP local file headers (PK\x03\x04). The first one starting a PureRAT
# source bundle is followed by 'Attribute0.cs' or 'Class0.cs' as the first entry.
lfhs = [m.start() for m in re.finditer(b'PK\x03\x04', dump)]
print(f"LFH count: {len(lfhs)}")

# Find the matching End-Of-Central-Directory record after the first source-cs LFH.
# The bundle is contiguous: [LFH...LFH][CD...CD][EOCD].
for off in lfhs:
    # Read filename
    _, ver, flags, method, _, _, crc, csize, usize, nlen, xlen = struct.unpack(
        '<IHHHHHIIIHH', dump[off:off+30])
    if 1 <= nlen <= 200:
        name = dump[off+30:off+30+nlen]
        if name.startswith((b'Attribute0', b'Class0', b'GClass0')):
            print(f"first source LFH: 0x{off:x}, name={name}")
            first = off
            break

# EOCD: PK\x05\x06 in the same vicinity (within ~100 KB after first LFH)
search_range = dump[first:first + 200_000]
eocd_rel = search_range.find(b'PK\x05\x06')
if eocd_rel < 0:
    raise RuntimeError("EOCD not found near first LFH")

# Read EOCD: sig(4) diskno(2) cddisk(2) entries_disk(2) entries_total(2)
#            cdsize(4) cdoff(4) commentlen(2)
eocd = first + eocd_rel
sig, _, _, _, total, cdsize, cdoff, clen = struct.unpack('<IHHHHIIH',
    dump[eocd:eocd + 22])
end = eocd + 22 + clen
size = end - first

print(f"ZIP archive at 0x{first:x}, size={size}, entries={total}")
Path("PayloadSource.zip").write_bytes(dump[first:end])
print("Saved PayloadSource.zip")
```

### 5. Extract

```python
import zipfile
with zipfile.ZipFile("PayloadSource.zip") as z:
    print(f"{len(z.namelist())} entries")
    z.extractall("new_inner/")
```

### 6. Diff against current `inner/`

```bash
diff -r inner/ new_inner/
```

Expected differences when PureRAT updates:

- New ProtoMember slots in GClass3 / GClass4 (additional config fields). Our
  `build.py: _encode_gclass3_body()` doesn't emit unknown fields, but the
  new stub will accept partial config without complaint as long as required
  ones are present.
- Renamed obfuscation tokens (e.g. `kLjw4iIsCLsZtxc4lksN0j` becomes a new
  random name). Our patching only replaces the placeholder string, which is
  invariant across versions, so renames don't matter.
- New `Class*` files for added features. They carry their own static-init
  callbacks that all chain through `Class16.kLjw...()`. Compile is unaffected.

### 7. Swap

```bash
rm inner/*.cs
cp new_inner/*.cs inner/
python tests.py robust
```

If `tests.py robust` still passes 4/4, the swap is good. If a test fails, the
most likely cause is a renumbered `[ProtoInclude]` discriminator on `GClass2`
— check `GClass2.cs` for the `(38, typeof(GClass3))` attribute and update
`build.py: _encode_gclass3()`'s `_tag(38, 2)` if the number changed.

## Why this works

`PureServer.exe` is native C++ with VMProtect-style packing. The AES key for
`data.pak` is hidden inside a control-flow-flattened state machine — static
extraction is hard. But the program has to use the data eventually, and the
moment it does, plaintext bytes exist in heap pages that any sufficiently
privileged observer can dump. `procdump -ma` requires admin (which a real
PureCrack user already has, since PureServer needs admin for port 443) and
takes a snapshot of the entire process address space. The decrypted ZIP is in
there.

This is a common property: protected binaries can resist *static* analysis
arbitrarily well, but they're highly observable to *dynamic* analysis from any
privileged context they run in.
