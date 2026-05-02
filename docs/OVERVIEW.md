# PureCrack RE & Replay — Overview

The big-picture technical narrative for this workspace. The README is "how to
run it"; this is "what's inside it and why."

---

## 0. Goal

Reverse PureRAT v4.0.9596's build pipeline far enough that you can produce
registering stubs locally, offline, without depending on PureCoder's online
backend or PureCrack's `PureServer.exe`. Don't modify the panel binary; just
reconstruct the network and build services it expects.

## 1. The pieces involved

| Software | What it is | What it does |
|----------|------------|--------------|
| **PureRAT.exe** (panel) | DevExpress WinForms .NET app, BoxedApp + DNGuard-HVM wrapped, ~92 MB. Authored by PureCoder. | The C2 panel UI — login, builder settings, connections list, feature dispatch. Talks to `api.purecoder.io` for licensing and stub building. |
| **PureServer.exe** | Native C++ (VMProtect-flavour), 10.5 MB. Authored by PureCrack (third-party crackers). | Local replacement for PureCoder's online build service. Reads `data.pak`, runs `csc.exe` and `dotNET_Reactor.exe`, returns a built stub when the panel hits `/compile`. |
| **data.pak** | 354,962 B PPAK-magic AES container. | Encrypted bundle of the inner stub source code (32 .cs files). |
| **PureHelper.dll** (plugin) | 33 MB .NET assembly. | Panel-side `ICustomPlugin` that auto-injects 37 client feature DLLs (Keylogger, HVNC, RemoteDesktop, etc.) into PureRAT's plugin registry on UI startup. |

## 2. Wire layer

PureRAT's licensing API is just HTTPS to `api*.purecoder.io`:

- TLS to port 443. Hosts file is expected to redirect those names to 127.0.0.1
  for local intercept (already configured on this VM).
- HTTP body: `[16-byte IV][AES-256-CBC ciphertext]` with a static AES-256 key
  `e6c43cc05d35fee7c8533d96203eeda357c65e85e30dbe622fad26fdfbb222a8`.
  Same key across panel versions v9572 → v9598.
- Inner payload: protobuf-net messages, no length framing beyond the
  Content-Length header.

Four endpoints:

| Endpoint | Behavior |
|----------|----------|
| `/api/licence/validate` | Login. Any serial accepted by the relay. Response includes the agent PFX wrapped in a fixed `outer[2]` shape. |
| `/api/licence/compile` | Build a stub. Panel sends IPs/Ports/Group/server cert/PFX/mutex/startup. Server returns a full .NET stub PE wrapped as `outer[4]{f1=1, f2="", f3=<pe-bytes>}`. |
| `/api/licence/heartbeat` | Periodic ACK. Minimal `{f1=1}` response. |
| `/api/licence/update-plugins` | Same as heartbeat in our handling. |

## 3. Stub architecture

A built stub is a layered .NET PE:

```
outer.exe   (compiled from Loader.cs, csc /target:winexe /platform:x86,
             optionally protected with dotNET_Reactor)
├── embedded resource "PayloadSource.zip"
│     content = 3DES-CBC-PKCS7( [4-byte LE size] || gzip(inner.dll) )
├── embedded resource "protobuf-net.dll"
└── Loader code:
      1. Read "PayloadSource.zip" resource
      2. 3DES decrypt with key+IV baked in as base64 string literals
      3. Strip 4-byte size prefix; gunzip
      4. Assembly.Load(raw)
      5. Walk types; invoke first no-arg public-static-void method
inner.dll  (compiled from 32 .cs sources + protobuf-net.dll reference)
├── Entry: GClass0.smethod_0()
├── Class9.smethod_3: deserialize embedded base64+gzip+protobuf config
│                     → GClass3 (IPs, Ports, ServerCert, Group, Mutex, ...)
└── Class9.smethod_4: mutex check → optional persistence thread →
                       TLS connect (TLS 1.0, no client cert) →
                       cert pin via .Equals(panel cert) →
                       send GClass4 ClientInformation length-prefixed →
                       enter command read loop
```

The cert pin is the critical detail: `Class9.smethod_8` does
`bakedCert.Equals(serverCert)`. If they don't match exactly,
`AuthenticateAsClient` throws and the stub disconnects without registering. The
relay handles this by extracting the panel's PFX (field 10) from the `/compile`
request and baking it into the stub.

## 4. GClass3 — per-build config schema

protobuf-net wire format. Wrapped as a `GClass2` with `[ProtoInclude(38, typeof(GClass3))]`.

