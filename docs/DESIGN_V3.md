# PureCrack v3 — Lifetime Kit Design

**Status:** approved 2026-04-28, in implementation
**Goal:** v3 is a single-binary lifetime crack — one EXE, no wrapper scripts,
no manual setup, no support tickets six months from now. Targets PureRAT
v4.0.9596 forever; if PureCoder ships a panel update that the operator
accidentally installs, v3 detects it and refuses to launch with a clear
error rather than silently breaking.

## What changed from v2

v2 had a working relay + dynamic stub builder, but operators had to run
`Launch.bat` → `Launch.ps1` first to set up the host environment (hosts
file, .NET strong-crypto regkeys, TLS cipher suites, MOTW strip,
PureHelper plugin path injection, kill stale PureRAT). `PureCrack.exe`
alone wouldn't work on a fresh box.

v3 absorbs all of `Launch.ps1`'s work into the binary itself, plus adds
self-healing for environment drift over time. The shipped distribution
becomes:

```
PureRatCrack-Crypter.Shop/
├── PureCrack.exe        (single binary, runs everything)
├── PureCrack.exe.config (binding redirects)
└── panel/               (frozen v4.0.9596 panel + plugins)
```

No `Launch.bat`. No `Launch.ps1`. No `data/` folder shipped — created at
runtime. No README chasing.

## Launch sequence

```
PureCrack.exe
  ├─ UAC self-elevate (manifest)
  ├─ Banner + embedded-asset sanity check
  │
  ├─ Provision (NEW, idempotent, every launch)
  │   ├─ MotwStripper       — recursive Zone.Identifier ADS removal
  │   ├─ NetFrameworkCrypto — SchUseStrongCrypto + SystemDefaultTlsVersions
  │   ├─ TlsCipherSuites    — Enable 7 cipher suites via BCrypt P/Invoke
  │   ├─ ProcessReaper      — kill stale PureRAT.exe (mutex collision avoidance)
  │   └─ TimeSanity         — warn loudly on obvious clock skew
  │
  ├─ Preflight (existing, slimmed)
  │
  ├─ AssetVerify (NEW — frozen-target enforcement)
  │   ├─ SHA256 panel/PureRAT.exe
  │   ├─ SHA256 panel/data.pak
  │   ├─ SHA256 panel/Plugins/PureHelper.dll
  │   └─ SHA256 panel/Plugins/PureHelper.Client.dll
  │   refuse to launch on mismatch (tells operator panel was updated)
  │
  ├─ HostsManager.Ensure (extended — 7 domains, was 3)
  │
  ├─ CertManager (existing, hardened)
  │   ├─ atomic temp+rename writes
  │   └─ cross-process file mutex on data/
  │
  ├─ CapturePrune (NEW — TTL-based cleanup of runs/captures/, 14d default)
  │
  ├─ SettingsAutoFix.ReorderIps + FixPluginPaths
  │
  └─ Launch panel + relay (existing, loopback-bound)
```

## New subcommands

| Subcommand | Purpose |
|------------|---------|
| (default)  | Full lifetime launch (above pipeline) |
| `verify`   | Run all integrity checks, no launch (read-only diagnostic) |
| `provision`| Run Provision steps only (one-shot host setup) |
| `selftest` | Run synthetic /validate + /compile through the relay; verify byte-for-byte responses against baked-in baseline |
| `smoke-build` | (existing) StubBuilder pipeline only |
| `doctor`      | (existing) Read-only state report — extended w/ asset hash report |
| `cleanup`     | (existing) Remove all PureCrack state — extended to undo regkeys + ciphers we set |
| `help`        | Usage |

## Frozen baseline (panel hashes)

v3.0.0 ships with these expected SHA-256 hashes:

| File | SHA-256 |
|------|---------|
| panel/PureRAT.exe              | `f7792cde754de2ec0023d2c4fad3592d394cdaa8ff011f3188989642d9adbaa6` |
| panel/data.pak                 | `efee5150ae55013540e5dccf8c95faf7e82d33e0c8c66d246c76b49876c78c6e` |
| panel/Plugins/PureHelper.dll   | `4d13c13d45e24baecc8359a9535f5a2ccdcf1249a56daaef8e42feb1491fa8ce` |
| panel/Plugins/PureHelper.Client.dll | `ed64609ee64110d3b7dd7d719b2b9dac2885b3e1c4e8154ff250fb4325b2648b` |

