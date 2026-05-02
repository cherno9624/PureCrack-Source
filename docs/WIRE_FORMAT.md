# Wire Format Reference

Field-level reference for the four PureRAT licensing endpoints. All bodies are
`[16-byte IV][AES-256-CBC ciphertext]` with key
`e6c43cc05d35fee7c8533d96203eeda357c65e85e30dbe622fad26fdfbb222a8`. Inner
payloads are protobuf-net messages.

---

## /api/licence/validate (request)

Outer wrap: `field[1] = body` (varint tag `0x0A`).

| Field | Type | Meaning |
|-------|------|---------|
| 1 | string | License key (e.g. `AAAA-BBBB-CCCC-DDDD-EEEE-FFFF-0000`) |
| 2 | string | Product = `"PureRAT"` |
| 3 | string | Panel version, e.g. `"4.0.9596.35655"` |
| 4 | string | HWID composite: `"<volSerial>|<user>|0000000000000000|<cpu>|<sid>"` |
| 6 | string | 32-char hex HMAC / session id |

## /api/licence/validate (response)

Outer wrap: `field[2]`. Inner:

| Field | Type | Meaning |
|-------|------|---------|
| 1 | varint | Status = `1` for OK |
| 2 | string | Reserved / empty |
| 3 | string | Reserved / empty |
| 5 | string | Reserved / empty |
| 7 | string | Greeting line, displayed in panel UI |
| 9 | string | Welcome line |
| 10 | submsg | Cert wrapper (see below) |
| 11 | string | HWID stats, e.g. `"HWID Changes: 1 of 9999 used"` |
| 12 | string | Reserved / empty |
| 13 | string | Expiry message, e.g. `"Expires in 9999 days"` |

Field 10 sub-message (cert wrapper):

| Field | Type | Meaning |
|-------|------|---------|
| 7 | submsg | nested |

The nested sub-message:

| Field | Type | Meaning |
|-------|------|---------|
| 2 | string | base64 PFX (the agent cert) |

## /api/licence/compile (request)

Outer wrap: `field[3] = body`.

| Field | Type | Meaning |
|-------|------|---------|
| 1 | string | License key |
| 2 | string | `"PureRAT"` |
| 3 | string | Panel version |
| 4 | string | HWID composite |
| 5 | submsg | BuildSettings (see below) |
| 6 | string | Session HMAC |

BuildSettings (request `field[5]`) wraps everything in another sub-field:

| Field | Type | Meaning |
|-------|------|---------|
| 9 | submsg | The actual build config |

Build config (request `field[5].field[9]`):

| Field | Type | Meaning |
|-------|------|---------|
| 1 | repeated string | C2 IPs |
| 2 | repeated int32 | C2 ports |
| 3 | string | base64 server cert (DER X509) |
| 4 | string | Group label |
| 5 | string | usually empty |
| 6 | string | output filename (panel uses for SaveFileDialog default) |
| 10 | string | base64 panel PFX (with private key) — full PKCS#12 |
| 11 | string | persistence filename, or empty |
| 12 | string | persistence folder env var, e.g. `"APPDATA"`, or empty |
| 14 | string | mutex name |

Note: this is the BUILD REQUEST schema. It's not the same as the stub-side
GClass3 schema (which has different field types — bools instead of strings for
some slots). PureServer translates between the two when constructing the stub.

## /api/licence/compile (response)

Outer wrap: `field[4] = body`. Inner:

| Field | Type | Meaning |
|-------|------|---------|
| 1 | varint | Status = `1` for OK |
| 2 | string | Reserved / empty |
| 3 | bytes | Stub PE bytes |
| 5 | string | Reserved (older versions) |
| 6 | varint | Reserved (older versions) |

Our relay sends the minimal triple `(f1=1, f2="", f3=<pe>)`. Older PureServer
captures included f5 and f6 with no observed effect on panel behavior.

## /api/licence/heartbeat (request, response)

Request: same outer-wrap pattern as validate. Body contains license key + HWID
+ session HMAC.

Response: `outer[2]{ f1=1 }`. Minimal ACK.

## /api/licence/update-plugins (request, response)

Same shape as heartbeat. Used for legacy plugin update notifications which the
panel ignores in current versions.

---

## Stub-side GClass3 schema (different from build request)

This is the protobuf the inner stub deserializes from its embedded config blob
at startup. Wrapped in `GClass2` via `[ProtoInclude(38, typeof(GClass3))]`, so
the wire bytes start with tag `0xB2 0x02` + varint length + body.

| Field | Type | C# field | Meaning |
|-------|------|----------|---------|
| 1 | repeated string | `List_0` | C2 IPs |
| 2 | repeated int32 | `List_1` | C2 ports |
| 3 | string | `String_0` | base64 X509 cert (the cert the stub pins) |
| 4 | string | `String_1` | Group |
| 5 | bool | `Boolean_0` | auto-start persistence thread |
| 6 | bool | `Boolean_1` | enable anti-debug |
| 7 | string | `String_2` | persistence filename, or "" |
| 8 | string | `String_3` | persistence folder env var, or "" |
| 9 | string | `stadrmoOn1` | mutex name |
| 10 | bool | `Boolean_2` | feature flag |

The blob is then GZip-compressed, base64-encoded, and substituted for the
placeholder `"H4sIAAAAAAAACgMAAAAAAAAAAAA="` in `inner/Class9.cs` line 38
(the `Class9.smethod_3` deserialize call).

---

## Bot registration packet (stub → panel)

After TLS handshake the stub sends a length-prefixed gzipped protobuf:

```
[4-byte LE length][gzip stream]
```

Decompressed:

```
GClass2 wrapper { ProtoInclude(1) = GClass4 ClientInformation }
GClass4 {
  f1: HWID (32-char hex)
  f4: Antivirus name (e.g. "Windows Defender")
  f5: OS string (e.g. "Windows 11 64Bit")
  f6: Stub version (e.g. "4.4.1")
  f7: Username
  f8: Computer name as "User[MACHINE]"
  f9: Port the stub connected on
  f10: IP the stub connected to
  f12: Privilege level varint (4 = admin)
  f14: Group (echoed from GClass3.String_1)
  f15: Stub file path
  f16: Uptime string ("0d 0h 5m 12s")
  f17: Screenshot bytes (JPEG ~30-40 KB)
  f18: Foreground window title
}
```

Field 14 round-tripping the Group string is the test-suite's success criterion
in `tests.py robust`.