| ProtoMember # | Type | Field name | Meaning |
|---|------|------------|---------|
| 1 | repeated string | `List_0` | C2 IPs |
| 2 | repeated int32 | `List_1` | C2 ports |
| 3 | string | `String_0` | base64 of X509 cert / PFX (the cert the stub pins) |
| 4 | string | `String_1` | Group label |
| 5 | bool | `Boolean_0` | auto-start persistence thread |
| 6 | bool | `Boolean_1` | run anti-debug (Class3.smethod_3) |
| 7 | string | `String_2` | persistence filename (or "") |
| 8 | string | `String_3` | persistence folder env var, e.g. "APPDATA" |
| 9 | string | `stadrmoOn1` | mutex name |
| 10 | bool | `Boolean_2` | feature flag |

Wire encoding is `0xB2 0x02 <varint length> <body>` — that's
`(38 << 3) | 2 = 306` = `0xB2 0x02` in varint, then the GClass3 body. Skipping
the wrapper produces `Invalid wire-type` in `proto_3 → ReadString` at runtime.

## 5. The key RE move

`PureServer.exe` is native C++ with VMProtect-style virtualization. Strings in
`.rdata` had no XREFs from `.text` because code flow goes through a VM handler.
Static decryption of `data.pak` was a dead end (AES key hidden in the VM
state machine).

Dynamic recovery instead:

1. Run `PureServer.exe` normally with a valid license activated.
2. `procdump -ma <pid> dump.bin` while it's idle waiting for compile requests.
3. Search the dump for the ZIP local-file-header signature `PK\x03\x04`. The
   inner source bundle is sitting fully decrypted in the heap because PureServer
   has to write the .cs files to a temp dir to feed `csc.exe`.
4. Carve from first LFH to the EOCD record. 43,127 bytes — exactly matching
   the `0xa877` length field after `data.pak`'s `PPAK` magic (so this confirms
   the recovery is the full payload).

What came out:
- 32 `.cs` files with class names `Attribute0`, `Class0..16`, `GClass0..15`,
  `GEnum0` — these are what compile to the inner DLL.
- The Loader.cs template, assembled from string fragments in `PureServer.exe`'s
  `.rdata`.
- The exact csc compile flags PureServer uses.
- The exact dotNET_Reactor protection flags.

This generalizes: **any time a packed/protected program needs to use the data
it's protecting, that data is observable to a sufficiently privileged dynamic
observer**. AES on `data.pak` only stops people who don't have execution
access. Everyone with a valid license has execution access, so the encryption
is theatre against the user community.

## 6. Python replay

`build.py` reproduces the pipeline 1:1:

```
config (dict)
  ├─ protobuf-net wire encode (GClass3 wrapped in GClass2 ProtoInclude(38))
  ├─ gzip
  ├─ base64
  ├─ substitute the placeholder "H4sIAAAAAAAACgMAAAAAAAAAAAA=" in inner/Class9.cs
  ├─ csc 32 .cs files + protobuf-net.dll ref → inner.dll (~72-79 KB)
  ├─ [4-byte LE size] || gzip(inner.dll)
  ├─ 3DES-CBC-PKCS7 encrypt with random 24-byte key + 8-byte IV
  ├─ generate Loader.cs from template, key+IV baked in as base64 literals
  ├─ csc Loader.cs with /resource:encrypted /resource:protobuf-net.dll
  └─ optional: dotNET_Reactor on outer EXE only
              (protecting the inner breaks Assembly.Load(byte[]) with 0xE0434352)
```

Single API: `build.build(config, out_exe, protect=False)`. Same input → byte-similar output across runs (modulo the random 3DES key).

## 7. Bugs hit and fixed during development

| Symptom | Root cause | Fix |
|---------|------------|-----|
| `Invalid wire-type` in `proto_3 → ReadString` at stub init | Skipped GClass2 ProtoInclude(38) wrapper | Always prefix `0xB2 0x02 <varlen>` before GClass3 body |
| Stub TCP-connects but never registers | `Class9.smethod_8` does strict `.Equals()` cert match | Extract panel PFX from /compile request, bake into GClass3.String_0 |
| Inner protected with Reactor exits 0xE0434352 | Reactor's native module-init fails when DLL is loaded via `Assembly.Load(byte[])` rather than from disk | Protect only the outer EXE; inner stays raw inside the encrypted resource |
| CMD window flashes during build | Detached relay (no console) → csc child opens its own console | `creationflags=CREATE_NO_WINDOW` + `stdin=DEVNULL` on every subprocess |
| Relay log truncated mid-`/compile` | Buffered stdout, parent killed before flush | `sys.stdout = io.TextIOWrapper(... line_buffering=True)` |
| Build subprocess hangs opaquely | `subprocess.run(capture_output=True)` blocks until done with no progress | Replaced with `Popen + readline()` streaming live output, monotonic-clock deadline |
| UI automation can't see panel descendants | Python at Medium integrity, panel at High → UIPI blocks UIA tree access | Self-elevate via ShellExecute `runas` (silent under UAC=0) |
| Panel rejects relay TLS handshake | Stale relay cert in root store collides with fresh one | `certutil -delstore Root` to clear, regenerate, re-add |

