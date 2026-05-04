# PureRAT — Threat Intel & Detection Reference

Defender-facing companion to the operator-framed RE in `OVERVIEW.md` and
`WIRE_FORMAT.md`. This doc treats PureRAT as a malware family and answers:
what does it do, what does it leave behind, and how do I detect or block it?

PureRAT is a commercial Windows .NET RAT authored by **PureCoder** and sold via
`api*.purecoder.io`. A cracked redistribution authored by **PureCrack** is sold
separately as `PureServer.exe + data.pak + PureHelper.dll`. Both produce
identical bots — the cracked kit only replaces the build service, not the
panel or the stub. Verified pivot version is **v4.0.9596** with stub version
string **`4.4.1`** (sent in the registration packet).

This document covers the **bot side**. Panel-side telemetry (operator
fingerprinting, license-key reuse, etc.) is out of scope here.

---

## 1. Capabilities inventory (MITRE ATT&CK)

Capabilities derived from the 32 `.cs` files in `inner/`. The stub is what
gets dropped on victims; the 37 feature DLLs in `PureHelper.dll` are loaded
dynamically by command (`GClass11` plugin install — see §1.10).

| ID | Tactic | Technique | Where in stub |
|----|--------|-----------|---------------|
| **T1071.001** | C2 | Web Protocols (TLS over TCP, custom protobuf inside) | `Class9.smethod_4` |
| **T1573.002** | C2 | Asymmetric Cryptography (TLS) | `Class9.smethod_4` |
| **T1090** | C2 | Multi-IP / multi-port failover | `Class9.smethod_5` |
| **T1132.002** | C2 | Non-standard Encoding (length-prefixed protobuf-net) | `Class9.h` |
| **T1568.001** | C2 | Fast Flux–style fallback host list, refreshed via runtime config (HKCU\Software\<HWID>) | `Class9.smethod_5`, `Class7` |
| **T1053.005** | Persistence | Scheduled Task/Job (`Register-ScheduledTask`, 5-min repetition) | `Class8.smethod_0` |
| **T1547.001** | Persistence | Drop copy in `%APPDATA%` or configured env var | `Class8.smethod_0` |
| **T1055** | Defense Evasion | Reflection-loaded inner assembly (`Assembly.Load(byte[])`) | Loader.cs (outer EXE) |
| **T1140** | Defense Evasion | Deobfuscate/Decode (3DES outer + GZip inner + base64 config) | Loader.cs, `Class9.smethod_3` |
| **T1027.002** | Defense Evasion | Software Packing (dotNET_Reactor, optional) | outer EXE, when `protect=True` |
| **T1497.001** | Defense Evasion | System Checks (admin role, AV product enum, webcam presence) | `Class4` |
| **T1518.001** | Discovery | Security Software Discovery (`SELECT * FROM AntiVirusProduct`) | `Class4.smethod_2` |
| **T1082** | Discovery | System Information Discovery (Win32_OS, HWID composite) | `Class4` |
| **T1033** | Discovery | System Owner/User Discovery | `Class4.d` |
| **T1057** | Discovery | Process Discovery (foreground window title) | `Class3.d` |
| **T1010** | Discovery | Application Window Discovery | `Class3.d` |
| **T1083** | Discovery | File and Directory Discovery (wallet/extension enum) | `Class3.smethod_0` |
| **T1217** | Discovery | Browser Information Discovery (30+ Chromium-based browsers enumerated) | `Class3.smethod_0` |
| **T1113** | Collection | Screen Capture (640×480 JPEG, configurable quality) | `Class4.i` |
| **T1056.001** | Collection | Keylogging (delivered as plugin via `GClass11`) | feature DLLs |
| **T1125** | Collection | Video Capture (webcam — feature DLL) | feature DLLs |
| **T1005** | Collection | Data from Local System (browser data dirs) | `Class3.smethod_0` |
| **T1657** | Collection | Financial Theft (47 wallet extensions + 11 desktop wallets enumerated) | `Class3.smethod_0` |
| **T1059.001** | Execution | PowerShell `-NoProfile -ExecutionPolicy Bypass -Enc <base64>` | `Class8.smethod_0` |
| **T1129** | Execution | Shared Modules (Assembly.Load reflection) | Loader.cs |
| **T1564.003** | Defense Evasion | Hidden Window (PowerShell launched with `WindowStyle = Hidden`, `CreateNoWindow = true`) | `Class8.smethod_0` |
| **T1112** | Defense Evasion | Modify Registry (config blob persisted at HKCU\Software\<HWID>) | `Class7` |

Notable absences (worth confirming with the panel UI but not seen in stub):

- No process hollowing or injection in the stub itself. All capability comes
  from in-process plugin DLLs loaded by reflection.
- No anti-VM checks (no CPUID brand string check, no MAC-prefix checks).
- Anti-debug is exactly one call: `SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED)`.
  This is anti-sleep, not anti-debug — it keeps the host awake while collecting.
- No process kill list (no targeting of analysis tools by name).
- No firewall manipulation in the stub.
- No UAC bypass in the stub. Persistence task is registered with the elevation
  level the stub already has; if non-admin, task gets registered without
  `-RunLevel Highest`.

