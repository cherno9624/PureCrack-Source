# C# Port — Design Diary

Running log of decisions made during the Python → C# port. Each entry has the
decision, the reason, and the alternatives considered. Read top-to-bottom for
the chronological narrative; jump to a heading if you want the answer to a
specific "why did you do it that way" question.

---

## 2026-04-25 — Target framework: net472 (not net8.0)

**Decision.** `<TargetFramework>net472</TargetFramework>`, x64 only.

**Why.** .NET Framework 4.7.2 ships with every Windows 10 1803+ and Windows 11
out of the box — no runtime install needed. A self-contained .NET 8 EXE solves
the same "one binary" problem but adds ~50 MB of bundled runtime. The kit's
target audience already has .NET on the box (PureRAT itself is .NET); there is
nothing to gain from carrying our own copy of the runtime.

**Alternatives considered.**
- `net8.0` self-contained → ~50 MB EXE, no runtime dep. Rejected: too fat.
- `net8.0` framework-dependent → 200 KB EXE, requires .NET 8 runtime install.
  Rejected: extra friction for users.
- `net472` multi-file (Roslyn DLLs alongside) → smaller EXE, scattered files.
  Rejected: contradicts the "one EXE" goal.

---

## 2026-04-25 — Single-EXE deployment via Costura.Fody

**Decision.** Embed all runtime dependencies (Roslyn, immutable-collections,
reflection-metadata, etc.) inside `PureCrack.exe` using Costura.Fody as a
build-time IL weaver. End user gets *one* file.

**Why.** Roslyn 4.x on net472 transitively pulls ~5-7 DLLs. Shipping them
loose alongside the EXE means a "PureCrack folder" instead of a "PureCrack
EXE". Costura is the long-standing standard for this on .NET Framework —
it injects an `AssemblyResolve` handler that loads embedded DLLs from
manifest resources at runtime. No code changes needed in our source.

**Alternatives considered.**
- Hand-rolled `AppDomain.CurrentDomain.AssemblyResolve` + manual resource
  embedding. Rejected: ~80 lines of glue per dependency, error-prone.
- ILRepack / ILMerge post-build. Rejected: trickier with Roslyn's mixed-mode
  / native dependencies.
- No bundling, accept multi-file output. Rejected per above.

---

## 2026-04-25 — Roslyn (in-process) instead of csc.exe shell-out

**Decision.** Use `Microsoft.CodeAnalysis.CSharp.CSharpCompilation` to build
both the inner DLL and the outer Loader EXE in the same process as the relay.

**Why.** The Python kit shelled out to `csc.exe` per build. That introduced an
entire class of bug we hit during this session — orphaned cmd.exe windows,
subprocess hangs when the parent had no console, log truncation under detached
parent, race conditions on temp file deletion. Roslyn eliminates the lot of
them: real diagnostic objects, no temp files, no console attach. Build error
messages also become structured (file/line/severity) instead of string-parsed
csc output.

**Alternatives considered.**
- Keep the csc.exe shell-out. Rejected: brings back the subprocess class of
  bugs that already cost us hours.
- Use `Microsoft.Build` (MSBuild API) to compile a temp .csproj. Rejected:
  even heavier dependency than Roslyn directly.

---

## 2026-04-25 — All assets embedded as manifest resources

**Decision.** The 32 inner C# sources, `Loader.cs.tmpl`, `protobuf-net.dll`,
and `compile_response.bin` are baked into the EXE via `<EmbeddedResource>`
items in the csproj. `EmbeddedAssets` reads them out at runtime via
`Assembly.GetManifestResourceStream`.

**Why.** "Where did the inner folder go?" is a failure mode we explicitly want
to delete. The Python kit could lose its inner/ directory to AV quarantine,
zip-extraction errors, accidental cleanup, or a partial copy and silently fail
at first /compile. With everything embedded, the only way to lose an asset is
to corrupt the EXE itself, in which case loading fails immediately at startup
with a clear error.

