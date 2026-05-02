# PureCrack — bundled panel

This folder is where PureCrack looks for its panel binary. The C# kit pins
to a single tested PureRAT version rather than searching arbitrary install
locations on the operator's box.

## Currently bundled

| File | Version | Purpose |
|------|---------|---------|
| `PureRAT.exe` | v4.0.9596.35655 | Panel binary (BoxedApp-packed) |
| `PureRAT.exe.config` | n/a | DevExpress UI + .NET 4.8 runtime config |
| `data.pak` | n/a | Asset bundle (348 KB) — PureRAT requires at startup |
| `data/GeoIP.mmdb` | n/a | GeoIP database (9.2 MB) — for VNC geolocation |
| `Plugins/PureHelper.dll` + `.Client.dll` | n/a | Bundled plugin (33 MB) |
| `Clients/` | empty | Populated at runtime as bots register |

We tried v4.0.9598 (newer). The bare-binary download we have for 9598
boots fine on a fresh install but won't run when paired with 9596's
companion files — it expects a different `data.pak` / `Plugins/` layout
than 9596 ships. Without a complete 9598 install kit, we're staying on
9596, which is verified end-to-end against our relay.

`PureRAT.exe` is **not in git** (88 MB; gitignored). On a fresh clone you
need to drop a copy here yourself — see *Installing* below. The `.config`
file is tracked so the version reference and binding-redirects are visible
in source control.

## Installing

If you cloned this repo, `PureRAT.exe` is missing. Copy it from a clean
v4.0.9598 install:

```powershell
Copy-Item "<path to clean PureRAT install>\PureRAT.exe" "<repo>\panel\PureRAT.exe"
```

Or override the lookup with the env var (no copy needed):

```cmd
set PURE_PANEL_EXE=C:\path\to\some\PureRAT.exe
PureCrack.exe
```

## Why this version

The C# relay's wire format was developed against v4.0.9596 (verified live
end-to-end). v4.0.9598 is two builds newer, with no documented protobuf
schema changes, and was confirmed compatible during initial bring-up.

Older versions (9572 etc.) likely also work — the protobuf-net wire format
hasn't churned across v4.0.x. Newer versions (v4.1+) are unverified;
expect a relay rebuild against the new wire layout if PureCoder ever bumps
the major.

## What gets created here at runtime

PureRAT itself writes `Settings.json` (and possibly `Data/` for GeoIP
state, `Plugins/` for installed plugins) into this folder on first run.
All of those are gitignored — they're per-operator state, never sources.

`SettingsAutoFix` from PureCrack will edit `Settings.json` on launch to
ensure `127.0.0.1` is first in the IPs array (so stubs hit the local
relay instead of a possibly-stale LAN address).