### Crypto wallet target list (T1657)

This is one of the strongest fingerprints — the 47 Chrome extension IDs in
`Class3.smethod_0` are an identifying constellation. Full list:

| Wallet | Extension ID(s) |
|--------|-----------------|
| MetaMask | `nkbihfbeogaeaoehlefnkodbefgpgknn`, `ejbalbakoplchlghecdalmeeeajnimhm`, `djclckkglechooblngghdinmeemkbgci` |
| TronLink | `ibnejdfjmmkpcnlpebklmnkoeoihofec` |
| Binance Chain Wallet | `fhbohimaelbohpjbbldcngcnapndodjp` |
| Yoroi | `ffnbelfdoeiohenkjibnmadjiehjhajb`, `akoiaibnepcedcplijmiamnaigbepmcb` |
| Jaxx Liberty | `cjelfplplebdjjenllpjcblmjkfcffne` |
| BitApp Wallet | `fihkakfobkmkjojpchpfgcmhfjnmnfpi` |
| iWallet | `kncchdigobghenbbaddojjnnaogfppfj` |
| Terra Station | `aiifbnbfobpmeekipheeijimdpnlpgpp` |
| BitClip | `ijmpgkjfkbfhoebgogflfebnmejmfbml` |
| EQUAL Wallet | `blnieiiffboillknjnepogjhkgnoapac` |
| Wombat | `amkmjjmmflddogmhpjloimipbofnfjih` |
| Nifty Wallet | `jbdaocneiiinmjbjlgalhcelgbejmnid` |
| Math Wallet | `afbcbjpbpfadlkmhmclhkeeodmamcflc` |
| Guarda | `hpglfhgfnhbgpjdenjgmdgoeiappafln`, `acdamagkdfmpkclpoglgnbddngblgibo` |
| Coin98 Wallet | `aeachknmefphepccionboohckonoeemg` |
| Trezor Password Manager | `imloifkgjagghnncjkhggdhalmcnfklk` |
| EOS Authenticator | `oeljdldpnmdbchonielidgobddffflal` |
| Authy | `gaedmjdfmmahhbjefcbgaolhhanlaolb` |
| GAuth Authenticator | `ilgcnhelpchnceeipipijaljkblbcobl` |
| Authenticator | `bhghoamapcdpbohphigoooaddinpkbai` |
| TezBox | `mnfifefkajgofkcjkemidiaecocnkjeh` |
| Cyano Wallet | `dkdedlpgdmmkkfjabffeganieamfklkm` |
| Exodus Web3 | `aholpfdialjgjfhomihkjbmgjidlcdno`, `ghocjofkdpicneaokfekohclmkfmepbp` |
| BitKeep | `jiidiaalihmmhddjgbnbgdfflelocpak`, `okejhknhopdbemmfefjglkdfdhpfmflg` |
| Coinbase Wallet | `hnfanknocfeofbddgcijnmhnfnkdnaad` |
| Trust Wallet | `egjidjbpglichdcondbcbdnbeeppgdph`, `heaomjafhiehddpnmncmhhpjaloainkn` |
| XDEFI Wallet | `hmeobnfnfcmdkdcmlblgagmfpfboieaf` |
| Phantom | `bfnaelmomeimhlpmgjnjophhpkkoljpa` |
| MOBOX WALLET | `fcckkdbjnoikooededlapcalpionmalo` |
| XDCPay | `bocpokimicclpaiekenaeelehdjllofo` |
| ICONex | `flpiciilemghbmfalicajoolhkkenfel` |
| Solana Wallet | `hfljlochmlccoobkbcgpmkpjagogcgpk` |
| Swash | `cmndjbecilbocjfkibfbifhngkdmjgog` |
| Finnie | `cjmkndjhnagcfbpiemnkdpomccnjblmj` |
| Keplr | `dmkamcknogkgcdfhhbddcghachkejeap` |
| Liquality Wallet | `kpfopkelmapcoipemfendmdcghnegimn` |
| Rabet | `hgmoaheomcjnaheggkfafnjilfcefbmo` |
| Ronin Wallet | `fnjhmkhhmkbjkkabndcnnogagogbneec` |
| ZilPay | `klnaejjgbibmhlephnhpmaofohgkpgkd` |
| Braavos Smart Wallet | `hkkpjehhcnhgefhbdcgfkeegglpjchdc` |
| Waves Keeper | `mijjdbgpgbflkaooedaemnlciddmamai` |

Plus desktop wallets via path probe: Atomic Wallet, Bitcoin-Qt, Dash-Qt,
Electrum, Ethereum (`%APPDATA%\Ethereum\keystore`), Exodus
(`%APPDATA%\Exodus\exodus.wallet`), Jaxx (`%APPDATA%\com.liberty.jaxx`),
Litecoin-Qt, Zcash, Foxmail, Telegram Desktop, Ledger Live.

This list is **only collected as metadata** in the stub's registration packet
(GClass4.String_5). Actual exfil of wallet contents happens via plugin DLLs
delivered as `GClass11` payloads.

### Browser data targets (T1217)

30+ Chromium-fork browsers checked under `%LOCALAPPDATA%`:

