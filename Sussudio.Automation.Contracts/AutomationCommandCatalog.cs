using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sussudio.Models;

namespace Sussudio.Tools;

public enum AutomationCommandPathPolicy
{
    None,
    ReadFile,
    WriteFile,
    Directory
}

public enum AutomationPayloadFieldType
{
    String,
    Boolean,
    Integer,
    Number,
    Object,
    Array
}

public sealed record AutomationPayloadFieldMetadata(
    string Name,
    AutomationPayloadFieldType Type,
    bool Required);

public sealed record AutomationCommandMetadata(
    AutomationCommandKind Kind,
    string Name,
    string PayloadShape,
    IReadOnlyList<AutomationPayloadFieldMetadata> PayloadFields,
    bool RequiresReadyDevices,
    int ResponseTimeoutMs,
    AutomationCommandPathPolicy PathPolicy,
    string CliHelp,
    string McpDescription);

public sealed record AutomationManifest(
    int SchemaVersion,
    IReadOnlyList<AutomationManifestCommand> Commands);

public sealed record AutomationManifestCommand(
    int Id,
    string Name,
    string PayloadShape,
    IReadOnlyList<AutomationManifestPayloadField> PayloadFields,
    int ResponseTimeoutMs,
    bool RequiresReadyDevices,
    string PathPolicy,
    string CliHelp,
    string McpDescription);

public sealed record AutomationManifestPayloadField(
    string Name,
    string Type,
    bool Required);

