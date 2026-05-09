using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Tools;

internal enum AutomationCommandPathPolicy
{
    None,
    ReadFile,
    WriteFile,
    Directory
}

internal sealed record AutomationCommandMetadata(
    AutomationCommandKind Kind,
    string Name,
    string PayloadShape,
    bool RequiresReadyDevices,
    int ResponseTimeoutMs,
    AutomationCommandPathPolicy PathPolicy,
    string CliHelp,
    string McpDescription);

// Shared command metadata used to keep the app server, ssctl, MCP, and raw
// automation client from drifting on readiness gates, path-bearing payloads,
// and long-running command timeout policy.
internal static class AutomationCommandCatalog
{
    internal static IReadOnlyList<AutomationCommandMetadata> Entries { get; } =
        BuildEntries()
            .OrderBy(entry => (int)entry.Kind)
            .ToArray();

    private static IReadOnlyDictionary<AutomationCommandKind, AutomationCommandMetadata> EntriesByKind { get; } =
        Entries.ToDictionary(entry => entry.Kind);

    internal static AutomationCommandMetadata Get(AutomationCommandKind kind)
    {
        if (EntriesByKind.TryGetValue(kind, out var metadata))
        {
            return metadata;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown automation command kind.");
    }

    internal static bool TryGet(string commandName, out AutomationCommandMetadata metadata)
    {
        if (TryResolveKind(commandName, out var kind))
        {
            metadata = Get(kind);
            return true;
        }

        metadata = null!;
        return false;
    }

    internal static string ValidatePath(
        AutomationCommandKind kind,
        string payloadKey,
        string path)
    {
        var metadata = Get(kind);
        if (metadata.PathPolicy == AutomationCommandPathPolicy.None)
        {
            return path;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"{metadata.Name} requires non-empty path payload '{payloadKey}'.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"{metadata.Name} payload '{payloadKey}' is not a valid path: {ex.Message}", ex);
        }

        switch (metadata.PathPolicy)
        {
            case AutomationCommandPathPolicy.WriteFile:
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new InvalidOperationException($"{metadata.Name} payload '{payloadKey}' must include a writable file path.");
                }

                Directory.CreateDirectory(directory);
                break;
            }
            case AutomationCommandPathPolicy.Directory:
                Directory.CreateDirectory(fullPath);
                break;
            case AutomationCommandPathPolicy.ReadFile:
                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException($"{metadata.Name} payload '{payloadKey}' must reference an existing file: '{fullPath}'.");
                }
                break;
        }

        return path;
    }

    private static bool TryResolveKind(string commandName, out AutomationCommandKind kind)
    {
        if (int.TryParse(commandName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) &&
            Enum.IsDefined(typeof(AutomationCommandKind), numeric))
        {
            kind = (AutomationCommandKind)numeric;
            return true;
        }

        if (Enum.TryParse(commandName, ignoreCase: true, out kind) &&
            Enum.IsDefined(kind))
        {
            return true;
        }

        var normalized = Normalize(commandName);
        foreach (var candidate in Enum.GetValues<AutomationCommandKind>())
        {
            if (Normalize(candidate.ToString()).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        kind = default;
        return false;
    }

    private static IReadOnlyList<AutomationCommandMetadata> BuildEntries()
    {
        var entries = Enum.GetValues<AutomationCommandKind>()
            .Select(CreateDefault)
            .ToDictionary(entry => entry.Kind);

        Set(entries, AutomationCommandKind.Authenticate, "{ authToken?: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "auth", "Validate automation authentication token.");
        Set(entries, AutomationCommandKind.GetSnapshot, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "state", "Get full application state snapshot.");
        Set(entries, AutomationCommandKind.GetDiagnostics, "{ maxEvents?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "diagnostics [--max N]", "Get recent diagnostic events.");
        Set(entries, AutomationCommandKind.RefreshDevices, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device refresh", "Refresh capture and audio device lists.");
        Set(entries, AutomationCommandKind.SelectDevice, "{ deviceId?: string, deviceName?: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device select <name>", "Select a capture device.");
        Set(entries, AutomationCommandKind.SelectAudioInputDevice, "{ deviceId?: string, deviceName?: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device audio-select <name>", "Select an audio input device.");
        Set(entries, AutomationCommandKind.SetCustomAudioInput, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device custom-audio on|off", "Enable or disable custom audio input.");
        Set(entries, AutomationCommandKind.SetResolution, "{ resolution: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set resolution <value>", "Set capture resolution.");
        Set(entries, AutomationCommandKind.SetFrameRate, "{ frameRate: double }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set fps <value>", "Set capture frame rate.");
        Set(entries, AutomationCommandKind.SetRecordingFormat, "{ format: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set format <value>", "Set recording container and codec format.");
        Set(entries, AutomationCommandKind.SetQuality, "{ quality: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set quality <value>", "Set recording quality.");
        Set(entries, AutomationCommandKind.SetCustomBitrate, "{ bitrateMbps: double }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set bitrate <value>", "Set custom recording bitrate in Mbps.");
        Set(entries, AutomationCommandKind.SetHdrEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set hdr on|off", "Enable or disable HDR recording mode.");
        Set(entries, AutomationCommandKind.SetAudioEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set audio on|off", "Enable or disable audio capture.");
        Set(entries, AutomationCommandKind.SetAudioPreviewEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set audio-preview on|off", "Enable or disable live audio monitoring.");
        Set(entries, AutomationCommandKind.SetOutputPath, "{ outputPath: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.Directory, "set output <path>", "Set recording output directory.");
        Set(entries, AutomationCommandKind.SetPreviewEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "preview start|stop", "Start or stop live preview.");
        Set(entries, AutomationCommandKind.SetRecordingEnabled, "{ enabled: bool }", ready: true, timeoutMs: AutomationPipeProtocol.RecordingResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "record start|stop", "Start or stop recording.");
        Set(entries, AutomationCommandKind.ArmClose, "{ armed?: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window close --arm", "Arm the next window close command.");
        Set(entries, AutomationCommandKind.WindowAction, "{ action: string, x?: int, y?: int, width?: int, height?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window <action>", "Control application window state and placement.");
        Set(entries, AutomationCommandKind.WaitForCondition, "{ condition: string, timeoutMs?: int, pollMs?: int }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "wait <condition>", "Wait for an automation condition.");
        Set(entries, AutomationCommandKind.VerifyLastRecording, "{}", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "verify", "Verify the last recording.");
        Set(entries, AutomationCommandKind.AssertSnapshot, "{ assertions: array }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "assert <json>", "Assert fields against the current snapshot.");
        Set(entries, AutomationCommandKind.SetTrueHdrPreviewEnabled, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set hdr-preview on|off", "Enable or disable true HDR preview.");
        Set(entries, AutomationCommandKind.ProbeVideoSource, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "probe source", "Probe live video source formats.");
        Set(entries, AutomationCommandKind.ProbePreviewColor, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "probe color", "Probe preview color metadata.");
        Set(entries, AutomationCommandKind.CapturePreviewFrame, "{ outputPath?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "frame [path]", "Capture the next preview frame to disk.");
        Set(entries, AutomationCommandKind.CaptureWindowScreenshot, "{ outputPath?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "screenshot [path]", "Capture the application window to disk.");
        Set(entries, AutomationCommandKind.SetVideoFormat, "{ videoFormat: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set video-format <value>", "Set video format override.");
        Set(entries, AutomationCommandKind.GetCaptureOptions, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "options", "Get capture option metadata.");
        Set(entries, AutomationCommandKind.SetPreset, "{ preset: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set preset <value>", "Set encoder preset.");
        Set(entries, AutomationCommandKind.SetSplitEncodeMode, "{ splitEncodeMode: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set split <value>", "Set split encode mode.");
        Set(entries, AutomationCommandKind.SetMjpegDecoderCount, "{ decoderCount: int }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set decoders <value>", "Set MJPEG decoder count.");
        Set(entries, AutomationCommandKind.SetShowAllCaptureOptions, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set show-all on|off", "Show or hide advanced capture options.");
        Set(entries, AutomationCommandKind.SetPreviewVolume, "{ previewVolumePercent: double }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set volume <value>", "Set preview audio monitor volume.");
        Set(entries, AutomationCommandKind.SetStatsVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "stats show|hide", "Show or hide stats panel.");
        Set(entries, AutomationCommandKind.SetDeviceAudioMode, "{ mode: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set audio-mode hdmi|analog", "Set device audio mode.");
        Set(entries, AutomationCommandKind.GetPerformanceTimeline, "{ maxEntries?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "timeline [--max N]", "Get performance timeline samples.");
        Set(entries, AutomationCommandKind.SetStatsSectionVisible, "{ section: string, visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "stats section <name> show|hide", "Show or hide a stats section.");
        Set(entries, AutomationCommandKind.SetAnalogAudioGain, "{ gain: double }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set gain <value>", "Set analog audio input gain.");
        Set(entries, AutomationCommandKind.SetSettingsVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "settings show|hide", "Show or hide settings panel.");
        Set(entries, AutomationCommandKind.FlashbackAction, "{ action: string, positionMs?: double }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback <action>", "Control flashback playback and range markers.");
        Set(entries, AutomationCommandKind.FlashbackExport, "{ seconds?: double, outputPath: string, useSelectionRange?: bool }", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "flashback export [seconds] [path] [--range]", "Export flashback buffer to MP4.");
        Set(entries, AutomationCommandKind.FlashbackGetSegments, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback segments", "List flashback buffer segments.");
        Set(entries, AutomationCommandKind.VerifyFile, "{ filePath: string, verificationProfile?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.ReadFile, "verify <path>", "Verify an arbitrary media file.");
        Set(entries, AutomationCommandKind.RestartFlashback, "{}", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "flashback apply", "Restart Flashback to apply deferred settings.");
        Set(entries, AutomationCommandKind.SetMicrophoneEnabled, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set mic on|off", "Enable or disable microphone recording.");
        Set(entries, AutomationCommandKind.SetFlashbackEnabled, "{ enabled: bool }", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "flashback on|off", "Enable or disable Flashback.");
        Set(entries, AutomationCommandKind.GetAudioRampTrace, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "audio-ramp-trace", "Get audio ramp trace diagnostics.");
        Set(entries, AutomationCommandKind.SetFrameTimeOverlayVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "frametime show|hide", "Show or hide the frametime graph overlay.");
        Set(entries, AutomationCommandKind.SetFlashbackTimelineVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback timeline show|hide", "Show or hide the Flashback timeline UI.");

        return entries.Values.ToArray();
    }

    private static AutomationCommandMetadata CreateDefault(AutomationCommandKind kind)
        => new(
            kind,
            kind.ToString(),
            "{}",
            RequiresReadyDevices: false,
            ResponseTimeoutMs: DefaultTimeout,
            PathPolicy: AutomationCommandPathPolicy.None,
            CliHelp: kind.ToString(),
            McpDescription: $"Automation command {kind}.");

    private static void Set(
        Dictionary<AutomationCommandKind, AutomationCommandMetadata> entries,
        AutomationCommandKind kind,
        string payloadShape,
        bool ready,
        int timeoutMs,
        AutomationCommandPathPolicy pathPolicy,
        string cliHelp,
        string mcpDescription)
        => entries[kind] = new AutomationCommandMetadata(
            kind,
            kind.ToString(),
            payloadShape,
            ready,
            timeoutMs,
            pathPolicy,
            cliHelp,
            mcpDescription);

    private static int DefaultTimeout => AutomationPipeProtocol.DefaultResponseTimeoutMs;

    private static string Normalize(string value)
    {
        var buffer = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(buffer);
    }
}
