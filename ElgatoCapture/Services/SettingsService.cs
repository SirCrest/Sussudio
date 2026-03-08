using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElgatoCapture.Services;

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
    public double? PreviewVolume { get; set; }
}

[JsonSerializable(typeof(UserSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;

public static class SettingsService
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElgatoCapture");

    private static readonly string SettingsFilePath =
        Path.Combine(SettingsDirectory, "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Logger.Log("SETTINGS_LOAD: no settings file found, using defaults.");
                return new UserSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.UserSettings);
            if (settings == null)
            {
                Logger.Log("SETTINGS_LOAD: deserialization returned null, using defaults.");
                return new UserSettings();
            }

            Logger.Log($"SETTINGS_LOAD: loaded from {SettingsFilePath}");
            return settings;
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_LOAD: failed to load ({ex.GetType().Name}: {ex.Message}), using defaults.");
            return new UserSettings();
        }
    }

    public static void Save(UserSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.UserSettings);
            File.WriteAllText(SettingsFilePath, json);
            Logger.Log($"SETTINGS_SAVE: saved to {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_SAVE: failed ({ex.GetType().Name}: {ex.Message})");
        }
    }
}