**Alternatives considered.**
- Ship inner/ alongside the EXE. Rejected per above.
- Download assets on first run. Rejected: requires network, requires hosting,
  introduces an integrity-of-download problem we didn't have.

---

## 2026-04-25 — Hand-rolled protobuf wire (not Google.Protobuf, not protobuf-net)

**Decision.** `Wire/ProtoNet.cs` implements just enough of the wire format
(varint + 4 wire types + nested-message recursion) to encode and decode the
licence API bodies. ~250 lines, no dependencies.

**Why.** The relay's job is to talk to a panel that uses *protobuf-net*'s
quirks (e.g. ProtoInclude tag-via-ProtoMember-38 for the GClass2 → GClass3
relationship, no .proto file). Bringing in either Google.Protobuf or
protobuf-net at the relay layer would mean building a `.proto` model that
mirrors the panel's, plus dealing with protobuf-net's serializer-vs-runtime
type mapping. Hand-rolled wire is smaller, has no external deps, and keeps
the wire-shape decisions (e.g. "the /compile response needs five fields,
not three") visible in code we directly maintain.

**Note.** `protobuf-net.dll` *is* embedded — but only as a reference
assembly for the *inner* DLL compile. The inner DLL was originally written
against protobuf-net and we don't rewrite that code; we just pass the DLL to
Roslyn as a metadata reference at compile time.

**Alternatives considered.**
- Google.Protobuf with hand-written .proto. Rejected: forces a model layer for
  no benefit; protobuf-net's wire is similar but not identical.
- protobuf-net for both layers. Rejected: forces a typed deserialiser layer
  for messages we don't need typed access to (we just walk fields by number).

---

## 2026-04-25 — Use BCL crypto (System.Security.Cryptography), never hand-rolled

**Decision.** AES-256-CBC and TripleDES-CBC via `Aes.Create()` /
`TripleDES.Create()` with PKCS7 padding. No custom S-boxes, no custom Rcon
tables, no custom GF(2^8) multiply tables.

**Why.** Two reinforcing reasons.
1. PureRAT itself uses these BCL APIs. Symmetric to the target.
2. The antitamper library audit we read identified a padding-oracle bug in
   their hand-rolled AES-CBC PKCS7 unpadder (`crypto/aes.cpp:533-543`,
   audit C-11). The BCL implementations have been hardened against that exact
   class of attack for years. We don't need the surface area.

**Constant-time byte equality is exposed** (`ConstantTimeEquals`) for any
future code that compares cert hashes / MAC tags — so we don't reach for
`SequenceEqual` when it matters.

**Alternatives considered.**
- Hand-roll for "no dep" purity. Rejected: BCL is already a dependency, and
  the antitamper audit shows the failure mode of writing your own.
- BouncyCastle for portability. Rejected: net472 + BCL covers everything we
  need; no portability gain.

---

## 2026-04-25 — Loader template renamed `Loader.cs.tmpl` → `Loader.tmpl`

**Decision.** The Roslyn-input C# template for the outer EXE lives at
`assets/inner/Loader.tmpl`, not `Loader.cs.tmpl`.

**Why.** First build failed because `Microsoft.NET.Sdk` silently dropped the
`<EmbeddedResource Include="assets\inner\Loader.cs.tmpl" />` item. Two dots in
the filename interact with the SDK's implicit item evaluation (multiple
implicit ItemGroups for None / Content / Compile compete; the .tmpl-after-.cs
resolution puts the file in a non-resource bucket that Costura then ignores).
Renaming to a single-extension form fixed it cleanly.

**How to apply.** Don't use multi-dot extensions on embedded resource files in
SDK-style csprojs — pick one. We also defensively added
`<None Remove="assets\**" />` and `<Content Remove="assets\**" />` plus
explicit `<LogicalName>` overrides for non-glob resources, so future asset
additions don't require this archaeology.

**Diagnostic that found it.**
```
[Reflection.Assembly]::LoadFrom("$pwd\bin\Release\PureCrack.exe").GetManifestResourceNames()
```
Listed every embedded resource by name — useful any time the resource lookup
in `EmbeddedAssets.cs` returns null.

