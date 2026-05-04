# PureCrack v3 — single-binary lifetime kit

PureRAT v4.0.9596 licence relay + dynamic stub builder + host provisioner
+ frozen-target verifier, all in one EXE. v3's design goal is **lifetime
stability**: the kit keeps working against `v4.0.9596.35655` indefinitely,
self-heals environment drift, and refuses to launch silently against a
panel that's been updated out from under it.

## Quick start (operator)

1. Extract the distribution somewhere. Tree:

   ```
   PureRatCrack-Crypter.Shop/
   ├── PureCrack.exe          ← run this
   ├── PureCrack.exe.config
   └── panel/                 ← bundled v4.0.9596 panel
       ├── PureRAT.exe
       ├── PureRAT.exe.config
       ├── data.pak
       ├── data/
       │   └── GeoIP.mmdb
       └── Plugins/
           ├── PureHelper.dll
           └── PureHelper.Client.dll
   ```

2. Right-click `PureCrack.exe` → **Run as administrator**.
   The manifest forces a UAC prompt; nothing else needed.

3. Watch the launch sequence:
   ```
   :: provision         ← MOTW strip, regkeys, TLS ciphers, stale-PID kill
   :: preflight         ← admin / port / hosts / panel / Roslyn check
   :: asset verify      ← SHA-256 vs frozen v4.0.9596 baseline
   :: hosts + certs     ← 7 loopback redirects, leaf cert in Root
   :: relay             ← LISTEN on 127.0.0.1:443 (loopback-only)
   :: settings + panel  ← IPs reorder, plugin path repair, panel launch
   READY                ← go click Login in the panel
   ```

4. Click **Login** (any key — relay returns OK). Then **Builder Settings
   → Build** to produce a stub. Stubs land in `runs/stubs/`; every
   licence-API request lands in `runs/captures/`.

5. `Ctrl-C` in the PureCrack window stops the relay. Panel keeps running.

That's it. No `Launch.bat`. No `Launch.ps1`. No two-step setup.

## What v3 changed from v2

| Was | Now |
|-----|-----|
| Operator runs `Launch.bat` → `Launch.ps1` first to set up the host, then `PureCrack.exe` | Single binary does everything via the **provision** step at startup |
| Ad-hoc setup (MOTW strip, regkeys, TLS ciphers, stale PID kill) lived in PowerShell | Absorbed into `src/Provisioning/` as proper C# modules — idempotent, every launch |
| Hosts file: 3 entries | 7 entries (also `us./eu.purecoder.{io,su}`) |
| Relay bound `0.0.0.0:443` | Loopback-only by default; `PURECRACK_BIND_ALL=1` to override |
| Panel cert was a CA:TRUE 10-year cert with KeyCertSign | Plain server-auth leaf, 1y validity, Server-Auth EKU only |
| PFX files unencrypted on disk | DPAPI-wrapped (LocalMachine scope), legacy files migrate on read |
| No verification of panel binary | SHA-256 verify against frozen v4.0.9596 baseline; refuse to launch on drift |
| `Settings.json` had a hardcoded dev-machine plugin path | `FixPluginPaths` rewrites to absolute on this machine every launch |
| `runs/captures/` accumulated forever | TTL-based prune (14d) at startup |
| File writes raced on crash → corrupt PFX/Settings.json/hosts | All writes via `AtomicFile.WriteAll*` (temp + rename) |
| Two PureCrack instances could race on shared state | Cross-process `KitMutex` — second instance refused with PID of holder |
| No way to tell if the kit was still working without launching | `PureCrack.exe selftest` — wire-format + crypto round-trip checks |

## Subcommands

```
PureCrack.exe                  Default — full kit launch (provision + verify + relay + panel)
PureCrack.exe verify           SHA-256 baseline check vs panel/, no launch
PureCrack.exe provision        Host setup only (MOTW strip + regkeys + ciphers + stale-kill)
PureCrack.exe selftest         Wire-format and crypto round-trip checks
PureCrack.exe smoke-build      Exercise the StubBuilder pipeline only (CI test)
PureCrack.exe doctor           Read-only state report (hosts / certs / data files / hash drift)
PureCrack.exe cleanup          Remove all PureCrack state from this machine, including regkeys
PureCrack.exe help             Usage
```