```
Chromium\User Data, Google\Chrome\User Data, Google(x86)\Chrome\User Data,
BraveSoftware\Brave-Browser\User Data, Microsoft\Edge\User Data,
Tencent\QQBrowser\User Data, MapleStudio\ChromePlus\User Data,
Iridium, 7Star, CentBrowser, Chedot, Vivaldi, Kometa, Elements Browser,
Epic Privacy Browser, uCozMedia\Uran, Fenrir Inc\Sleipnir5,
CatalinaGroup\Citrio, Coowon, liebao, QIP Surf, Orbitum, Comodo\Dragon,
Amigo, Torch, Comodo, 360Browser, Maxthon3, K-Melon, Sputnik, Nichrome,
CocCoc\Browser, Uran, Chromodo, Mail.Ru\Atom
```

---

## 2. Network indicators

### 2.1 C2 channel (bot ↔ panel)

| Indicator | Value | Detection note |
|-----------|-------|----------------|
| Transport | TLS over TCP | — |
| TLS version | **TLS 1.0 only** (`SslProtocols.Tls`, hardcoded) | TLS 1.0 in 2026 traffic = strong anomaly. JA3 fingerprint will be very distinct from modern clients. |
| Server cert validation | Strict `X509Certificate2.Equals` against pinned cert | Cert in stub matches panel listener cert exactly. SSL inspection that re-signs will break the bot's TLS handshake — useful as a network defense. |
| Default cert subject | `CN=PureRAT Agent` | Self-signed in default kits. Unique CN. |
| Inactivity | Read/write timeout: 5 minutes | — |
| Heartbeat (`GClass8`) | Configurable `method_2(20, 60)` seconds | First sleep window 20-60s after registration. |
| Frame format | `[4-byte LE length][protobuf-net body]` | Body is `GClass2` wrapper with `[ProtoInclude(N, typeof(GClass4))]`. |
| Tag pattern (registration) | First wire byte: `0x0A` (field 1, type LEN) — wrapped registration | Static prefix on first packet after handshake. |
| Receive/send buffer | 512000 (`Class9.int_0`) | Distinctive, default Windows is 8192-65536. |
| Stub version field | `"4.4.1"` (`GClass4.QnsdsyyYrB`, ProtoMember 6) | Hardcoded in v4.0.9596 builds. Future builds may bump. |

### 2.2 Licensing channel (panel ↔ vendor)

The panel itself talks to PureCoder's backend. Detectable on user networks
where employees might be running cracked PureRAT (rare, but it happens):