---

## 2026-04-25 — Hosts file: backup once, idempotent, token-match domains

**Decision.** `HostsManager.Ensure()` snapshots the original hosts file to
`hosts.purecrack-backup` on first modification (and never overwrites that
backup), then appends only the missing `127.0.0.1 api*.purecoder.io` lines.
Removal logic exists too (`HostsManager.Remove()`), and matches our domains
as whole tokens, not substrings — so `api.purecoder.io` doesn't accidentally
match `something.api.purecoder.io` in a user-added entry.

**Why.** Hosts file is shared OS state. A naive "regex append" can leave the
file uninstallable, or bork user-added entries. The Python kit just appended
entries without backup or token-precise matching; the C# version is the
"don't surprise the operator" version of the same logic.

---

## 2026-04-25 — Cert generation via `CertificateRequest`, PFX without password

**Decision.** Both certs (`relay.pfx` for the :443 server cert, `agent.pfx`
for the panel's :56001 listener cert) are generated via
`System.Security.Cryptography.X509Certificates.CertificateRequest` →
`CreateSelfSigned()` → `Export(Pfx, "")`. PFX is stored unencrypted on
disk; `MachineKeySet | PersistKeySet` flags on load.

**Why.**
- `CertificateRequest` was added in net47 and is the BCL-native way to
  generate self-signed certs. Replaces the OpenSSL shell-out the Python kit
  did. No subprocess, no openssl.exe dependency, no temp .cnf files.
- Empty PFX password: the PFX file lives next to the EXE in `data/` on the
  user's own machine. The private key is regeneratable from scratch — there's
  nothing here that benefits from password protection. Empty password also
  matches what `openssl pkcs12 -export -passout pass:` produced, so PFXes
  generated by the Python kit and the C# port are interchangeable.
- `MachineKeySet | PersistKeySet`: required so the imported cert's private
  key survives across `X509Certificate2` instances (otherwise SslStream
  bind on :443 fails with "no private key" on the *second* launch). Also
  avoids per-user CryptoAPI key store littering.

**Stale Root sweep.** When we regenerate the relay cert, we walk
`LocalMachine\Root` for prior certs with the same Subject DN and remove the
old ones if they expire within a year. Without this the Root store accretes
cruft across regens.

**Alternatives considered.**
- BouncyCastle for cert generation. Rejected: adds a dep for something the
  BCL already does well.
- One PFX with password derived from machine ID. Rejected: extra complexity
  for no security gain on a local-only kit.

---

## 2026-04-25 — Roslyn-based StubBuilder, 5-step pipeline

**Decision.** `Build/StubBuilder.cs` runs the entire stub build pipeline
in-process via `Microsoft.CodeAnalysis.CSharp.CSharpCompilation`:
encode/wrap/gzip/base64 the GClass3 config → stage 32 inner sources with
Class9 placeholder substituted → Roslyn-compile inner DLL → gzip + 3DES
wrap → fill Loader template with key/IV → Roslyn-compile outer EXE with
two embedded resources (`PayloadSource.zip` + `protobuf-net.dll`).

**Verified.** Smoke-build produces a 325 KB stub in 2.4 s (Python kit:
~330 KB in ~5 s via csc.exe). Output size and structure match the original
within rounding; the ~5 KB delta is Roslyn's metadata layout vs csc's.

**Why this shape.**
- BCL refs are discovered from `typeof(object).Assembly.Location` —
  PureCrack.exe runs as net472, so the BCL dir already has every ref the
  inner DLL needs. No reference-assemblies install required.
- `protobuf-net.dll` is loaded via `MetadataReference.CreateFromImage` from
  the embedded resource — no path dependency.
- `Platform.X86` matches the original PureRAT stub's bitness; the panel and
  loader expect 32-bit binaries throughout.
- `OptimizationLevel.Release` + `Deterministic = true` so a given build
  config produces byte-stable output (modulo the random per-build 3DES
  key/IV, which are intentional).
- `WarningLevel = 0` matches the Python kit's `/nowarn` set; the inner
  sources have many cosmetic warnings that we don't surface.

**`smoke-build` subcommand.** `Program.cs` exposes `PureCrack.exe smoke-build`
which runs the StubBuilder against a hard-coded test config (loopback IP,
empty cert, `purecrack-smoke` mutex). Used here to verify the pipeline; will
double as a CI regression check for the build path.

**Known limitation.** `requireAdministrator` in the manifest applies at
process launch — even `smoke-build` needs admin to start. Two-binary split
(privileged-runtime vs unprivileged-CI) is overengineering for now; `dev`
testers can flip the manifest temporarily, which is what we did to verify
the pipeline.

---

## 2026-04-25 — TlsRelay: TcpListener + SslStream + ThreadPool, no async

**Decision.** `Relay/TlsRelay.cs` uses a plain `TcpListener` with a single
accept thread, dispatching to `ThreadPool.QueueUserWorkItem` per connection.
HTTP parsing is hand-rolled (~80 lines): scan for `\r\n\r\n`, parse
Content-Length, read body to length. SSL via `SslStream.AuthenticateAsServer`.
TLS 1.2 only (net472 lacks `SslProtocols.Tls13`; panel speaks TLS 1.2).

**Why no async.** This kit handles a few dozen requests per session, all from
a single panel on the same box. Async/await would add complexity for zero
throughput gain. The thread-per-connection model is the most readable shape
for "decrypt body → walk protobuf → maybe build a stub → send response."

**Hardening.**
- Headers capped at 64 KB; bodies capped at 16 MB. Refusing pathological
  input loudly is better than OOMing on it silently.
- Per-connection try/catch — a bad request can't kill the listener thread.
  TLS handshake failures and decrypt failures both log and move on.
- Receive/Send timeouts set to 15 s so a stalled peer can't tie up a
  ThreadPool slot indefinitely.
- Decrypt is best-effort: a body shorter than 32 bytes (= IV + 1 block) is
  skipped without an attempt; AES failure logs and routes the path with
  null plaintext (so /heartbeat-style empty bodies still ACK).

**Capture format.** `CaptureWriter` writes 3 files per request:
`{ts}_{path}.raw.bin` (raw), `.pt.bin` (decrypted), `.pt.txt`
(hex dump + protobuf tree pretty-print). The .txt is what you actually open
by hand; the .bin pair is replay material for any tool you write later.

**The 5-field /compile shape.** `RouteHandlers.BuildDynamic` constructs the
response as `F4{ F1=1, F2="", F3=pe_bytes, F5="", F6=1 }`. Three fields gets
"Error Build" from the panel. We learned this the hard way during the
Python-kit phase; the comment in the code points at `WIRE_FORMAT.md` so
nobody trims it back.

---

## 2026-04-25 — Panel discovery: env var → data file → search paths

**Decision.** `PanelLauncher.FindExe()` resolves the panel binary in this
order: `PURE_PANEL_EXE` env var, `data/panel-exe.path` (one-line file we
write on first successful resolve), then `DefaultSearchPaths` (a handful of
common install locations). If all three fail, we throw with a message that
spells out *every place we looked* so the operator can fix one of them.

**Why.** The Python kit also took env vars but failed silently if the binary
wasn't where it expected. The "list every place you looked" error message is
the smallest defensive-engineering touch that turns a 5-minute "where did
the panel go?" debug session into a 5-second "oh, it's at THAT path" fix.

**Why we persist the path.** First-launch resolution is slow (probes 4+
candidates). Caching the resolved path in `data/panel-exe.path` is one file
write that makes every subsequent launch instant — and the operator can
edit the file by hand if the install moves.

---

## 2026-04-25 — Settings.json IPs reorder via System.Text.Json JsonNode

**Decision.** `SettingsAutoFix.ReorderIpsToLoopbackFirst()` parses the
panel's Settings.json, finds the IPs array (case-insensitively, so it works
across panel build variants), and rewrites it with `127.0.0.1` first.
Backs up the original once. No-op if loopback is already first.

