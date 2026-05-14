namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static Task<int> HandlePreviewAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "preview start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "preview start|stop");
        return HandleSimpleCommandAsync(
            context,
            "SetPreviewEnabled",
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("preview expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleRecordAsync(CommandContext context)
    {
        var action = RequireWord(context.Rest, 0, "record start|stop").ToLowerInvariant();
        EnsureArgCount(context.Rest, 1, "record start|stop");
        return HandleSimpleCommandAsync(
            context,
            "SetRecordingEnabled",
            new Dictionary<string, object?> { ["enabled"] = action switch { "start" => true, "stop" => false, _ => throw new UsageException("record expects start or stop.") } },
            includeData: false);
    }

    private static Task<int> HandleCaptureAsync(CommandContext context, string commandName, string defaultPath)
    {
        var outputPath = context.Rest.Count == 0 ? defaultPath : JoinRemaining(context.Rest, 0);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        return HandleSimpleCommandAsync(
            context,
            commandName,
            new Dictionary<string, object?> { ["outputPath"] = outputPath },
            includeData: true);
    }

    private static Task<int> HandleSetAsync(CommandContext context)
    {
        var subcommand = RequireWord(context.Rest, 0, "set <option> ...").ToLowerInvariant();
        return subcommand switch
        {
            "resolution" => SendSetValueAsync(context, "SetResolution", "resolution", JoinRemaining(context.Rest, 1), "set resolution <value>"),
            "fps" => SendSetValueAsync(context, "SetFrameRate", "frameRate", ParseDouble(RequireWord(context.Rest, 1, "set fps <value>")), "set fps <value>"),
            "format" => SendSetValueAsync(context, "SetRecordingFormat", "format", NormalizeRecordingFormat(JoinRemaining(context.Rest, 1)), "set format <value>"),
            "quality" => SendSetValueAsync(context, "SetQuality", "quality", JoinRemaining(context.Rest, 1), "set quality <value>"),
            "bitrate" => SendSetValueAsync(context, "SetCustomBitrate", "bitrateMbps", ParseDouble(RequireWord(context.Rest, 1, "set bitrate <value>")), "set bitrate <value>"),
            "preset" => SendSetValueAsync(context, "SetPreset", "preset", JoinRemaining(context.Rest, 1), "set preset <value>"),
            "split" => SendSetValueAsync(context, "SetSplitEncodeMode", "splitEncodeMode", JoinRemaining(context.Rest, 1), "set split <value>"),
            "video-format" => SendSetValueAsync(context, "SetVideoFormat", "videoFormat", JoinRemaining(context.Rest, 1), "set video-format <value>"),
            "decoders" => SendSetValueAsync(context, "SetMjpegDecoderCount", "decoderCount", ParseInt(RequireWord(context.Rest, 1, "set decoders <value>")), "set decoders <value>"),
            "hdr" => SendSetValueAsync(context, "SetHdrEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr on|off")), "set hdr on|off"),
            "hdr-preview" => SendSetValueAsync(context, "SetTrueHdrPreviewEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set hdr-preview on|off")), "set hdr-preview on|off"),
            "audio" => SendSetValueAsync(context, "SetAudioEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio on|off")), "set audio on|off"),
            "audio-preview" => SendSetValueAsync(context, "SetAudioPreviewEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set audio-preview on|off")), "set audio-preview on|off"),
            "volume" => SendSetValueAsync(context, "SetPreviewVolume", "previewVolumePercent", ParseDouble(RequireWord(context.Rest, 1, "set volume <value>")), "set volume <value>"),
            "audio-mode" => SendSetValueAsync(context, "SetDeviceAudioMode", "mode", RequireWord(context.Rest, 1, "set audio-mode hdmi|analog"), "set audio-mode hdmi|analog"),
            "gain" => SendSetValueAsync(context, "SetAnalogAudioGain", "gain", ParseDouble(RequireWord(context.Rest, 1, "set gain <value>")), "set gain <value>"),
            "output" => SendSetValueAsync(context, "SetOutputPath", "outputPath", JoinRemaining(context.Rest, 1), "set output <path>"),
            "show-all" => SendSetValueAsync(context, "SetShowAllCaptureOptions", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set show-all on|off")), "set show-all on|off"),
            "mic" or "microphone" => SendSetValueAsync(context, "SetMicrophoneEnabled", "enabled", ParseOnOff(RequireWord(context.Rest, 1, "set mic on|off")), "set mic on|off"),
            _ => throw new UsageException($"Unknown set command '{subcommand}'.")
        };
    }

}
