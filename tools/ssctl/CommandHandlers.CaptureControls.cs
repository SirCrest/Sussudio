using Sussudio.Models;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static Task<int> HandlePreviewAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "preview start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "preview start|stop");
        return HandleSimpleCommandAsync(
            context,
            AutomationCommandKind.SetPreviewEnabled,
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("preview expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleRecordAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "record start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "record start|stop");
        return HandleSimpleCommandAsync(
            context,
            AutomationCommandKind.SetRecordingEnabled,
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("record expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleCaptureAsync(CommandContext context, AutomationCommandKind kind, string defaultPath)
    {
        var outputPath = context.Rest.Count == 0 ? defaultPath : JoinRemaining(context.Rest, 0);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        return HandleSimpleCommandAsync(
            context,
            kind,
            new Dictionary<string, object?> { ["outputPath"] = outputPath },
            includeData: true);
    }

    private static Task<int> HandleSetAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "set <option> ...").ToLowerInvariant();
        return subcommand switch
        {
            "resolution" => SendSetValueAsync(context, AutomationCommandKind.SetResolution, "resolution", JoinRemaining(context.Rest, 1), "set resolution <value>"),
            "fps" => SendSetValueAsync(context, AutomationCommandKind.SetFrameRate, "frameRate", ParseDouble(RequireWord(context.Rest, 1, "set fps <value>")), "set fps <value>"),
            "format" => SendSetValueAsync(context, AutomationCommandKind.SetRecordingFormat, "format", NormalizeRecordingFormat(JoinRemaining(context.Rest, 1)), "set format <value>"),
            "quality" => SendSetValueAsync(context, AutomationCommandKind.SetQuality, "quality", JoinRemaining(context.Rest, 1), "set quality <value>"),
            "bitrate" => SendSetValueAsync(context, AutomationCommandKind.SetCustomBitrate, "bitrateMbps", ParseDouble(RequireWord(context.Rest, 1, "set bitrate <value>")), "set bitrate <value>"),
            "preset" => SendSetValueAsync(context, AutomationCommandKind.SetPreset, "preset", JoinRemaining(context.Rest, 1), "set preset <value>"),
            "split" => SendSetValueAsync(context, AutomationCommandKind.SetSplitEncodeMode, "splitEncodeMode", JoinRemaining(context.Rest, 1), "set split <value>"),
            "video-format" => SendSetValueAsync(context, AutomationCommandKind.SetVideoFormat, "videoFormat", JoinRemaining(context.Rest, 1), "set video-format <value>"),
            "decoders" => SendSetValueAsync(context, AutomationCommandKind.SetMjpegDecoderCount, "decoderCount", ParseInt(RequireWord(context.Rest, 1, "set decoders <value>")), "set decoders <value>"),
            "hdr" => SendSetValueAsync(context, AutomationCommandKind.SetHdrEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr on|off")), "set hdr on|off"),
            "hdr-preview" => SendSetValueAsync(context, AutomationCommandKind.SetTrueHdrPreviewEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr-preview on|off")), "set hdr-preview on|off"),
            "audio" => SendSetValueAsync(context, AutomationCommandKind.SetAudioEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio on|off")), "set audio on|off"),
            "audio-preview" => SendSetValueAsync(context, AutomationCommandKind.SetAudioPreviewEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio-preview on|off")), "set audio-preview on|off"),
            "volume" => SendSetValueAsync(context, AutomationCommandKind.SetPreviewVolume, "previewVolumePercent", ParseDouble(RequireWord(context.Rest, 1, "set volume <value>")), "set volume <value>"),
            "audio-mode" => SendSetValueAsync(context, AutomationCommandKind.SetDeviceAudioMode, "mode", RequireWord(context.Rest, 1, "set audio-mode hdmi|analog"), "set audio-mode hdmi|analog"),
            "gain" => SendSetValueAsync(context, AutomationCommandKind.SetAnalogAudioGain, "gain", ParseDouble(RequireWord(context.Rest, 1, "set gain <value>")), "set gain <value>"),
            "output" => SendSetValueAsync(context, AutomationCommandKind.SetOutputPath, "outputPath", JoinRemaining(context.Rest, 1), "set output <path>"),
            "show-all" => SendSetValueAsync(context, AutomationCommandKind.SetShowAllCaptureOptions, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set show-all on|off")), "set show-all on|off"),
            "mic" or "microphone" => SendSetValueAsync(context, AutomationCommandKind.SetMicrophoneEnabled, "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set mic on|off")), "set mic on|off"),
            _ => throw new UsageException($"Unknown set command '{subcommand}'.")
        };
    }

    private static Task<int> SendSetValueAsync(
        CommandContext context,
        AutomationCommandKind kind,
        string propertyName,
        object value,
        string usage)
    {
        if (context.Rest.Count < 2)
        {
            throw new UsageException(usage);
        }

        return HandleSimpleCommandAsync(
            context,
            kind,
            new Dictionary<string, object?> { [propertyName] = value },
            includeData: false);
    }
}
