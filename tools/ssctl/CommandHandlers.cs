using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// CLI command layer over the shared automation pipe. Handlers keep console
// parsing, command payload shape, and human-readable output together while
// leaving transport details in AutomationPipeClient.
internal static partial class CommandHandlers
{
    public static Task<int> ExecuteAsync(PipeTransport transport, IReadOnlyList<string> arguments, bool globalJson)
    {
        var context = new CommandContext(transport, arguments, globalJson);
        return arguments[0].ToLowerInvariant() switch
        {
            "state" => HandleStateAsync(context),
            "diagnostics" => HandleDiagnosticsAsync(context),
            "options" => HandleOptionsAsync(context),
            "manifest" => HandleManifestAsync(context),
            "timeline" => HandleTimelineAsync(context),
            "memory" => HandleMemoryAsync(context),
            "audio-ramp-trace" => HandleAudioRampTraceAsync(context),
            "presentmon" => HandlePresentMonAsync(context),
            "diagnostic-session" or "session" => HandleDiagnosticSessionAsync(context),
            "preview" => HandlePreviewAsync(context),
            "record" => HandleRecordAsync(context),
            "screenshot" => HandleCaptureAsync(context, AutomationCommandKind.CaptureWindowScreenshot, "temp/window_screenshot.png"),
            "frame" => HandleCaptureAsync(context, AutomationCommandKind.CapturePreviewFrame, "temp/preview_capture.bmp"),
            "recordings" => HandleRecordingsAsync(context),
            "set" => HandleSetAsync(context),
            "device" => HandleDeviceAsync(context),
            "window" => HandleWindowAsync(context),
            "wait" => HandleWaitAsync(context),
            "verify" => HandleVerifyAsync(context),
            "assert" => HandleAssertAsync(context),
            "probe" => HandleProbeAsync(context),
            "stats" => HandleStatsAsync(context),
            "frametime" or "frame-time" => HandleFrameTimeAsync(context),
            "settings" => HandleSettingsAsync(context),
            "flashback" => HandleFlashbackAsync(context),
            _ => throw new UsageException($"Unknown command '{arguments[0]}'.")
        };
    }

    private sealed class CommandContext
    {
        public CommandContext(PipeTransport transport, IReadOnlyList<string> arguments, bool globalJson)
        {
            Transport = transport;
            GlobalJson = globalJson;
            Rest = arguments.Skip(1).ToList();
        }

        public PipeTransport Transport { get; }
        public bool GlobalJson { get; }
        public List<string> Rest { get; }
    }
}