## Environment overrides

| Var | Purpose |
|-----|---------|
| `PURECRACK_WORKSPACE`         | Override workspace root (default: EXE dir) |
| `PURECRACK_BIND_ALL=1`        | Bind relay on `0.0.0.0` (DANGEROUS — leaks PFX, exposes /compile) |
| `PURECRACK_SKIP_ASSET_VERIFY=1` | Force-launch against a drifted panel (DANGEROUS — wire format may differ) |
| `PURE_PANEL_EXE`              | Override path to PureRAT.exe |
| `PURE_SETTINGS_JSON`          | Override path to Settings.json |

## Layout

```
PureCrack-v3/
├── PureCrack.csproj
├── app.manifest                requireAdministrator
├── FodyWeavers.xml             Costura config (single-EXE embedding)
│
├── src/
│   ├── Program.cs              ~180 lines — entry, dispatch, crash log
│   │
│   ├── Cli/                    one file per subcommand
│   │   ├── DefaultCommand.cs   the full kit launch
│   │   ├── VerifyCommand.cs
│   │   ├── ProvisionCommand.cs
│   │   ├── SelfTestCommand.cs
│   │   ├── SmokeBuildCommand.cs
│   │   ├── DoctorCommand.cs
│   │   ├── CleanupCommand.cs
│   │   └── CertSubjects.cs     known DNs (cleanup/doctor share this)
│   │
│   ├── Provisioning/           absorbs Launch.ps1
│   │   ├── Provision.cs        orchestrator
│   │   ├── MotwStripper.cs     Zone.Identifier ADS removal
│   │   ├── NetFrameworkCrypto.cs SchUseStrongCrypto regkeys
│   │   ├── TlsCipherSuites.cs  Enable-TlsCipherSuite shellout
│   │   ├── ProcessReaper.cs    kill stale PureRAT
│   │   └── TimeSanity.cs       clock check
│   │
│   ├── Verify/                 frozen-target enforcement
│   │   ├── ExpectedHashes.cs   SHA-256 baseline (const strings)
│   │   ├── AssetVerifier.cs    hash + report
│   │   ├── CapturePrune.cs     TTL-based runs/captures/ cleanup
│   │   └── SelfTest.cs         wire-format and crypto round-trips
│   │
│   ├── Setup/
│   │   ├── HostsManager.cs     7 loopback redirects
│   │   └── CertManager.cs      relay + agent PFX, DPAPI-wrapped
│   │
│   ├── Crypto/
│   │   └── Symmetric.cs        AES-256-CBC + 3DES-CBC helpers
│   │
│   ├── Wire/
│   │   └── ProtoNet.cs         hand-rolled protobuf encoder/decoder + dump
│   │
│   ├── Build/
│   │   ├── BuildConfig.cs
│   │   ├── InnerProto.cs       GClass3 encoder + GClass2 wrap
│   │   └── StubBuilder.cs      Roslyn-based 5-step pipeline
│   │
│   ├── Relay/
│   │   ├── TlsRelay.cs         loopback-bound, origin-checked /compile
│   │   ├── RouteHandlers.cs    /validate /compile /heartbeat /update-plugins
│   │   └── CaptureWriter.cs    /api/licence/* only, atomic writes
│   │
│   ├── Panel/
│   │   ├── PanelLauncher.cs    locate + launch PureRAT.exe
│   │   └── SettingsAutoFix.cs  IPs reorder + plugin path repair
│   │
│   ├── Util/
│   │   ├── Workspace.cs
│   │   ├── EmbeddedAssets.cs
│   │   ├── Log.cs
│   │   ├── Polyfills.cs
│   │   ├── KitPorts.cs         shared port constants
│   │   ├── KitMutex.cs         cross-process instance lock
│   │   └── AtomicFile.cs       temp + rename writes
│   │
│   └── Preflight.cs            5-check startup sanity pass
│
├── assets/                     embedded into PureCrack.exe at build time
│   ├── inner/                  32 .cs sources + Loader.tmpl + protobuf-net.dll
│   └── compile_response.bin    canned /compile fallback
│
├── docs/
│   ├── DESIGN_V3.md            this version's design rationale
│   ├── PORT_NOTES.md           v2 design diary (kept for context)
│   ├── OVERVIEW.md             full RE narrative
│   ├── WIRE_FORMAT.md          field-level protobuf reference
│   ├── PROTECTION_ANALYSIS.md  why every prior protection layer failed
│   ├── MEMORY_DUMP_RECIPE.md
│   └── THREAT_INTEL.md
│
└── panel/                      bundled v4.0.9596 panel + plugins
```