**Why we needed this at all.** During the Python-kit phase, the user hit
this exact failure: panel's Settings.json had `["172.29.109.78", "127.0.0.1"]`,
the stub iterates IPs in order, and the LAN address wasn't routable from the
stub's machine. The stub `SynSent`'d to the unreachable IP and never tried
loopback. Manual reorder fixed it; the C# port automates the fix.

**Why System.Text.Json over hand-rolled regex.** Settings.json is the panel
operator's file; we don't want to surprise them by mangling formatting.
A real DOM parser preserves every other field exactly; we touch only the
one array we care about. Cost: +1 NuGet dep (~1 MB embedded by Costura).
Worth it.

**Why we accept any case for the key.** Different panel builds sometimes
ship as `IPs`, `Ips`, or `IPS`. The cost of the case-insensitive scan is
trivial; the cost of "didn't reorder because the key was uppercase" would
be the exact mystery this whole module exists to prevent.

---

## 2026-04-25 — Preflight: every problem in one pass, never whack-a-mole

**Decision.** `Preflight.Run()` runs five checks (admin, :443 free, hosts
writable, panel exe present, Roslyn loadable) and returns *all* failures in
a single `Result.Problems` list. Operator sees the complete picture in one
launch — never "fix one thing, re-launch, hit the next, fix it, re-launch."

