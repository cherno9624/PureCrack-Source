# Protection Analysis — PureCoder & PureCrack

Two distinct protection stacks live in this ecosystem, and we've effectively
defeated both. This doc separates them and lists the structural weaknesses in
each.

| Layer | Author | Purpose |
|-------|--------|---------|
| PureCoder's stack | PureCoder (original vendor) | Stop unauthorized use of the panel + bot codebase |
| PureCrack's stack | PureCrack (third-party crackers) | Stop unauthorized use of *PureCrack's* distribution of the cracked panel — i.e., gate access on a paid PureCrack license |

These stacks layer on top of each other. PureCrack inherits everything
PureCoder did, then adds their own license enforcement on top.

---

## Part 1 — PureCoder's protection stack

### What they shipped

| Layer | Tooling | Where it sits |
|-------|---------|---------------|
| **L1: PE wrapper** | BoxedApp | Wraps `PureRAT.exe`, redirects file/registry I/O into virtual containers, hides the inner binary. |
| **L2: .NET protector** | DNGuard HVM | Inside BoxedApp's container. JIT-rewrites managed methods at runtime, refuses to run under debuggers/profilers. |
| **L3: Online activation** | Cloudflare-fronted backend at `api*.purecoder.io` | License key + HWID validated server-side; per-session AES key derived from the activation response; all subsequent traffic encrypted with that session key. |
| **L4: Wire crypto (panel ↔ backend)** | AES-256-CBC, static key | The stable key `e6c43cc05d35fee...` for legacy/v9572-v9598 endpoints. Same key across versions. |
| **L5: v9598 addition** | Offline-license validator (~335 KB in `RT_RCDATA/15`) | Lets the panel validate a previously-issued license offline without re-contacting the backend. |

### Threat model PureCoder seems to have built for

- Casual pirates trying to run the panel without a license → blocked by L3
  (no valid response, panel won't reach main UI).
- Static analysts trying to reverse the panel binary → blocked by L1+L2
  (BoxedApp hides the .NET payload, DNGuard makes the unwrapped IL unreadable).
- Network observers replaying validate/compile responses → blocked by L3+L4
  (per-session AES + nonce makes raw replay impossible).

### Where the stack actually held up

- **Static analysis of the panel** is genuinely expensive. de4dot can't touch
  DNGuard-protected v3+. ILSpy bombs on the post-DNGuard methods. Researchers
  who tried to crack the panel binary directly were stuck for weeks.
- **Network replay is non-trivial.** PureCoder's per-session keying means you
  can't dump a valid `/validate` response and resend it from a different
  machine — the session key is derived per-activation.
- **Their backend is alive long enough that the protection has economic
  value.** Cloudflare front, working DNS, real cert chain — credible enough
  that buyers can't trivially route around it.

### Where the stack failed structurally

#### 1. The static AES key on the wire endpoints

`e6c43cc05d35fee7c8533d96203eeda357c65e85e30dbe622fad26fdfbb222a8` is the same
across v9572 → v9598. Hardcoded in the panel binary. Once any one panel build
is compromised (even partially), this key is recovered forever. It's the
"breaks once, breaks forever" pattern — small extraction effort, no rotation
mitigation.

This key is what makes our relay viable. We don't need to crack DNGuard or
BoxedApp; we only need to speak the wire protocol, and the key was extracted
once and never changed.

#### 2. The validation gate doesn't protect downstream artifacts

PureCoder's L3 stops you from *starting* the panel without activation. But the
panel, once activated, is fully functional and exposes every protocol detail to
its user. If a user is willing to pay for one license, they can:

- Capture all protocol traffic from their session.
- Dump panel memory at any point.
- Capture the contents of every `/compile` response, which includes the
  produced stub (without the stub's internal protection — that's separate).

This means PureCoder's protection model assumes their users are honest. The
moment any user is dishonest *and* willing to pay one license fee, the
protection collapses. PureCrack reportedly did exactly this in 48 hours.

#### 3. `data.pak`'s AES key isn't at the static layer

PureCoder does have AES on the stub-source bundle (which is what PureCrack
ships as `data.pak`), but the key is stored in obfuscated code that runs at
licensing time, *after* the activation gate. So the key is recoverable to
anyone who can run the program — i.e., anyone with a license. The encryption
prevents passive recovery, not active recovery.

#### 4. The bot's cert pinning is the only post-build defense

Every built stub pins its panel cert with `X509Certificate2.Equals()`. That
prevents MITM but provides zero defense against reverse engineering of the
stub itself, because the cert is just data — extracting it is trivial once
you have the stub's decrypted resource. The cert pin protects the *channel*,
not the *code*.

### Summary

PureCoder built strong static-analysis resistance and weak dynamic-analysis
resistance. Their threat model assumed observers couldn't run the program
authenticated. That assumption broke as soon as one motivated buyer existed.

---

## Part 2 — PureCrack's crack methodology

### What PureCrack did, mechanically

1. Bought a PureRAT license from PureCoder.
2. Set up live observation of their licensed panel: hooked the wire layer,
   captured `/compile` responses, captured the AES key.
3. Memory-dumped the panel after activation, recovered the inner stub source
   (or built it from observation of decrypted artifacts).
4. Extracted PureRAT's 37 client feature DLLs (Keylogger, HVNC, RemoteDesktop,
   etc.) from panel resources.
