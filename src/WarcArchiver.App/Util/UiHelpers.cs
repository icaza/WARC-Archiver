using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WarcArchiver.App.Util;

/// <summary>Small, UI-independent helpers: number formatting, clipboard and Explorer access.</summary>
internal static class UiHelpers
{
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "—";
        }

        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0
            ? bytes.ToString(CultureInfo.InvariantCulture) + " B"
            : value.ToString(value >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture) + " " + units[unit];
    }

    /// <summary>First <paramref name="length"/> characters followed by an ellipsis (used for long hashes in tables).</summary>
    public static string Shorten(string? text, int length)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        return text.Length <= length ? text : text[..length] + "…";
    }

    /// <summary>Formats an ISO-8601 timestamp (as stored in manifests and the audit log) for display in local-time columns.</summary>
    public static string FormatTimestamp(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return string.Empty;
        }
        return DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset value)
            ? value.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
            : iso;
    }

    /// <summary>Puts text on the clipboard. The clipboard can be locked by another program, so failure is reported, not thrown.</summary>
    public static bool TryCopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
        catch (ThreadStateException)
        {
            return false;
        }
    }

    /// <summary>Opens Explorer on a folder, or on the folder containing a file with that file selected.</summary>
    public static bool TryShowInExplorer(string path)
    {
        try
        {
            var start = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
            if (File.Exists(path))
            {
                start.Arguments = "/select,\"" + path + "\"";
            }
            else if (Directory.Exists(path))
            {
                start.Arguments = "\"" + path + "\"";
            }
            else
            {
                return false;
            }
            using Process? process = Process.Start(start);
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        {
            return false;
        }
    }

    /// <summary>Opens a text file in the user's default editor, falling back to Notepad when .json has no association.</summary>
    public static bool TryOpenInEditor(string path)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            try
            {
                var notepad = new ProcessStartInfo("notepad.exe") { UseShellExecute = false };
                notepad.ArgumentList.Add(path);
                using Process? process = Process.Start(notepad);
                return true;
            }
            catch (Exception inner) when (inner is Win32Exception or InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>Removes spaces and colons and upper-cases, so thumbprints copied from any dialog compare equal.</summary>
    public static string NormalizeThumbprint(string? thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return string.Empty;
        }
        return new string(thumbprint.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
    }
}
