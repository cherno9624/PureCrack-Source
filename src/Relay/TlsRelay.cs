using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using PureCrack.Crypto;
using PureCrack.Util;

namespace PureCrack.Relay;

/// <summary>
/// HTTP-over-TLS relay that impersonates PureRAT's licence API. Listens on
/// <c>127.0.0.1:443</c> by default with a self-signed cert covering
/// <c>api*.purecoder.io</c>; the hosts file (managed by <see cref="Setup.HostsManager"/>)
/// sends those names to <c>127.0.0.1</c>. The relay is loopback-only on purpose:
/// the validate response embeds a working PFX (private key included) and the
/// /compile endpoint runs Roslyn — neither should be reachable from the network.
/// Set <c>PURECRACK_BIND_ALL=1</c> only if you knowingly want to expose it.
///
/// Single accept thread, ThreadPool work items per connection. Handlers are
/// in <see cref="RouteHandlers"/>; capture writing in <see cref="CaptureWriter"/>.
/// </summary>
public sealed class TlsRelay : IDisposable
{
    private readonly X509Certificate2 _serverCert;
    private readonly RouteHandlers _routes;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly IPAddress _boundAddress;
    private Thread? _acceptThread;
    private int _requestCount;

    /// <summary>Default port — :443, what the panel hits via the hosts redirect.</summary>
    public const int DefaultPort = 443;

    /// <summary>Reject HTTP body claims larger than this (16 MB) to prevent OOM on bad input.</summary>
    private const int MaxBodyBytes = 16 * 1024 * 1024;

    public bool DynamicBuildEnabled { get; set; } = true;

    public TlsRelay(X509Certificate2 serverCert, RouteHandlers routes,
                    IPAddress? bindAddress = null, int port = DefaultPort)
    {
        _serverCert = serverCert ?? throw new ArgumentNullException(nameof(serverCert));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _boundAddress = bindAddress ?? ResolveDefaultBind();
        _listener = new TcpListener(_boundAddress, port);
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Loopback by default. Opt-in public bind via <c>PURECRACK_BIND_ALL=1</c>
    /// for users with a legitimate reason (e.g. running the relay on a separate
    /// host than the panel inside an isolated VM). Logs loudly when the override
    /// is used so you don't accidentally expose the kit.
    /// </summary>
    private static IPAddress ResolveDefaultBind()
    {
        var bindAll = Environment.GetEnvironmentVariable("PURECRACK_BIND_ALL");
        if (string.Equals(bindAll, "1", StringComparison.Ordinal))
        {
            Log.Warn("PURECRACK_BIND_ALL=1 set — relay will listen on 0.0.0.0. " +
                     "validate response leaks a working PFX, /compile is a Roslyn-driven " +
                     "CPU sink. Make sure your firewall actually blocks :443 inbound.");
            return IPAddress.Any;
        }
        return IPAddress.Loopback;
    }

    /// <summary>
    /// Bind the listener and start accepting connections on a background thread.
    /// Idempotent — calling twice is a no-op after the first call.
    /// </summary>
    public void Start()
    {
        if (_acceptThread != null) return;
        _listener.Start();
        var visibility = _boundAddress.Equals(IPAddress.Loopback) ? "loopback-only"
                       : _boundAddress.Equals(IPAddress.Any)      ? "PUBLIC"
                       : _boundAddress.ToString();
        Log.Ok($"relay LISTEN on {_listener.LocalEndpoint} ({visibility})");
        _acceptThread = new Thread(AcceptLoop)
        {
            IsBackground = true,
            Name = "TlsRelay-accept",
        };
        _acceptThread.Start();
    }

    public void Stop()
    {
        if (_cts.IsCancellationRequested) return;
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* already stopped */ }
        _acceptThread?.Join(TimeSpan.FromSeconds(2));
        Log.Info("relay stopped");
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
        _serverCert.Dispose();
    }

    // ------------------------------------------------------------------------
    // Accept loop
    // ------------------------------------------------------------------------