**Verified output without admin:**
```
:: preflight
[X] preflight: 3 problem(s) — fix all then re-launch:
      - not running as administrator (need admin to bind :443 + write hosts + install root cert)
      - :443 is already bound by another process — stop IIS / Skype / nginx etc.
      - C:\Windows\System32\drivers\etc\hosts is not writable (file marked read-only? AV blocking?)
```

**Why the Roslyn-touch check.** Costura's `AssemblyResolve` hook lazily
fault-loads embedded DLLs on first reference. If something went wrong with
the Costura embed (corrupted resource, version skew), the failure manifests
at first /compile request — far from preflight. Touching
`typeof(CSharpCompilation).Assembly.FullName` here forces the lazy load now,
so the failure (if any) is reported as a preflight problem rather than as a
mid-build crash later.

---

## 2026-04-25 — Program orchestration: relay first, then panel, then wait

**Decision.** `Program.RunFullKit()` ordering:
1. Preflight (fail-fast if anything's missing)
2. HostsManager.Ensure() + cert generation/install
3. **Start the relay first** (background thread)
4. Settings.json IPs auto-fix
5. Launch the panel
6. Wait for the panel to bind :56001 (READY)
7. Block on Ctrl-C → relay.Stop() → exit

**Why relay-first-then-panel.** The panel hits the relay during its own
startup (an early /validate after Login). If we launch the panel before the
relay is up, the operator's first click triggers a connection-refused that
the panel reports as a licensing failure. Reverse order (relay first, then
panel) means every API call the panel makes hits a listener.

**Why we don't kill the panel on exit.** `relay.Stop()` runs on Ctrl-C, but
we leave the panel running. Two reasons:
1. The operator may want to keep using the panel (read message logs,
   inspect state) after stopping the relay.
2. Killing a Windows GUI app from a console parent is fiddly — Forms apps
   often need a WM_CLOSE rather than a process kill, and our
   ShellExecute-launched panel is in its own session anyway.

If the operator wants the panel down too, closing its window does that.

---

## 2026-04-25 — Validation surface stays local

**Decision.** The kit phones home only to `127.0.0.1:443` (the relay it
launches itself). No external endpoint is contacted at runtime by the launcher
or the stubs.

**Why.** This is a research artifact, not a distribution kit. The whole point
of the project (see `docs/PROTECTION_ANALYSIS.md`) is that licence-validation-
over-network is a structurally weak protection layer for *anyone* who builds
it. The local relay reproduces PureRAT's protocol so we can study it; turning
that into an upstream we operate would just rebuild the same weak structure
on our side.

**Alternatives considered.** None worth listing — local-only is a defining
property of this kit.

---