// Shared command metadata used to keep the app server, ssctl, MCP, and raw
// automation client from drifting on readiness gates, path-bearing payloads,
// and long-running command timeout policy.
public static class AutomationCommandCatalog
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new();

    public static IReadOnlyList<AutomationCommandMetadata> Entries { get; } =
        BuildEntries()
            .OrderBy(entry => (int)entry.Kind)
            .ToArray();

    private static IReadOnlyDictionary<AutomationCommandKind, AutomationCommandMetadata> EntriesByKind { get; } =
        Entries.ToDictionary(entry => entry.Kind);

    public static AutomationCommandMetadata Get(AutomationCommandKind kind)
    {
        if (EntriesByKind.TryGetValue(kind, out var metadata))
        {
            return metadata;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown automation command kind.");
    }

    public static AutomationManifest CreateManifest()
        => new(
            SchemaVersion: 1,
            Commands: Entries
                .Select(entry => new AutomationManifestCommand(
                    Id: (int)entry.Kind,
                    Name: entry.Name,
                    PayloadShape: entry.PayloadShape,
                    PayloadFields: entry.PayloadFields
                        .Select(field => new AutomationManifestPayloadField(
                            field.Name,
                            field.Type.ToString(),
                            field.Required))
                        .ToArray(),
                    ResponseTimeoutMs: entry.ResponseTimeoutMs,
                    RequiresReadyDevices: entry.RequiresReadyDevices,
                    PathPolicy: entry.PathPolicy.ToString(),
                    CliHelp: entry.CliHelp,
                    McpDescription: entry.McpDescription))
                .ToArray());

    public static string CreateManifestJson()
        => JsonSerializer.Serialize(CreateManifest(), ManifestJsonOptions);

    public static bool TryGet(string commandName, out AutomationCommandMetadata metadata)
    {
        if (TryResolveKind(commandName, out var kind))
        {
            metadata = Get(kind);
            return true;
        }

        metadata = null!;
        return false;
    }

    public static string ValidatePath(
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

        Set(entries, AutomationCommandKind.Authenticate, "{ authToken?: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "auth", "Validate automation authentication token.", Optional("authToken", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.GetSnapshot, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "state", "Get full application state snapshot.");
        Set(entries, AutomationCommandKind.GetDiagnostics, "{ maxEvents?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "diagnostics [--max N]", "Get recent diagnostic events.", Optional("maxEvents", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.RefreshDevices, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device refresh", "Refresh capture and audio device lists.");
        Set(entries, AutomationCommandKind.SelectDevice, "{ deviceId?: string, deviceName?: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device select <name>", "Select a capture device.", Optional("deviceId", AutomationPayloadFieldType.String), Optional("deviceName", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SelectAudioInputDevice, "{ deviceId?: string, deviceName?: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device audio-select <name>", "Select an audio input device.", Optional("deviceId", AutomationPayloadFieldType.String), Optional("deviceName", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetCustomAudioInput, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "device custom-audio on|off", "Enable or disable custom audio input.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetResolution, "{ resolution: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set resolution <value>", "Set capture resolution.", Required("resolution", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetFrameRate, "{ frameRate: double }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set fps <value>", "Set capture frame rate.", Required("frameRate", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.SetRecordingFormat, "{ format: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set format <value>", "Set recording container and codec format.", Required("format", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetQuality, "{ quality: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set quality <value>", "Set recording quality.", Required("quality", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetCustomBitrate, "{ bitrateMbps: double }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set bitrate <value>", "Set custom recording bitrate in Mbps.", Required("bitrateMbps", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.SetHdrEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set hdr on|off", "Enable or disable HDR recording mode.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetAudioEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set audio on|off", "Enable or disable audio capture.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetAudioPreviewEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set audio-preview on|off", "Enable or disable live audio monitoring.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetOutputPath, "{ outputPath: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.Directory, "set output <path>", "Set recording output directory.", Required("outputPath", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetPreviewEnabled, "{ enabled: bool }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "preview start|stop", "Start or stop live preview.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetRecordingEnabled, "{ enabled: bool }", ready: true, timeoutMs: AutomationPipeProtocol.RecordingResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "record start|stop", "Start or stop recording.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.ArmClose, "{ armed?: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window close --arm", "Arm the next window close command.", Optional("armed", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.WindowAction, "{ action: string, x?: int, y?: int, width?: int, height?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window <action>", "Control application window state and placement.", Required("action", AutomationPayloadFieldType.String), Optional("x", AutomationPayloadFieldType.Integer), Optional("y", AutomationPayloadFieldType.Integer), Optional("width", AutomationPayloadFieldType.Integer), Optional("height", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.WaitForCondition, "{ condition: string, timeoutMs?: int, pollMs?: int }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "wait <condition>", "Wait for an automation condition.", Required("condition", AutomationPayloadFieldType.String), Optional("timeoutMs", AutomationPayloadFieldType.Integer), Optional("pollMs", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.VerifyLastRecording, "{}", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "verify", "Verify the last recording.");
        Set(entries, AutomationCommandKind.AssertSnapshot, "{ assertions: array }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "assert <json>", "Assert fields against the current snapshot.", Required("assertions", AutomationPayloadFieldType.Array));
        Set(entries, AutomationCommandKind.SetTrueHdrPreviewEnabled, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set hdr-preview on|off", "Enable or disable true HDR preview.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.ProbeVideoSource, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "probe source", "Probe live video source formats.");
        Set(entries, AutomationCommandKind.ProbePreviewColor, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "probe color", "Probe preview color metadata.");
        Set(entries, AutomationCommandKind.CapturePreviewFrame, "{ outputPath?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "frame [path]", "Capture the next preview frame to disk.", Optional("outputPath", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.CaptureWindowScreenshot, "{ outputPath?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "screenshot [path]", "Capture the application window to disk.", Optional("outputPath", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetVideoFormat, "{ videoFormat: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set video-format <value>", "Set video format override.", Required("videoFormat", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.GetCaptureOptions, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "options", "Get capture option metadata.");
        Set(entries, AutomationCommandKind.SetPreset, "{ preset: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set preset <value>", "Set encoder preset.", Required("preset", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetSplitEncodeMode, "{ splitEncodeMode: string }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set split <value>", "Set split encode mode.", Required("splitEncodeMode", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.SetMjpegDecoderCount, "{ decoderCount: int }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set decoders <value>", "Set MJPEG decoder count.", Required("decoderCount", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.SetShowAllCaptureOptions, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set show-all on|off", "Show or hide advanced capture options.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetPreviewVolume, "{ previewVolumePercent: double }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set volume <value>", "Set preview audio monitor volume.", Required("previewVolumePercent", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.SetStatsVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "stats show|hide", "Show or hide stats panel.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetDeviceAudioMode, "{ mode: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set audio-mode hdmi|analog", "Set device audio mode.", Required("mode", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.GetPerformanceTimeline, "{ maxEntries?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "timeline [--max N]", "Get performance timeline samples.", Optional("maxEntries", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.SetStatsSectionVisible, "{ section: string, visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "stats section <name> show|hide", "Show or hide a stats section.", Required("section", AutomationPayloadFieldType.String), Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetAnalogAudioGain, "{ gain: double }", ready: true, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set gain <value>", "Set analog audio input gain.", Required("gain", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.SetSettingsVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "settings show|hide", "Show or hide settings panel.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.FlashbackAction, "{ action: string, positionMs?: double }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback <action>", "Control flashback playback and range markers.", Required("action", AutomationPayloadFieldType.String), Optional("positionMs", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.FlashbackExport, "{ seconds?: double, outputPath: string, useSelectionRange?: bool, force?: bool }", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "flashback export [seconds] [path] [--range] [--force]", "Export flashback buffer to MP4. Refuses an existing destination unless force=true.", Optional("seconds", AutomationPayloadFieldType.Number), Required("outputPath", AutomationPayloadFieldType.String), Optional("useSelectionRange", AutomationPayloadFieldType.Boolean), Optional("force", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.FlashbackGetSegments, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback segments", "List flashback buffer segments.");
        Set(entries, AutomationCommandKind.VerifyFile, "{ filePath: string, verificationProfile?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.ReadFile, "verify <path> [--profile NAME]", "Verify an arbitrary media file.", Required("filePath", AutomationPayloadFieldType.String), Optional("verificationProfile", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.RestartFlashback, "{}", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "flashback apply", "Restart Flashback to apply deferred settings.");
        Set(entries, AutomationCommandKind.SetMicrophoneEnabled, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "set mic on|off", "Enable or disable microphone recording.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetFlashbackEnabled, "{ enabled: bool }", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "flashback on|off", "Enable or disable Flashback.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.GetAudioRampTrace, "{ maxEntries?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "audio-ramp-trace", "Get audio ramp trace diagnostics.", Optional("maxEntries", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.SetFrameTimeOverlayVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "frametime show|hide", "Show or hide the frametime graph overlay.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.SetFlashbackTimelineVisible, "{ visible: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback timeline show|hide", "Show or hide the Flashback timeline UI.", Required("visible", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.GetAutomationManifest, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "manifest", "Get automation command manifest.");
        Set(entries, AutomationCommandKind.SetFullScreenEnabled, "{ enabled: bool }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "window fullscreen on|off", "Enter or exit full-screen mode.", Required("enabled", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.OpenRecordingsFolder, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "recordings open", "Open the current recordings output folder.");

        return entries.Values.ToArray();
    }

    private static AutomationCommandMetadata CreateDefault(AutomationCommandKind kind)
        => new(
            kind,
            kind.ToString(),
            "{}",
            Array.Empty<AutomationPayloadFieldMetadata>(),
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
        string mcpDescription,
        params AutomationPayloadFieldMetadata[] payloadFields)
        => entries[kind] = new AutomationCommandMetadata(
            kind,
            kind.ToString(),
            payloadShape,
            payloadFields,
            ready,
            timeoutMs,
            pathPolicy,
            cliHelp,
            mcpDescription);

    private static int DefaultTimeout => AutomationPipeProtocol.DefaultResponseTimeoutMs;

    private static AutomationPayloadFieldMetadata Required(string name, AutomationPayloadFieldType type)
        => new(name, type, Required: true);

    private static AutomationPayloadFieldMetadata Optional(string name, AutomationPayloadFieldType type)
        => new(name, type, Required: false);

    private static string Normalize(string value)
    {
        var buffer = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(buffer);
    }
}
