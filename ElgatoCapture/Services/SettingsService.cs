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
    public bool? IsMicrophoneEnabled { get; set; }
    public string? SelectedMicrophoneDeviceId { get; set; }
    public double? MicrophoneVolume { get; set; }
    public double? PreviewVolume { get; set; }
    public bool? ShowAllCaptureOptions { get; set; }
    public bool? IsStatsVisible { get; set; }
    public string? SelectedDeviceAudioMode { get; set; }
    public double? AnalogAudioGainPercent { get; set; }
    public bool? FlashbackGpuDecode { get; set; }
    public int? FlashbackBufferMinutes { get; set; }
}

[JsonSerializable(typeof(UserSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class SettingsJsonContext : JsonSerializerContext;

public static class SettingsService
{
    private static string GetSettingsDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElgatoCapture");

    private static string GetSettingsFilePath()
        => Path.Combine(GetSettingsDirectory(), "settings.json");

    public static UserSettings Load()
    {
        var settingsFilePath = GetSettingsFilePath();
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

    public static void Save(UserSettings settings)
    {
        var settingsDirectory = GetSettingsDirectory();
        var settingsFilePath = GetSettingsFilePath();
        try
        {
            Directory.CreateDirectory(settingsDirectory);
            var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.UserSettings);
            File.WriteAllText(settingsFilePath, json);
            Logger.Log($"SETTINGS_SAVE: saved to {settingsFilePath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_SAVE: failed ({ex.GetType().Name}: {ex.Message})");
        }
    }
}