## Building from source

```
dotnet restore
dotnet build -c Release
```

Output is `bin/Release/PureCrack.exe`. Costura embeds Roslyn + protobuf-net
+ System.Text.Json + the 32 inner sources + the canned /compile blob into
one file — no scattered DLLs alongside.

To verify the kit is still working after any source change:

```
PureCrack.exe selftest
```

Runs all wire-format and crypto round-trip checks without needing a panel.
Catches silent breakage from BCL changes, Roslyn version skew, or refactor
bugs in encode/decode paths.

## Frozen baseline

v3.0.0 is pinned to PureRAT `v4.0.9596.35655`. Expected file hashes are
in `src/Verify/ExpectedHashes.cs`. Any drift fails `verify` and aborts
the default launch. To bump the baseline (operator updates the panel):
recompute hashes from the new `panel/` files, paste into
`ExpectedHashes.cs`, bump `<Version>` in `PureCrack.csproj`, rebuild.

The current baseline:

| File | SHA-256 |
|------|---------|
| `panel/PureRAT.exe`                  | `f7792cde754de2ec0023d2c4fad3592d394cdaa8ff011f3188989642d9adbaa6` |
| `panel/data.pak`                     | `efee5150ae55013540e5dccf8c95faf7e82d33e0c8c66d246c76b49876c78c6e` |
| `panel/Plugins/PureHelper.dll`       | `4d13c13d45e24baecc8359a9535f5a2ccdcf1249a56daaef8e42feb1491fa8ce` |
| `panel/Plugins/PureHelper.Client.dll`| `ed64609ee64110d3b7dd7d719b2b9dac2885b3e1c4e8154ff250fb4325b2648b` |

## Troubleshooting

**`asset verify FAILED`** → your panel doesn't match the v4.0.9596
baseline. Either restore from a clean install, or — if you intentionally
updated the panel — update the kit's baseline (see *Frozen baseline*).
Set `PURECRACK_SKIP_ASSET_VERIFY=1` to force-launch anyway, but expect
wire-format weirdness.

**`another PureCrack instance is already running`** → exit the running
copy first. The PID is logged with the error.

**`relay LISTEN on 127.0.0.1:443 (loopback-only)`** → this is the new
default. Set `PURECRACK_BIND_ALL=1` only if you have a documented reason
(e.g. running the relay on a different host than the panel inside an
isolated VM).

**`legacy CA:TRUE cert detected — regenerating as plain leaf`** → harmless
one-time upgrade. The new cert is a plain server-auth leaf with a 1-year
validity. The panel will need to be re-trusted (it already is, the relay
re-installs into Root automatically).

## What this isn't

- **Not** a distribution-ready cracked binary. It's a research artifact —
  one well-documented EXE that reproduces the licence protocol so you can
  understand it. Same disclaimer as v2.
- **Not** an upstream licence server. Validation stays local.
- **Not** a protected binary. No anti-debug, anti-VM, anti-tamper, no
  obfuscation. See `docs/PROTECTION_ANALYSIS.md` for why those layers are
  structurally weak.

## Provenance

v3 of `PureCrack-CSharp/`. Same protocol surface as v2. The v2 design
diary lives at `docs/PORT_NOTES.md`; v3-specific design decisions are at
`docs/DESIGN_V3.md`. PureRAT `v4.0.9596.35655` verified end-to-end.
