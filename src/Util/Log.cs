using System;
using System.Runtime.InteropServices;

namespace PureCrack.Util;

/// <summary>
/// Single-purpose colored console logger. No log levels, no filtering — every call
/// hits stdout. Color codes are stripped automatically when stdout is redirected
/// or when the console doesn't support virtual-terminal processing.
/// </summary>
internal static class Log
{
    private static readonly object Lock = new();

    // Order matters: TryEnableVirtualTerminal() must run before UseColor is
    // computed, so the success of VT-enabling feeds into the decision to emit
    // raw escape codes vs strip them.
    private static readonly bool UseColor =
        !Console.IsOutputRedirected && TryEnableVirtualTerminal();

    private const string Reset = "\x1b[0m";
    private const string Bold  = "\x1b[1m";
    private const string Red   = "\x1b[91m";
    private const string Green = "\x1b[92m";
    private const string Yellow= "\x1b[93m";
    private const string Blue  = "\x1b[94m";
    private const string Gray  = "\x1b[90m";

    public static void Banner(string text)
    {
        var bar = new string('=', text.Length + 4);
        lock (Lock)
        {
            Write(Bold + bar + Reset + "\n");
            Write(Bold + "  " + text + "  " + Reset + "\n");
            Write(Bold + bar + Reset + "\n");
        }
    }

    public static void Section(string text)
    {
        lock (Lock) Write("\n" + Bold + Blue + ":: " + text + Reset + "\n");
    }

    public static void Info(string text)  => Tagged(Blue,   "[*]", text);
    public static void Ok(string text)    => Tagged(Green,  "[+]", text);
    public static void Warn(string text)  => Tagged(Yellow, "[!]", text);
    public static void Err(string text)   => Tagged(Red,    "[X]", text);
    public static void Debug(string text) => Tagged(Gray,   "[.]", text);

    public static void Bullet(string text)
    {
        lock (Lock) Write("    " + text + "\n");
    }

    public static void Kv(string key, string value)
    {
        lock (Lock) Write("    " + Gray + key.PadRight(14) + Reset + value + "\n");
    }

    private static void Tagged(string color, string tag, string text)
    {
        lock (Lock) Write(color + tag + Reset + " " + text + "\n");
    }

    private static void Write(string s)
    {
        if (!UseColor)
        {
            // Strip ANSI escape sequences when redirected or VT-unsupported.
            var i = 0;
            while (i < s.Length)
            {
                if (s[i] == '\x1b' && i + 1 < s.Length && s[i + 1] == '[')
                {
                    var end = s.IndexOf('m', i);
                    if (end < 0) break;
                    i = end + 1;
                }
                else
                {
                    Console.Write(s[i]);
                    i++;
                }
            }
        }
        else
        {
            Console.Write(s);
        }
    }

    // ============================================================================
    // Win32 — enable virtual terminal processing on stdout
    // ============================================================================
    //
    // Win10 1809+ supports ANSI escape codes in conhost.exe, but doesn't
    // enable them by default on every console handle. Without this enable,
    // cmd.exe shows raw "\x1b[91m" garbage instead of color. Calling
    // SetConsoleMode with ENABLE_VIRTUAL_TERMINAL_PROCESSING flips it on.
    //
    // Returns true if escape codes will render correctly. False on legacy
    // consoles (pre-1809), in which case we strip codes in Write() above.

    private const int    STD_OUTPUT_HANDLE                 = -11;
    private const uint   ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private static readonly IntPtr InvalidHandleValue       = new(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private static bool TryEnableVirtualTerminal()
    {
        try
        {
            var h = GetStdHandle(STD_OUTPUT_HANDLE);
            if (h == IntPtr.Zero || h == InvalidHandleValue) return false;
            if (!GetConsoleMode(h, out var mode)) return false;
            if ((mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0) return true; // already on
            return SetConsoleMode(h, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
        catch
        {
            return false;
        }
    }
}
