using System.Text.Json;
using Godot;

namespace MeiLinMod.MeiLinModCode.Mechanics.Settings;

/// <summary>
/// Shared compatibility contract for Fei, YukiMod, and MeiLinMod.
/// Each mod may ship the encounter independently, while only one provider enters encounter pools when co-loaded.
/// </summary>
public static class GloomyEncounterSharedSettings
{
    private const string SettingsDirName = "chaosmod";
    private const string SettingsFileName = "gloomy_encounter_settings.json";
    private const string EnabledDomainKey = "CHAOSMOD_GLOOMY_ENCOUNTER_ENABLED";
    private const string ProviderDomainKeyPrefix = "CHAOSMOD_GLOOMY_PROVIDER_";

    private static readonly string[] ProviderPriority = ["Fei", "YukiMod", "MeiLinMod"];
    private static int _loaded;
    private static bool _enabled = true;

    public static bool Enabled
    {
        get
        {
            EnsureLoaded();
            return ReadDomainBool(EnabledDomainKey, _enabled);
        }
    }

    public static void RegisterProvider(string modId)
    {
        EnsureLoaded();
        AppDomain.CurrentDomain.SetData(ProviderDomainKeyPrefix + modId, true);
    }

    public static bool IsActiveProvider(string modId)
    {
        EnsureLoaded();
        foreach (string candidate in ProviderPriority)
        {
            if (ReadDomainBool(ProviderDomainKeyPrefix + candidate, false))
                return string.Equals(candidate, modId, StringComparison.Ordinal);
        }

        return true;
    }

    public static void SetEnabled(bool value, bool persist)
    {
        EnsureLoaded();
        _enabled = value;
        AppDomain.CurrentDomain.SetData(EnabledDomainKey, value);
        if (persist)
            Save();
    }

    private static void EnsureLoaded()
    {
        if (Interlocked.Exchange(ref _loaded, 1) != 0)
            return;

        try
        {
            string path = GetSettingsPath();
            if (File.Exists(path))
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("Enabled", out JsonElement enabled))
                {
                    if (enabled.ValueKind == JsonValueKind.True)
                        _enabled = true;
                    else if (enabled.ValueKind == JsonValueKind.False)
                        _enabled = false;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn("[GloomyEncounterSharedSettings] Load failed: " + ex.Message);
        }

        AppDomain.CurrentDomain.SetData(EnabledDomainKey, _enabled);
    }

    private static void Save()
    {
        try
        {
            string path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new SettingsData { Enabled = Enabled }));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn("[GloomyEncounterSharedSettings] Save failed: " + ex.Message);
        }
    }

    private static bool ReadDomainBool(string key, bool fallback)
    {
        try
        {
            object? value = AppDomain.CurrentDomain.GetData(key);
            if (value is bool result)
                return result;
            if (value is string text && bool.TryParse(text, out result))
                return result;
        }
        catch
        {
        }

        return fallback;
    }

    private static string GetSettingsPath()
    {
        string baseDir = OS.GetUserDataDir();
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = ProjectSettings.GlobalizePath("user://");
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, SettingsDirName, SettingsFileName);
    }

    private sealed class SettingsData
    {
        public bool Enabled { get; set; } = true;
    }
}