5. Built `PureServer.exe` — a native C++ replacement for PureCoder's online
   build service. Reads `data.pak`, runs csc, runs dotNET_Reactor, returns
   the produced stub.
6. Built `PureHelper.dll` — a panel-side `ICustomPlugin` that auto-injects
   the 37 client feature DLLs into PureRAT's plugin registry on startup.
7. Wrote their own license daemon: a small Cloudflare Worker at
   `api-metrics-v2.jonesmaster227.workers.dev` that gates `PureServer.exe`
   on a per-customer license key + HWID combination.

### What PureCrack added on top

| Layer | Purpose |
|-------|---------|
| **C1: PureCrack license server** | Gates `PureServer.exe` on a paid PureCrack key. Cloudflare Worker. |
| **C2: HWID locking** | Each PureCrack license key is bound to up to 5 HWIDs. Reactivation requires their backend. |
| **C3: VMProtect on PureServer.exe** | Native binary obfuscation. Hides the data.pak AES key, the build pipeline, the license-state machine. |
| **C4: Per-session AES on activation** | Same model PureCoder used. Each `/activate` call to their Cloudflare Worker derives a per-session key for subsequent `/heartbeat` and `/release` traffic. |
| **C5: data.pak structure** | PPAK-magic AES container holding the inner sources, plus configuration metadata. |

### Where PureCrack's stack failed structurally

#### 1. Same fundamental flaw as PureCoder, one level up

PureCrack's protection stops you from running their `PureServer.exe` without a
PureCrack license. But the moment you *have* a PureCrack license, the
contents of `data.pak` are recoverable from PureServer's process memory the
same way PureCoder's panel was vulnerable — `procdump -ma` after a successful
activation, grep for `PK\x03\x04` in the heap, carve the ZIP. Took 10 minutes
of manual effort in this session.

This is the meta-pattern: every protection layer in this stack is gated by
"have you paid the previous layer?" Once any one license at any one layer is
compromised, the data that layer protected is recoverable forever.

#### 2. The session-key model still makes raw replay hard, but doesn't help

We never actually defeat PureCrack's session-keyed channel. We don't need to —
we replace `PureServer.exe` entirely with our own relay. PureCrack only
protects the path between their PureServer.exe and their Cloudflare Worker;
they have no way to verify that `PureRAT.exe` is talking to a real PureServer
vs a stand-in.

This is a structural blind spot: the panel binary doesn't authenticate its
local license daemon. So substituting the daemon is undetectable.

#### 3. PureHelper.dll exposes the feature DLLs in plain .NET resources