## 8. License relay

`relay.py` listens on `0.0.0.0:443` with a self-signed TLS cert covering
`api*.purecoder.io`. Handles all four endpoints. The interesting one is
`/compile`:

1. Decrypt body with the static AES key.
2. Walk the protobuf tree:
   `req.field[3]` is the inner request body
   `req[3].field[5]` is BuildSettings
   `req[3][5].field[9]` is the GClass3-shaped build config
3. Extract `ips`, `ports`, `cert_b64` (field 3), `panel_pfx_b64` (field 10),
   `group`, `mutex`, `startup_name`, `startup_env`.
4. Prefer the full PFX over the plain cert so `.Equals()` passes against the
   panel's TLS listener.
5. Call `build.build(cfg, out_exe, protect=...)`.
6. Read produced PE, wrap as `outer[4]{f1=1, f2="", f3=<pe>}`, AES-encrypt,
   return.

Fallback: if dynamic build raises, serve `data/compile_response.bin` (a
Frida-captured real PureServer response). This has frozen cert/config from
capture time, so produced stubs won't actually register against arbitrary
panels — present for debugging the wire layer only.

## 9. End-to-end orchestrator

`e2e.py` two modes:

- **Launch mode** (`python e2e.py`): clean stale procs → start relay detached
  → ShellExecute panel → wait for window → triple-fallback Enter on Login
  (pywinauto + WScript.Shell + Win32 PostMessage) → wait for 56001 → TLS-probe
  panel cert → print READY. Stays running until Ctrl-C.

- **Self-test** (`python e2e.py --test`): same setup, then build a verification
  stub with the probed cert, run it, watch ESTABLISHED count on 56001 for the
  +1 delta. Confirmed live in this session as
  `Connected: User[WINDEV2407EVAL] @ 127.0.0.1` in the panel's Server Logs.

There's also `--build` which tries to drive Builder Settings → Build button →
SaveFileDialog via pywinauto. Self-elevates because PureRAT runs at High
integrity and UIPI blocks Medium-integrity access. UI traversal works
(reaches the Build button), but DevExpress SimpleButton's Click event doesn't
reliably fire from UIA Invoke. Recommended path is launch mode + manual click.

## 10. Test suite

`tests.py` has three commands:

- `python tests.py robust` — 4 configs in sequence (`clean_default`,
  `custom_group`, `multi_port`, `unicode_group`), each builds a stub, runs it
  against a Python mock-panel using the same self-signed PFX on both ends,
  captures the registration packet, decodes protobuf to verify the Group field
  round-trips. Currently 4/4 pass.
- `python tests.py protected` — single config with `protect=True` to exercise
  the dotNET_Reactor pass.
- `python tests.py one` — single-config verbose E2E with extra logging.

## 11. Why static analysis didn't work where dynamic did

VMProtect makes the native code unreadable to standard tools — de4dot bombed
with `BadImageFormatException`, Ghidra would have needed days for the VM
handlers. But the *output* of that code — the decrypted ZIP, the assembled
Loader.cs, the CLI flag literals — all sits in plaintext somewhere observable
at runtime. Dumping RAM turned a week-long static problem into a ten-minute
grep.

PureCoder + PureCrack invested heavily in making static reverse engineering
expensive. They invested almost nothing in making dynamic recovery hard
(beyond anti-debug, which is bypassable and was bypassed by procdump). This is
common in commercial protectors: they protect against the threat model of
"someone has the binary but not the activation," and shrug at "someone has the
activation."

## 12. Final layout

```
PureCrack-Workspace/
├── README.md             operator quickstart
├── docs/
│   ├── OVERVIEW.md       this file
│   ├── WIRE_FORMAT.md    protobuf field maps for the four endpoints
│   └── MEMORY_DUMP_RECIPE.md   refresh inner sources from a new PureCrack drop
├── relay.py              TLS license relay + dynamic /compile handler
├── build.py              builder + cert prober + CLI
├── e2e.py                autonomous orchestrator
├── tests.py              mock-panel test suite
├── inner/                32 .cs sources + protobuf-net.dll
├── data/                 agent_cert.pfx, compile_response.bin, relay TLS cert
└── runs/                 captures, e2e logs, stubs, test artifacts
```

Four Python files + three dirs + README + docs. Everything resolves paths from
`__file__`. `PURECRACK_WORKSPACE` env var overrides if you relocate.
