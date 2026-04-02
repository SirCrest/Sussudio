using System.Collections.Generic;
using System.Globalization;
using System;
using System.Linq;

namespace ElgatoCapture.Tools;

internal static class AutomationPipeProtocol
{
    internal const string DefaultPipeName = "ElgatoCaptureAutomation";
    internal const string AutomationKeyEnvVar = "ELGATOCAPTURE_AUTOMATION_TOKEN";
    internal const int DefaultConnectTimeoutMs = 5000;
    internal const int DefaultResponseTimeoutMs = 15000;
    internal const int ExtendedResponseTimeoutMs = 60000;
    internal const int DefaultNotReadyRetries = 15;
    internal const int DefaultNotReadyDelayMs = 1000;

    internal static IReadOnlyDictionary<string, int> CommandMap { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authenticate"] = 0,
            ["GetSnapshot"] = 1,
            ["GetDiagnostics"] = 2,
            ["RefreshDevices"] = 3,
            ["SelectDevice"] = 4,
            ["SelectAudioInputDevice"] = 5,
            ["SetCustomAudioInput"] = 6,
            ["SetResolution"] = 7,
            ["SetFrameRate"] = 8,
            ["SetRecordingFormat"] = 9,
            ["SetQuality"] = 10,
            ["SetCustomBitrate"] = 11,
            ["SetHdrEnabled"] = 12,
            ["SetAudioEnabled"] = 13,
            ["SetAudioPreviewEnabled"] = 14,
            ["SetOutputPath"] = 15,
            ["SetPreviewEnabled"] = 16,
            ["SetRecordingEnabled"] = 17,
            ["ArmClose"] = 18,
            ["WindowAction"] = 19,
            ["WaitForCondition"] = 20,
            ["VerifyLastRecording"] = 21,
            ["AssertSnapshot"] = 22,
            ["SetTrueHdrPreviewEnabled"] = 23,
            ["ProbeVideoSource"] = 24,
            ["ProbePreviewColor"] = 25,
            ["CapturePreviewFrame"] = 26,
            ["CaptureWindowScreenshot"] = 27,
            ["SetVideoFormat"] = 28,
            ["GetCaptureOptions"] = 29,
            ["SetPreset"] = 30,
            ["SetSplitEncodeMode"] = 31,
            ["SetMjpegDecoderCount"] = 32,
            ["SetShowAllCaptureOptions"] = 33,
            ["SetPreviewVolume"] = 34,
            ["SetStatsVisible"] = 35,
            ["SetDeviceAudioMode"] = 36,
            ["GetPerformanceTimeline"] = 37,
            ["SetStatsSectionVisible"] = 38,
            ["SetAnalogAudioGain"] = 39,
            ["SetSettingsVisible"] = 40,
            ["FlashbackAction"] = 41,
            ["FlashbackExport"] = 42,
            ["FlashbackGetSegments"] = 43,
            ["VerifyFile"] = 44
        };

    internal static string? GetConfiguredAuthToken(string? explicitAuthToken = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitAuthToken))
        {
            return explicitAuthToken;
        }

        var envToken = Environment.GetEnvironmentVariable(AutomationKeyEnvVar);
        return string.IsNullOrWhiteSpace(envToken) ? null : envToken;
    }

    internal static int GetDefaultResponseTimeout(string commandName)
    {
        return commandName switch
        {
            "WaitForCondition" or "VerifyLastRecording" or "CapturePreviewFrame" or
            "CaptureWindowScreenshot" or "FlashbackExport" or "VerifyFile" => ExtendedResponseTimeoutMs,
            _ => DefaultResponseTimeoutMs
        };
    }

    internal static bool TryGetCommandValue(string commandName, out int commandValue)
        => CommandMap.TryGetValue(commandName, out commandValue);

    internal static Dictionary<string, object?> CreateRequestEnvelope(
        int commandValue,
        Dictionary<string, object?>? payload = null,
        string? authToken = null)
    {
        return new Dictionary<string, object?>
        {
            ["command"] = commandValue,
            ["correlationId"] = Guid.NewGuid().ToString("N"),
            ["authToken"] = authToken ?? GetConfiguredAuthToken(),
            ["payload"] = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal)
        };
    }

    internal static int ResolveCommand(string command)
    {
        if (int.TryParse(command, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric;
        }

        if (CommandMap.TryGetValue(command, out var directMatch))
        {
            return directMatch;
        }

        var normalized = Normalize(command);
        foreach (var entry in CommandMap)
        {
            if (Normalize(entry.Key).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        throw new ArgumentException($"Unknown command '{command}'.");
    }

    private static string Normalize(string value)
    {
        var buffer = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(buffer);
    }
}