The 33 MB `PureHelper.dll` carries 37 `PureHelper.ClientDLLs.XX_FeatureName.dll`
embedded resources. These are .NET resources accessible via standard
`Assembly.GetManifestResourceStream` calls. They aren't even encrypted —
ILSpy can list them.

So even ignoring PureServer entirely, anyone with a copy of `PureHelper.dll`
has the 37 feature client DLLs. They're rendered useless without a working
panel + bot infrastructure to dispatch them, but the code itself is recoverable
trivially.

#### 4. The static panel-side AES key is inherited unchanged

PureCrack didn't rotate `e6c43cc0...`. They couldn't — the panel is unmodified
PureCoder code, and the panel's static key is what makes their cracked stack
work at all (their `PureServer.exe` has to speak the same wire protocol).
This means anyone who knows PureCoder's key automatically has half the
protocol against PureCrack's stack too.

#### 5. The HWID-binding is local and bypassable

PureCrack's license keys are bound to up to 5 HWIDs server-side. But the HWID
sent during activation is computed locally inside `PureServer.exe`. We can
trivially patch PureServer to send any HWID we want (even before VMProtect, the
HWID assembly happens via standard Win32 APIs that can be hooked). We never
needed to do this because we don't run PureServer at all, but it would be
straightforward.

### Summary

PureCrack inherits all of PureCoder's structural weaknesses (static keys,
no daemon authentication from the panel) and adds their own (data.pak
recoverable from licensed-PureServer memory). Their VMProtect layer makes
static analysis of `PureServer.exe` very hard — but, again, only relevant
against attackers without a license. Once you can run PureServer, dynamic
recovery is fast.

---

## Part 3 — The structural pattern

Both PureCoder and PureCrack faced the same core problem: **a program that
needs to use protected data has to make that data observable to whoever runs
it, eventually**. This is the unsolved "obfuscated program problem" in
applied cryptography. The two common workarounds are:

| Workaround | What it does | Where it breaks |
|------------|--------------|-----------------|
| **Online activation** | Move the secret to a server the user can't see | The server has to send the secret eventually for the program to work. The connection is observable. |
| **Hardware secure enclave** | Move the secret to a chip the user's OS can't read | Doesn't apply to consumer software, and the channel between enclave and program is still observable. |

Neither vendor here used a secure enclave (it's not a viable model for
distributable Windows software). Both used online activation. Both lost the
moment one paying user defected.

The protection layers each side stacked (BoxedApp, DNGuard, VMProtect,
dotNET_Reactor) are *static-analysis* defenses. They don't solve the obfuscated
program problem — they just make the *static* approach to it expensive. The
*dynamic* approach (run the program, observe what it does with the data) was
left wide open by both vendors:

- Anti-debug routines exist but bypass via procdump (which doesn't attach a
  debugger; it asks the kernel for the snapshot directly) defeats them.
- Anti-VM routines didn't exist for either side, and would have caused
  legitimate-customer false-positive issues anyway.
- No checks for memory dumpers (procdump fingerprint detection, etc.) on
  either side.

### Why this matters for understanding the workspace

Our workspace works because:

1. We have a valid license (recovered into our test environment from past
   captures or a real activation). PureCoder's L3 doesn't stop us — we don't
   even contact their backend; the relay impersonates it locally.
2. We don't try to defeat BoxedApp/DNGuard on the panel — we don't need to.
   The panel's behavior is fully visible at the network layer once it starts
   talking, and that's all we need.
3. We don't try to defeat VMProtect on `PureServer.exe` — we don't run
   `PureServer.exe` at all. We replaced it with our own Python pipeline using
   sources extracted via `procdump` of an authenticated `PureServer.exe` run.
4. The static AES key on the panel↔backend channel is the linchpin. Without it
   our relay couldn't decrypt `/compile` requests. With it, everything else
   falls into place.

If PureCoder ever rotated the static AES key — and only that, nothing else —
our entire workspace would stop working overnight. They probably haven't because
rotating it would break compatibility with all installed panel versions in the
field, and they have no signing infrastructure to push a new key with a panel
update.