| Indicator | Value |
|-----------|-------|
| FQDNs | `api.purecoder.io`, `api*.purecoder.io` (subdomain wildcards used) |
| URI paths | `/api/licence/validate`, `/api/licence/compile`, `/api/licence/heartbeat`, `/api/licence/update-plugins` |
| HTTP body shape | `[16-byte IV][AES-256-CBC ciphertext]`, key `e6c43cc05d35fee7c8533d96203eeda357c65e85e30dbe622fad26fdfbb222a8` (static across v9572 → v9598) |
| Inner payload | protobuf-net |
| Cracked-kit telemetry | `api-metrics-v2.jonesmaster227.workers.dev` (PureCrack's Cloudflare Worker for license enforcement) |

The static AES key on `api*.purecoder.io` traffic is publicly documented now.
PCAPs of an operator's panel can be **fully decrypted** by anyone who captures
them. This is a defender's gift — if you have NetFlow/PCAP from a host running
the panel, you can recover license keys, HWID, build configs, and the
delivered stub PE.

### 2.3 Likely victim → attacker traffic shape

```
[stub] --SYN-->     [C2:port from List_1]
[stub] <--SYN/ACK
... TLS 1.0 handshake (ClientHello with single cipher, low cipher count)
... Server cert is self-signed, often CN=PureRAT Agent
[stub] --[4-byte LE length][gzip(protobuf GClass2 wrapping GClass4)]-->
... idle 20-60s
[stub] --[4-byte LE length][protobuf GClass8]-->  (heartbeat with screenshot)
... ad-hoc commands from panel, length-prefixed
```

JA3 hash will be relatively low-entropy across PureRAT samples because the
TLS stack is .NET Framework's default with no customization. Multiple .NET
RATs share this JA3 — it's necessary but not sufficient.

### 2.4 Network blocks worth deploying

For corporate egress, block:

- All TLS to `api*.purecoder.io` and any subdomain of `purecoder.io`.
- All TLS to `*.workers.dev` paths beginning with `/api-metrics-v2/` (or just
  block the specific worker `api-metrics-v2.jonesmaster227.workers.dev` —
  others may exist for different PureCrack license cohorts).
- Outbound TLS 1.0 entirely (it shouldn't exist in 2026; if it does, it's
  legacy or malware).
- Outbound TCP to any IP where the served cert has `CN=PureRAT Agent`.

---

## 3. Host indicators

### 3.1 File system

| Path | Note |
|------|------|
| `%APPDATA%\<original_filename>.exe` | Default persistence drop |
| `%<env_var>%\<configured_name>` | If config sets `String_3`/`String_2`, drop goes there. Common: `APPDATA`, `LOCALAPPDATA`, `TEMP`. |
| Outer EXE size | ~600 KB - 2 MB depending on dotNET_Reactor pass |
| Outer EXE signing | Unsigned (cracked builds); could be signed by attacker if they bring their own cert |
| .NET resources in outer EXE | `PayloadSource.zip` (3DES-encrypted gzipped inner DLL), `protobuf-net.dll` |

### 3.2 Registry

| Key | Value | Purpose |
|-----|-------|---------|
| `HKCU\Software\<32-char hex HWID>` | binary values, names look hex-ish | Per-bot config cache (override C2 list, persist between sessions) |

The HWID is `MD5(ProcessorId + DiskSerial + RAMSerial + UserDomain + UserName).ToUpper()`.
It's stable per machine and uppercase hex — distinctive registry path shape.

### 3.3 Scheduled tasks

```
TaskName: <stub filename without extension>
Action.Execute: %APPDATA%\<filename>  (or configured env-var path)
Trigger: Once at registration time, repeating every 5 minutes
Settings:
  ExecutionTimeLimit: 0 (no limit)
  AllowStartIfOnBatteries: True
  DontStopIfGoingOnBatteries: True
RunLevel: Highest (only if registered by admin process)
```

Created via `powershell.exe -NoProfile -ExecutionPolicy Bypass -Enc <base64>`.
The encoded command (UTF-16 LE, base64) decodes to a `Register-ScheduledTask`
PowerShell line. Decoding any `-Enc` PowerShell command and matching for
`Register-ScheduledTask` + `RepetitionInterval (New-TimeSpan -Minutes 5)`
catches this very reliably.

### 3.4 Mutex

Mutex name is configurable per build (`GClass3.stadrmoOn1`). Default kits ship
with bot1/bot2/etc. but real campaigns will rotate. **Don't hunt on mutex name
specifically** — hunt on the mutex creation pattern combined with other
indicators.

### 3.5 Process tree

```
explorer.exe (or initial loader)
└── <stub>.exe
    └── powershell.exe -NoProfile -ExecutionPolicy Bypass -Enc <b64>   # persistence install (one-shot, exits)
```

After persistence is set, scheduled task launches `<stub>.exe` directly with
no parent. The `-Enc` PowerShell child process is the loud part — short-lived
but visible to EDR.

### 3.6 WMI access pattern

In quick succession, the stub queries:

- `root\cimv2`: `Win32_Processor.ProcessorId`
- `root\cimv2`: `Win32_DiskDrive.SerialNumber`
- `root\cimv2`: `Win32_PhysicalMemory.SerialNumber`
- `root\SecurityCenter2`: `SELECT * FROM AntiVirusProduct`
- `root\cimv2`: `SELECT Caption FROM Win32_OperatingSystem`
- `root\cimv2`: `SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')`

Five-to-six WMI queries spanning `cimv2` and `SecurityCenter2` from a single
process within ~1s of process start = strong telemetry signal. EDR with WMI
visibility (Sysmon Event ID 19/20/21, Defender ATP, CrowdStrike) sees this
clearly.

---

## 4. YARA rules

These match the inner DLL after Assembly.Load (i.e., after the outer EXE
unpacks itself). For pre-execution scanning of outer EXEs, the encrypted
resource defeats most string-based YARA — see §4.3 for outer-EXE rules that
target the loader stub instead.

### 4.1 Inner DLL (high confidence)

```yara
rule PureRAT_Inner_DLL_v4 {
    meta:
        description = "PureRAT inner agent DLL (post Assembly.Load)"
        author      = "threat-intel"
        date        = "2026-04-24"
        family      = "PureRAT"
        version     = "v4.0.9596 / stub 4.4.1"
        confidence  = "high"

    strings:
        // Obfuscation token chained from every static ctor
        $init_tok      = "kLjw4iIsCLsZtxc4lksN0j" ascii wide

        // Persistence helper with distinctive obfuscated method name
        $pers_method   = "pwfVayjWiK" ascii wide

        // GClass3 mutex field name (rare token)
        $mutex_field   = "stadrmoOn1" ascii wide

        // Class9 method names (network/cert)
        $netmeth_smfd  = "smFdyqYylo" ascii wide
        $netmeth_qns   = "QnsdsyyYrB" ascii wide

        // Wallet enumeration constellation — any 5 hits = strong PureRAT
        $w_metamask    = "nkbihfbeogaeaoehlefnkodbefgpgknn" ascii
        $w_tronlink    = "ibnejdfjmmkpcnlpebklmnkoeoihofec" ascii
        $w_binance     = "fhbohimaelbohpjbbldcngcnapndodjp" ascii
        $w_phantom     = "bfnaelmomeimhlpmgjnjophhpkkoljpa" ascii
        $w_keplr       = "dmkamcknogkgcdfhhbddcghachkejeap" ascii
        $w_ronin       = "fnjhmkhhmkbjkkabndcnnogagogbneec" ascii
        $w_braavos     = "hkkpjehhcnhgefhbdcgfkeegglpjchdc" ascii
        $w_zilpay      = "klnaejjgbibmhlephnhpmaofohgkpgkd" ascii

        // Persistence command shape
        $sched_cmd     = "Register-ScheduledTask -TaskName" ascii wide
        $sched_int     = "RepetitionInterval (New-TimeSpan -Minutes 5)" ascii wide

        // Stub version (hardcoded)
        $version       = "4.4.1" ascii wide

    condition:
        uint16(0) == 0x5A4D                       // PE
        and 2 of ($init_tok, $pers_method, $mutex_field, $netmeth_smfd, $netmeth_qns)
        and 5 of ($w_*)
        and all of ($sched_cmd, $sched_int)
}
```

### 4.2 Wallet-stealer fingerprint (medium confidence, broader)

```yara
rule Crypto_Wallet_Enum_PureRAT_Family {
    meta:
        description = "Heuristic: 47-wallet extension list characteristic of PureRAT and forks"
        confidence  = "medium"
        note        = "May also fire on other commodity stealers that copy this list"

    strings:
        // Strong wallet IDs that appear together in PureRAT
        $w1  = "nkbihfbeogaeaoehlefnkodbefgpgknn"  // MetaMask
        $w2  = "ibnejdfjmmkpcnlpebklmnkoeoihofec"  // TronLink
        $w3  = "fhbohimaelbohpjbbldcngcnapndodjp"  // Binance
        $w4  = "ffnbelfdoeiohenkjibnmadjiehjhajb"  // Yoroi
        $w5  = "cjelfplplebdjjenllpjcblmjkfcffne"  // Jaxx
        $w6  = "bfnaelmomeimhlpmgjnjophhpkkoljpa"  // Phantom
        $w7  = "fnjhmkhhmkbjkkabndcnnogagogbneec"  // Ronin
        $w8  = "klnaejjgbibmhlephnhpmaofohgkpgkd"  // ZilPay
        $w9  = "dmkamcknogkgcdfhhbddcghachkejeap"  // Keplr
        $w10 = "hkkpjehhcnhgefhbdcgfkeegglpjchdc"  // Braavos
        $w11 = "mijjdbgpgbflkaooedaemnlciddmamai"  // Waves Keeper
        $w12 = "hgmoaheomcjnaheggkfafnjilfcefbmo"  // Rabet
        $w13 = "kpfopkelmapcoipemfendmdcghnegimn"  // Liquality
        $w14 = "akoiaibnepcedcplijmiamnaigbepmcb"  // Yoroi (alt)
        $w15 = "ejbalbakoplchlghecdalmeeeajnimhm"  // MetaMask (alt)

    condition:
        12 of them
}
```

### 4.3 Outer loader EXE (low-medium confidence)

The outer EXE is dotNET_Reactor-protected by default, which strips most
useful strings. What remains:

```yara
rule PureRAT_Outer_Loader {
    meta:
        description = "PureRAT outer loader EXE (Loader.cs compiled output)"
        confidence  = "low-medium"
        note        = "False positives expected; combine with network telemetry"

    strings:
        // Resource names baked in by csc
        $r1 = "PayloadSource.zip" ascii wide
        $r2 = "protobuf-net.dll"  ascii wide

        // dotNET_Reactor markers (also fires on other Reactor-packed binaries)
        $r3 = "<NetReactor>" ascii
        $r4 = ".NET Reactor"  ascii wide

    condition:
        uint16(0) == 0x5A4D
        and (all of ($r1, $r2))
        and 1 of ($r3, $r4)
}
```

The combination `PayloadSource.zip` + `protobuf-net.dll` as embedded
resources is reasonably specific even without other markers.

---

## 5. Sigma rules

### 5.1 PowerShell scheduled-task persistence (T1053.005 + T1059.001)

```yaml
title: PureRAT-style PowerShell Scheduled Task Persistence
id: 8b6e0a1c-2e10-4f11-8c72-f2a4ee10b9a1
status: experimental
description: Detects encoded PowerShell launching Register-ScheduledTask with 5-minute repetition (PureRAT, AsyncRAT, several others)
references:
  - https://attack.mitre.org/techniques/T1053/005/
logsource:
  product: windows
  category: process_creation
detection:
  selection_pwsh:
    Image|endswith:
      - '\powershell.exe'
      - '\pwsh.exe'
    CommandLine|contains|all:
      - '-NoProfile'
      - '-ExecutionPolicy'
      - 'Bypass'
      - '-Enc'
  filter_legit:
    ParentImage|endswith:
      - '\explorer.exe'
      - '\cmd.exe'
      - '\powershell_ise.exe'
  condition: selection_pwsh and not filter_legit
fields:
  - ParentImage
  - CommandLine
  - User
level: high
```

To catch the post-decode pattern specifically, run a follow-up correlation on
the decoded `-Enc` blob looking for `Register-ScheduledTask` +
`RepetitionInterval (New-TimeSpan -Minutes 5)`. Most EDRs decode `-Enc`
automatically.

### 5.2 Suspicious WMI security/AV enumeration sequence (T1518.001)

```yaml
title: Process Performs Sequential AV + Hardware WMI Queries
id: 4f1a8af0-4bc9-4e36-8d3d-9d8f6e9a3a7b
status: experimental
description: PureRAT and similar .NET RATs query AV product, OS, processor, disk serial, and webcam in close succession at process start
logsource:
  product: windows
  service: wmi
detection:
  q1:
    Operation: ExecQuery
    Query|contains: 'Win32_Processor'
  q2:
    Operation: ExecQuery
    Query|contains: 'AntiVirusProduct'
  q3:
    Operation: ExecQuery
    Query|contains: 'Win32_OperatingSystem'
  q4:
    Operation: ExecQuery
    Query|contains: 'Win32_PnPEntity'
  timeframe: 5s
  condition: all of q* | count() by ProcessId > 3
level: medium
```

### 5.3 Registry persistence under HWID-shaped key

```yaml
title: Registry Write to HKCU\Software\<32-char-hex> with Binary Value
id: a7c2e1d0-9b3e-4e1c-8d44-1e5a4d2b9c11
description: PureRAT writes per-bot config to HKCU\Software\<MD5(...)> as binary values
logsource:
  product: windows
  category: registry_event
detection:
  selection:
    EventType: SetValue
    TargetObject|re: 'HKCU\\Software\\[A-F0-9]{32}\\.+'
    Details|startswith: 'Binary Data'
  condition: selection
level: medium
```

### 5.4 Outbound TLS 1.0 connection to non-corporate destination

```yaml
title: TLS 1.0 Outbound to Internet
id: 1c0a3b62-7c11-46aa-8f31-2a9e0a3a7c91
description: Modern Windows apps don't use TLS 1.0; PureRAT hardcodes SslProtocols.Tls
logsource:
  product: zeek
  service: ssl
detection:
  selection:
    version: 'TLSv10'
    server_name|expand: '%internal_domains%'
  exclude:
    server_name|expand: '%legacy_corp_systems%'
  condition: selection and not exclude
level: medium
```

---

## 6. Hunt queries

### 6.1 Splunk / SIEM

```spl
# 1. Encoded PowerShell installing repeating scheduled tasks
index=windows EventCode=4688
| where match(CommandLine, "(?i)powershell.*-Enc\\s+[A-Za-z0-9+/=]{200,}")
| eval decoded=base64decode(replace(CommandLine, "(?i).*-Enc\\s+", ""))
| where match(decoded, "(?i)Register-ScheduledTask.*RepetitionInterval.*Minutes\\s*5")
| stats count by host, ParentImage, Image, decoded
```

```spl
# 2. PureRAT registry persistence under HWID-shaped key
index=sysmon EventID=13
| rex field=TargetObject "(?i)HKCU\\\\Software\\\\(?<hwid>[A-F0-9]{32})\\\\"
| where isnotnull(hwid) and Details="Binary Data"
| stats count earliest(_time) latest(_time) values(Image) by host, hwid
```

```spl
# 3. .NET process making outbound TLS 1.0 to a self-signed cert
# (requires Zeek + ja3 + Sysmon process telemetry merge)
index=zeek sourcetype=ssl version="TLSv10"
| join src_ip [search index=sysmon EventID=3 Image="*\\AppData\\*"]
| stats count by src_ip, dest_ip, dest_port, ja3_hash, server_name, Image
```

### 6.2 KQL (Defender ATP)

```kql
// Encoded PowerShell scheduling 5-minute repeat task
DeviceProcessEvents
| where InitiatingProcessFileName !in ("explorer.exe","cmd.exe")
| where FileName in ("powershell.exe","pwsh.exe")
| where ProcessCommandLine has_all ("-NoProfile","-Enc")
| extend EncodedCmd = extract(@"-Enc[^A-Za-z0-9+/=]+([A-Za-z0-9+/=]+)", 1, ProcessCommandLine)
| extend Decoded = base64_decodestring(EncodedCmd)
| where Decoded has "Register-ScheduledTask" and Decoded has "Minutes 5"
| project Timestamp, DeviceName, AccountName, InitiatingProcessFileName, Decoded
```

```kql
// Inner-DLL load with 47-wallet enumeration footprint
// Requires Defender to surface managed-assembly load events
DeviceImageLoadEvents
| where FileName endswith ".dll"
| where InitiatingProcessParentFileName has_any ("AppData","LocalAppData","Temp")
| join kind=inner (DeviceProcessEvents
    | where ProcessCommandLine has_any ("nkbihfbeogaeaoehlefnkodbefgpgknn",
                                          "ibnejdfjmmkpcnlpebklmnkoeoihofec",
                                          "bfnaelmomeimhlpmgjnjophhpkkoljpa")
) on DeviceId
```

### 6.3 Network (Zeek)

```zeek
# JA3 + cert subject filter
event ssl_established(c: connection) {
    if (c$ssl?$cert_chain && |c$ssl$cert_chain| > 0) {
        local subj = c$ssl$cert_chain[0]$x509$certificate$subject;
        if (/CN=PureRAT Agent/ in subj) {
            print fmt("PureRAT C2 candidate: %s -> %s:%d cert=%s",
                     c$id$orig_h, c$id$resp_h, c$id$resp_p, subj);
        }
    }
}
```

---

## 7. Mitigations

### 7.1 Preventive

- **App control**: Windows Defender Application Control (WDAC) or AppLocker
  to deny executables in `%APPDATA%`, `%LOCALAPPDATA%\Temp`, `%TEMP%`.
  Persistence drop fails outright. Single highest-leverage control.
- **PowerShell Constrained Language Mode** for non-admin user contexts.
  Breaks encoded `Register-ScheduledTask` install path on user-tier hosts.
- **Block TLS 1.0 outbound** at egress. PureRAT hardcodes `SslProtocols.Tls`
  (TLS 1.0 only). Newer builds may bump this — recheck on version increments.
- **TLS inspection / SSL re-signing** breaks the bot's cert pin. Even if
  inspection is bypass-listed for sensitive categories, malicious TLS gets
  caught.
- **Block FQDN list**: `purecoder.io`, `*.purecoder.io`,
  `api-metrics-v2.jonesmaster227.workers.dev` (and any other PureCrack
  worker URLs as they're discovered).

### 7.2 Detective

- Sigma rules in §5.
- Hunt queries in §6.
- Sysmon config that captures `EventID 1` (process), `EventID 13` (registry
  set), `EventID 19/20/21` (WMI), `EventID 22` (DNS), and `EventID 3`
  (network). PureRAT shows up across all five.
- Scheduled Task creation: Sysmon `EventID 4` audit + `Microsoft-Windows-TaskScheduler/Operational`
  `EventID 106` for any task with `RepetitionInterval` <= 5 minutes from a
  user-context process.

### 7.3 Responsive

If a PureRAT bot is found on a host:

1. **Don't kill the process first** — do memory dump first. The decrypted
   inner DLL, baked-in cert, and C2 IP/port list are all in RAM. Memory
   captures the operator's panel cert (uniquely identifies their build) and
   the live config blob (any C2 hosts that were swapped via `GClass3`
   command after registration).
2. Kill scheduled task: `Get-ScheduledTask | Where TaskName -eq <name> | Unregister-ScheduledTask`.
3. Delete persistence drop in `%APPDATA%` or wherever `String_3` pointed.
4. Remove `HKCU\Software\<HWID>` registry key.
5. Pivot on stolen credentials — assume browser data, wallets, and any
   active session tokens are compromised. Force password rotation across
   all browser-saved credentials and wallet seed phrases (i.e., assume
   funds-at-risk for any wallet with a hot seed on the host).
6. Capture C2 cert from memory; report C2 IPs to ISPs and threat-intel
   sharing groups (CIRCL, abuse.ch, AlienVault OTX). The cert is unique
   per panel install — a strong identifier for that operator across multiple
   victims.

---

## 8. Critique of the existing RE workspace

This workspace's existing docs (`OVERVIEW.md`, `WIRE_FORMAT.md`,
`PROTECTION_ANALYSIS.md`, `MEMORY_DUMP_RECIPE.md`) are technically strong but
narrowly framed. Strengths and gaps from a defender's perspective:

### What the existing docs do well

| Strength | Why it matters for defenders |
|----------|------------------------------|
| Complete wire-protocol decode | Lets defenders **decrypt captured panel ↔ backend traffic**. The static AES key + protobuf field maps in `WIRE_FORMAT.md` are sufficient to write a Wireshark dissector. |
| Cert-pinning mechanism documented | Tells defenders SSL inspection breaks the bot. Useful for designing mitigations. |
| Persistence mechanism documented | Shape of the `Register-ScheduledTask` PowerShell command is exactly what Sigma rules need. |
| Stub version field identified | `4.4.1` is a usable tracking string for hunt and YARA. |
| Static AES key publicly identified | This is a public fact now. Anyone with PCAP from a panel host can rebuild the operator's full activity timeline. |

### Gaps the existing docs leave open

| Gap | Impact |
|-----|--------|
| **No detection signatures.** No YARA, no Sigma, no IOC list. | Defenders can't act on the RE without redoing it themselves. |
| **No capability inventory or ATT&CK mapping.** | SOC tooling and reports need ATT&CK IDs. Nothing in `OVERVIEW.md` maps to ATT&CK. |
| **Wallet/browser target list never enumerated.** | The wallet ID list is one of the strongest fingerprints for this family — the docs reference `Class3` but never list the IDs. |
| **No IOC list for outer EXE.** | Defenders care about pre-execution detection; existing docs only describe what happens after Assembly.Load. |
| **No discussion of plugin DLL features.** | The 37 feature DLLs (Keylogger, HVNC, RemoteDesktop, RDP cloning, etc.) are mentioned but not characterized. Capability gap for defenders sizing the threat. |
| **No JA3 / TLS fingerprint discussion.** | Network detection layer ignored. |
| **No discussion of the Cloudflare Worker (`api-metrics-v2.jonesmaster227.workers.dev`).** | One of the most actionable IOCs for blocking PureCrack-cracked deployments specifically. |
| **No anti-analysis or evasion characterization.** | "What does this RAT do to evade EDR?" is a defender's first question. Answer: very little (one anti-sleep call). The docs don't say. |
| **No mention of MITRE ATT&CK.** | Industry-standard taxonomy missing entirely. |
| **No mitigations section.** | Defender response is left to the reader. |
| **No discussion of the 47 wallet extensions as a fingerprint.** | This list is rare and characteristic. Big missed opportunity. |
| **Framing is operator-only.** | The README says "stays out of the way" and "operator quickstart." Nothing in the workspace surfaces detection-relevant artifacts unless you read the `inner/` source yourself. |

### Process gaps in the RE itself

| Gap | Note |
|-----|------|
| No samples database | Existing docs reference v4.0.9596 specifically but don't enumerate hashes of in-the-wild samples. Without a sample corpus, family attribution is fragile. Recommended: cross-reference against MalwareBazaar, VirusTotal Retrohunt, and Triage submissions tagged `purerat`. |
| No campaign linkage | No discussion of which threat actors / campaigns use PureRAT. (Public reporting links it to wallet-theft, info-stealer-as-a-service, and Latin American banking-trojan operators in 2024-2025 — none of that context is in the docs.) |
| No comparison to peer families | PureRAT shares lineage with Quasar, AsyncRAT, DcRAT — same protobuf-net + reflection-loader pattern. Forks are common. The docs treat this as a unique target rather than a member of a known family tree. |
| No guidance on version drift | `MEMORY_DUMP_RECIPE.md` is good for re-extracting `inner/`, but doesn't say how to detect whether the protocol shape changed. A defender's regression test is missing. |
| Static AES key is treated as a vendor weakness | Correct framing for offensive RE, but for defenders the more actionable framing is: "this key lets you decrypt ALL captured panel-to-backend traffic across versions." That's an offensive intel asset for the *blue* team. |
| `compile_response.bin` is a Frida-captured real `/compile` blob | This is a sample artifact — could be donated to a malware sharing group like MalwareBazaar tagged `purerat`, useful for community detection development. The workspace treats it as fallback test data. |

### Specifically: the 4.4.1 version string

The stub version `"4.4.1"` is hardcoded in `Class9.smethod_4` (`QnsdsyyYrB = "4.4.1"`).
The panel binary version is `4.0.9596`. These are different versioning schemes.
The existing docs note both numbers but don't flag that the discrepancy is a
reliable internal fingerprint — `panel_version != stub_version` is unique to
PureRAT and useful for distinguishing it from related .NET RAT families that
use a single version.

### Specifically: SSLv3 / TLS 1.0 hardcoded

`Class9.smethod_4` line ~195 calls
`AuthenticateAsClient(host, null, SslProtocols.Tls, false)`. `SslProtocols.Tls`
in .NET Framework is **TLS 1.0 only** (not "default" — the enum value is
literally TLS 1.0). This is one of the strongest network signals and the
existing docs don't call it out. In 2026 a .NET app actively negotiating
TLS 1.0 is anomalous to the point of being diagnostic.

---

## 9. Recommended additions to the workspace

For the workspace to be useful to defenders as well as the original RE
audience:

1. **This file (`THREAT_INTEL.md`)** — added.
2. **A `samples/` directory** with hashes of known-bad PureRAT samples cross-referenced
   to public sources (MalwareBazaar, VirusTotal). Don't ship samples — ship
   hashes + URLs to the sharing platforms. (Out of scope for this analysis;
   recommended as a follow-up.)
3. **A `signatures/` directory** containing the YARA and Sigma rules from §4
   and §5 as standalone files, version-tagged.
4. **A `wireshark-dissector/` directory** with a Lua dissector for the
   `api*.purecoder.io` endpoints using the documented AES key. (One-day
   project; would let any analyst with PCAP from a panel host watch the
   protocol live.)
5. **Submit `compile_response.bin` and a sample stub PE to MalwareBazaar**
   tagged `purerat` so the security community can develop signatures
   independently.

---

## 10. Open questions for further investigation

- **Plugin DLL inventory.** The 37 feature DLLs in `PureHelper.dll` need
  their own ATT&CK mapping. Likely covers T1056.001 (keylog), T1113
  (screenshot — already in stub), T1125 (webcam), T1090 (proxying via
  HVNC), T1573.001 (symmetric crypto for in-channel data), T1602.001
  (data from remote system), T1059 variants for command execution, etc.
- **Field reports / campaign attribution.** Public reporting on PureRAT
  campaigns 2024-2026 should be aggregated. Anecdotally tied to crypto theft
  and Latin American operators; needs a citations review.
- **Variants and forks.** Is there a `PureRAT-lite`? A renamed fork? The
  protobuf-net + Reactor-loader + Class<N>/GClass<N> obfuscation pattern is
  a near-textbook .NET RAT shape and probably has cousins.
- **Build-pipeline fingerprint of operator.** The dotNET_Reactor settings
  used during `/compile` are baked into the outer EXE. Operators using
  default kit settings vs. custom Reactor settings should be distinguishable
  via PE structure analysis. Unique-per-operator detection, if developed,
  would help track operators across rebuilds.
- **Coordination with PureCoder / vendor takedown viability.** PureCoder
  operates `api*.purecoder.io` openly. Domain takedown via Cloudflare /
  registrar abuse channels may be viable depending on jurisdiction. The
  Cloudflare Worker `api-metrics-v2.jonesmaster227.workers.dev` is more
  likely to take down via Cloudflare's abuse program — Cloudflare has
  cooperated on similar worker-based malware infra in 2024-2025.
