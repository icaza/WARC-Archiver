using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WarcArchiver.Core.Config;

public sealed record ConfigLoadResult(AppConfig Config, string Path, string? Warning, string Sha256Hex);

public static class ConfigStore
{
    const long MaxConfigBytes = 1024 * 1024;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>config.json next to the executable; falls back to %LOCALAPPDATA%\WarcArchiver when that folder is read-only.</summary>
    public static string ResolvePath()
    {
        string beside = System.IO.Path.Combine(AppContext.BaseDirectory, "config.json");
        if (File.Exists(beside) || CanWriteTo(AppContext.BaseDirectory))
        {
            return beside;
        }
        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WarcArchiver",
            "config.json");
    }

    public static ConfigLoadResult Load(string? path = null)
    {
        path ??= ResolvePath();
        string? warning = null;
        var config = new AppConfig();
        string hash = string.Empty;

        try
        {
            if (!File.Exists(path))
            {
                Save(config, path);
                warning = "config.json was not found, so a default one was created.";
            }

            var info = new FileInfo(path);
            if (info.Length > MaxConfigBytes)
            {
                warning = "config.json is larger than 1 MB and was ignored. Built-in defaults are in use.";
            }
            else
            {
                byte[] bytes = File.ReadAllBytes(path);
                hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
                config = JsonSerializer.Deserialize<AppConfig>(bytes, JsonOptions) ?? new AppConfig();
            }
        }
        catch (JsonException ex)
        {
            config = new AppConfig();
            warning = $"config.json is not valid JSON (line {ex.LineNumber + 1}, position {ex.BytePositionInLine}). " +
                      "Built-in defaults are in use and your file was left untouched.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            config = new AppConfig();
            warning = $"config.json could not be read or created ({ex.Message}). Built-in defaults are in use.";
        }

        config.Normalize();
        return new ConfigLoadResult(config, path, warning, hash);
    }

    public static void Save(AppConfig config, string path)
    {
        string? dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(temp, path, overwrite: true);
    }

    static bool CanWriteTo(string directory)
    {
        try
        {
            string probe = System.IO.Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
