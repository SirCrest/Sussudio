using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sussudio.Services.Runtime;

// Persisted user preferences. Missing nullable values mean "use current app
// defaults" so new settings can be added without breaking older installs.
public class UserSettings
{
    public string? SelectedDeviceId { get; set; }
    public string? OutputPath { get; set; }
    public string? SelectedRecordingFormat { get; set; }
    public string? SelectedQuality { get; set; }
    public string? SelectedPreset { get; set; }
    public string? SelectedSplitEncodeMode { get; set; }
    public double? CustomBitrateMbps { get; set; }
    public bool? IsHdrEnabled { get; set; }
    public bool? IsAudioEnabled { get; set; }
    public bool? IsAudioPreviewEnabled { get; set; }
    public bool? IsCustomAudioInputEnabled { get; set; }
    public string? SelectedAudioInputDeviceId { get; set; }
    public bool? IsMicrophoneEnabled { get; set; }
    public string? SelectedMicrophoneDeviceId { get; set; }
    public double? MicrophoneVolume { get; set; }
    public double? PreviewVolume { get; set; }
    public bool? IsStatsVisible { get; set; }
    public string? SelectedDeviceAudioMode { get; set; }
    public double? AnalogAudioGainPercent { get; set; }
    public bool? FlashbackGpuDecode { get; set; }
    public int? FlashbackBufferMinutes { get; set; }
}

[JsonSerializable(typeof(UserSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;

// LocalAppData settings store for the WinUI app. Load is forgiving and Save is
// serialized so UI updates cannot corrupt the JSON file.
public static class SettingsService
{
    private static readonly object _lock = new();

    private static string GetSettingsDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sussudio");

    private static string GetSettingsFilePath()
        => Path.Combine(GetSettingsDirectory(), "settings.json");

    public static UserSettings Load()
    {
        var settingsFilePath = GetSettingsFilePath();
        lock (_lock)
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                {
                    Logger.Log("SETTINGS_LOAD: no settings file found, using defaults.");
                    return new UserSettings();
                }

                var json = File.ReadAllText(settingsFilePath);
                var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.UserSettings);
                if (settings == null)
                {
                    Logger.Log("SETTINGS_LOAD: deserialization returned null, using defaults.");
                    return new UserSettings();
                }

                Logger.Log($"SETTINGS_LOAD: loaded from {settingsFilePath}");
                return settings;
            }
            catch (Exception ex)
            {
                Logger.Log($"SETTINGS_LOAD: failed to load ({ex.GetType().Name}: {ex.Message}), using defaults.");
                return new UserSettings();
            }
        }
    }

    public static void Save(UserSettings settings)
    {
        var settingsDirectory = GetSettingsDirectory();
        var settingsFilePath = GetSettingsFilePath();
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(settingsDirectory);
                var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.UserSettings);
                var tempPath = settingsFilePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, settingsFilePath, overwrite: true);
                Logger.Log($"SETTINGS_SAVE: saved to {settingsFilePath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"SETTINGS_SAVE: failed ({ex.GetType().Name}: {ex.Message})");
            }
        }
    }
}