These are baked into source as `const string` in `Verify/ExpectedHashes.cs`.
A future kit release that bundles updated panel binaries bumps these
constants and a kit version. v3 itself stays valid against
v4.0.9596.35655 indefinitely.

## File-level changes

### New files

```
src/Provisioning/MotwStripper.cs
src/Provisioning/NetFrameworkCrypto.cs
src/Provisioning/TlsCipherSuites.cs
src/Provisioning/ProcessReaper.cs
src/Provisioning/TimeSanity.cs
src/Provisioning/Provision.cs        — orchestrator
src/Verify/AssetVerifier.cs
src/Verify/ExpectedHashes.cs
src/Verify/CapturePrune.cs
src/Verify/SelfTest.cs
src/Util/AtomicFile.cs
src/Util/KitMutex.cs
docs/DESIGN_V3.md                    — this doc
```

### Modified files

```
PureCrack.csproj                     — version 3.0.0, ref System.Security (already)
src/Program.cs                       — pipeline + subcommands restructured
src/Setup/HostsManager.cs            — Domains[] 3 → 7
src/Setup/CertManager.cs             — atomic writes, file mutex
src/Preflight.cs                     — slimmed (Provision now handles env)
README.md                            — single-binary launch
panel/data/Settings.json             — already cleared (v2 patch)
```

## Why each new piece exists

- **MotwStripper** — operators extract from a downloaded zip; every file
  picks up a `:Zone.Identifier` NTFS ADS that .NET treats as remote. The
  panel fails to load PureHelper.dll with HRESULT 0x80131515. Stripping
  the ADS removes the block. Idempotent.

- **NetFrameworkCrypto** — `SchUseStrongCrypto=1` and
  `SystemDefaultTlsVersions=1` under `HKLM\SOFTWARE\Microsoft\.NETFramework\v4.0.30319`
  (and Wow6432Node mirror) force .NET 4.x apps to negotiate TLS 1.2.
  Without this the panel's TLS handshake fails on Win11 default config.
  One-shot but always-checked.

- **TlsCipherSuites** — Win11 disables a half-dozen DHE-RSA / 3DES suites
  by default. The panel speaks one of those. Enable on launch via
  `BCryptAddContextFunction`. Idempotent (already-enabled → no-op).

- **ProcessReaper** — PureRAT uses a process-wide named mutex; two copies
  collide. If the previous panel session crashed without cleanup, the
  next launch silently fails. Killing stale PureRAT processes before
  Launch.Panel() is the cheapest fix.

- **TimeSanity** — `DateTime.UtcNow` reading something obviously wrong
  (year < 2026 or > 2050) means the OS clock is broken. Cert validity
  checks would misbehave. Loud warning, not a refusal.

- **AssetVerifier** — frozen-target enforcement. The whole point of the
  lifetime guarantee. Computed inside the binary with SHA-256 of
  `File.ReadAllBytes`. On first failure the launch aborts with an exact
  expected-vs-got hash diff and a "your panel was updated" message.

- **CapturePrune** — `runs/captures/` accumulates over months. Pruning at
  14 days keeps the kit usable forever without disk fill. TTL is a
  constant; lifetime kit doesn't need it configurable.

- **SelfTest** — synthetic `/validate` + `/compile` round-trip through
  the relay against a baked-in expected response. Anyone can run
  `PureCrack.exe selftest` to know "is this kit still working?" without
  needing a panel running. Catches silent breakage from BCL changes,
  Roslyn version skew, my own bugs.

- **AtomicFile** — `WriteAllBytes` on a PFX, crash mid-write → corrupt
  PFX next launch → PFX regen is fine BUT the same pattern in any future
  state file would lose data. Temp+rename pattern eliminates the window.

- **KitMutex** — two PureCrack instances launched in parallel race on
  cert generation, regkey, hosts. Global named mutex, taken at launch,
  released on exit. First-run wins; second instance reports the existing
  PID and exits.