    private void AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = _listener.AcceptTcpClient();
            }
            catch (SocketException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Err($"accept: {ex.Message}");
                continue;
            }

            // Hand off to ThreadPool. Each connection is short-lived (one
            // request, then close — the panel doesn't pipeline). The lambda
            // wraps HandleConnection in a top-level catch so any escape (e.g.
            // ObjectDisposedException from a peer that RST'd before the work
            // item ran) terminates the work item, not the process.
            ThreadPool.QueueUserWorkItem(state =>
            {
                var c = (TcpClient)state!;
                try { HandleConnection(c); }
                catch (Exception ex) { Log.Err($"workitem: {ex.GetType().Name}: {ex.Message}"); }
            }, client);
        }
    }

    // ------------------------------------------------------------------------
    // Per-connection handler
    // ------------------------------------------------------------------------

    private void HandleConnection(TcpClient client)
    {
        var n = Interlocked.Increment(ref _requestCount);

        // CRITICAL: read the remote endpoint INSIDE try/finally. Socket.RemoteEndPoint
        // can throw ObjectDisposedException / SocketException when the peer RST'd
        // before this work item ran — common with port scanners. The original code
        // had this lookup outside the try, so the throw escaped the lambda and
        // killed the process via AppDomain.UnhandledException.
        IPEndPoint? remoteEp = null;
        var addr = "?";

        try
        {
            try
            {
                remoteEp = client.Client?.RemoteEndPoint as IPEndPoint;
                addr = remoteEp?.ToString() ?? "?";
            }
            catch (Exception ex)
            {
                addr = $"<addr lookup failed: {ex.GetType().Name}>";
            }
            Log.Section($"#{n} from {addr}");

            client.ReceiveTimeout = 15000;
            client.SendTimeout = 15000;
            using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);
            try
            {
                // net472 doesn't have SslProtocols.Tls13 (added in .NET 4.8).
                // The panel speaks TLS 1.2; that's all we need.
                ssl.AuthenticateAsServer(
                    _serverCert,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12,
                    checkCertificateRevocation: false);
            }
            catch (Exception ex)
            {
                Log.Warn($"TLS handshake failed: {ex.Message}");
                return;
            }

            var (path, body) = ReadHttpRequest(ssl);
            Log.Bullet($"path: {path}");
            Log.Bullet($"body: {body.Length}b");

            var pt = TryDecrypt(body);
            if (pt != null) Log.Bullet($"decrypt OK: {pt.Length}b");

            // CaptureWriter filters internally — only /api/licence/* gets
            // persisted, scanner noise is dropped.
            var prefix = CaptureWriter.Dump(path, body, pt);
            if (prefix != null) Log.Bullet($"dumped: {Path.GetFileName(prefix)}");

            var (responsePb, label) = Route(path, pt, remoteEp);
            Log.Bullet($"resp: {label} ({responsePb.Length}b)");

            SendResponse(ssl, responsePb);
            Log.Ok($"sent {label}");
        }
        catch (Exception ex)
        {
            Log.Err($"handler: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try { client.Close(); } catch { /* best effort */ }
        }
    }

    // ------------------------------------------------------------------------
    // Request parsing
    // ------------------------------------------------------------------------

    private static (string path, byte[] body) ReadHttpRequest(Stream s)
    {
        // Read until "\r\n\r\n", then parse Content-Length, then read that many
        // body bytes. Cap headers at 64 KB and body at MaxBodyBytes to refuse
        // pathological input.
        using var ms = new MemoryStream();
        var buf = new byte[4096];
        var headerEnd = -1;
        while (headerEnd < 0)
        {
            var got = s.Read(buf, 0, buf.Length);
            if (got <= 0) break;
            ms.Write(buf, 0, got);
            if (ms.Length > 65536) throw new InvalidOperationException("HTTP headers exceed 64 KB");
            headerEnd = FindHeaderEnd(ms.GetBuffer(), (int)ms.Length);
        }
        if (headerEnd < 0) throw new InvalidOperationException("HTTP request truncated before \\r\\n\\r\\n");

        var headerBytes = new byte[headerEnd];
        Buffer.BlockCopy(ms.GetBuffer(), 0, headerBytes, 0, headerEnd);
        var headerStr = Encoding.UTF8.GetString(headerBytes);

        var contentLength = 0;
        var path = "/";
        foreach (var line in headerStr.Split(new[] { "\r\n" }, StringSplitOptions.None))
        {
            if (line.StartsWith("POST ", StringComparison.Ordinal) ||
                line.StartsWith("GET ",  StringComparison.Ordinal))
            {
                var sp = line.Split(' ');
                if (sp.Length >= 2) path = sp[1];
            }
            else if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var v = line.Substring("Content-Length:".Length).Trim();
                int.TryParse(v, out contentLength);
            }
        }
        if (contentLength < 0 || contentLength > MaxBodyBytes)
            throw new InvalidOperationException($"refusing body of size {contentLength}");

        var bodyStart = headerEnd + 4; // skip the trailing \r\n\r\n
        var alreadyHave = (int)ms.Length - bodyStart;
        var body = new byte[contentLength];
        if (alreadyHave > 0)
        {
            var take = Math.Min(alreadyHave, contentLength);
            Buffer.BlockCopy(ms.GetBuffer(), bodyStart, body, 0, take);
            alreadyHave = take;
        }
        var bodyOff = Math.Max(0, alreadyHave);
        while (bodyOff < contentLength)
        {
            var got = s.Read(body, bodyOff, contentLength - bodyOff);
            if (got <= 0) break;
            bodyOff += got;
        }
        if (bodyOff < contentLength)
            Log.Warn($"body truncated: got {bodyOff} of {contentLength}");
        return (path, body);
    }

    private static int FindHeaderEnd(byte[] buf, int len)
    {
        // Linear scan for "\r\n\r\n". Not worth Boyer-Moore for a 4-byte needle.
        for (var i = 0; i <= len - 4; i++)
        {
            if (buf[i] == 0x0D && buf[i + 1] == 0x0A &&
                buf[i + 2] == 0x0D && buf[i + 3] == 0x0A)
                return i;
        }
        return -1;
    }

    // ------------------------------------------------------------------------
    // Decrypt + route + send
    // ------------------------------------------------------------------------

    private static byte[]? TryDecrypt(byte[] body)
    {
        // Body shape: [16-byte IV][AES-256-CBC ciphertext]. Anything shorter
        // than 32 bytes is too small to be a real licence-API body.
        if (body.Length < 32) return null;
        try { return Symmetric.AesDecryptFraming(body); }
        catch (Exception ex)
        {
            Log.Warn($"decrypt err: {ex.Message}");
            return null;
        }
    }

    private (byte[] body, string label) Route(string path, byte[]? plaintext, IPEndPoint? remoteEp)
    {
        if (path.IndexOf("/validate", StringComparison.OrdinalIgnoreCase) >= 0)
            return (_routes.ValidatePb, "validate");

        if (path.IndexOf("/compile", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Defense-in-depth: even if someone sets PURECRACK_BIND_ALL=1 or a
            // future bug exposes us, /compile must NEVER run a Roslyn build for
            // a non-loopback caller. The panel always lives on the same host as
            // the relay, so loopback is the only legitimate origin.
            var fromLoopback = IsLoopback(remoteEp);
            if (!fromLoopback)
            {
                Log.Warn($"refusing /compile from non-loopback origin {remoteEp} — serving canned response");
                return (_routes.Compile(plaintext: null, dynamicBuildEnabled: false),
                        "compile-canned-rejected-origin");
            }
            return (_routes.Compile(plaintext, DynamicBuildEnabled),
                    plaintext != null && DynamicBuildEnabled ? "compile-dynamic" : "compile-canned");
        }

        if (path.IndexOf("/heartbeat", StringComparison.OrdinalIgnoreCase) >= 0 ||
            path.IndexOf("/update-plugins", StringComparison.OrdinalIgnoreCase) >= 0)
            return (RouteHandlers.AckResponse(), "ack");

        // Unknown path — return validate-shaped response. Panel ignores
        // unrecognised endpoints anyway; this is just to avoid 404s in the
        // capture log.
        return (_routes.ValidatePb, "fallback-validate");
    }

    /// <summary>
    /// True if the endpoint is on the local machine. IPv4 127.0.0.0/8, IPv6 ::1,
    /// and IPv4-mapped-IPv6 ::ffff:127.0.0.1 all qualify. Null endpoint counts
    /// as non-loopback (fail-safe — better to serve a canned response than to
    /// trigger a build for an unknown origin).
    /// </summary>
    private static bool IsLoopback(IPEndPoint? ep)
    {
        if (ep?.Address == null) return false;
        var a = ep.Address;
        if (IPAddress.IsLoopback(a)) return true;
        if (a.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(a.MapToIPv4())) return true;
        return false;
    }

    private static void SendResponse(Stream s, byte[] responsePb)
    {
        var encrypted = Symmetric.AesEncryptFraming(responsePb);
        var headers =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: application/octet-stream\r\n" +
            $"Content-Length: {encrypted.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(headers);
        s.Write(headerBytes, 0, headerBytes.Length);
        s.Write(encrypted, 0, encrypted.Length);
        s.Flush();
    }
}
