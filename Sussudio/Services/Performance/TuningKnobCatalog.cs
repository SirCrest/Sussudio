using System.Collections.Generic;

namespace Sussudio.Services.Performance;

/// <summary>
/// Every <c>SUSSUDIO_*</c> environment variable the app reads, with its type, default, range, when it is read
/// (<see cref="TuningKnobApply"/>), the file that reads it, and what it does. Defaults and ranges mirror the read
/// sites exactly (the readers clamp; <see cref="TuningKnobStore"/> rejects instead). To add a knob: add the read site,
/// then one row here with the same default and range, and the apply scope the read site's lifetime implies.
/// Variables that cannot be tuned safely at run time are listed in <see cref="Excluded"/> with the reason.
/// </summary>
public static class TuningKnobCatalog
{
    private const string Renderer = "Sussudio/Services/Preview/D3D11PreviewRenderer.cs";
    private const string Jitter = "Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs";
    private const string MjpegPipeline = "Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs";
    private const string UnifiedCapture = "Sussudio/Services/Capture/UnifiedVideoCapture.cs";
    private const string ReadLoop = "Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs";
    private const string FlashbackPlayback = "Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs";
    private const string Hub = "Sussudio/Services/Automation/AutomationDiagnosticsHub.cs";
    private const string GpuTiming = "Sussudio/Services/Preview/D3D11PreviewRenderer.GpuTiming.cs";
    private const string Sensors = "Sussudio/Services/Performance/TelemetrySensors.cs";

    /// <summary>MMCSS task names that exist under HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks on a default install.</summary>
    private static readonly string[] MmcssTasks =
    {
        "Audio", "Capture", "Distribution", "Games", "Low Latency", "Playback", "Pro Audio", "Window Manager",
    };

    /// <summary>The values of every <c>auto|on|off</c> telemetry knob (see <see cref="TelemetrySensorModes"/>).</summary>
    private static readonly string[] AutoOnOff = { "auto", "on", "off" };

    public static IReadOnlyList<TuningKnobDefinition> Knobs { get; } = new[]
    {
        // Preview renderer: read in the D3D11PreviewRenderer constructor, which runs on every preview start.
        Int("SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL", 1, 0, 1, TuningKnobApply.PreviewRestart, Renderer,
            "DXGI Present sync interval: 1 waits for vblank, 0 presents immediately."),
        Int("SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY", 1, 1, 3, TuningKnobApply.PreviewRestart, Renderer,
            "Waitable swap chain maximum frame latency (frames queued ahead of the display)."),
        Int("SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT", 2, 2, 4, TuningKnobApply.PreviewRestart, Renderer,
            "Swap chain back-buffer count."),
        Int("SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH", 4, 1, 8, TuningKnobApply.PreviewRestart, Renderer,
            "Frames the renderer may hold pending before the oldest are dropped."),
        Bool("SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN", true, TuningKnobApply.PreviewRestart, Renderer,
            "Wait on the swap chain's frame-latency waitable handle before rendering."),
        Bool("SUSSUDIO_PREVIEW_INPUT_VIEW_CACHE", true, TuningKnobApply.PreviewRestart, Renderer,
            "Cache shader resource views for external (decoder) input textures."),
        Bool("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS", true, TuningKnobApply.PreviewRestart, Renderer,
            "Collect DXGI present statistics for slip and cadence diagnostics."),
        Int("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL", 2, 1, 120, TuningKnobApply.PreviewRestart, Renderer,
            "Sample DXGI frame statistics every N frames."),
        Bool("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_DWM_FLUSH", false, TuningKnobApply.PreviewRestart, Renderer,
            "Flush DWM before reading DXGI frame statistics (diagnostic; adds cost)."),
        Bool("SUSSUDIO_PREVIEW_COMPOSITION_MODE_PROBE", false, TuningKnobApply.PreviewRestart, Renderer,
            "Probe the DWM composition mode every 120 frames."),
        Dbl("SUSSUDIO_PREVIEW_SLOW_FRAME_THRESHOLD_MS", "0", 0, 1000, TuningKnobApply.PreviewRestart, Renderer,
            "Log slow-frame diagnostics above this many ms; 0 derives the threshold from the expected frame interval."),
        Bool("SUSSUDIO_PREVIEW_MEDIA_PRESENT_DURATION", false, TuningKnobApply.PreviewRestart, Renderer,
            "Configure the swap chain's media present duration at creation."),
        Bool("SUSSUDIO_PREVIEW_RENDER_STALE_DROP", true, TuningKnobApply.PreviewRestart, Renderer,
            "Drop frames that went stale before the render thread reached them."),
        Int("SUSSUDIO_PREVIEW_INPUT_TEXTURE_RING", 3, 1, 4, TuningKnobApply.PreviewRestart, Renderer,
            "Size of the input texture ring used for CPU-uploaded frames."),
        Mmcss("SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK", "Playback", TuningKnobApply.PreviewRestart, Renderer,
            "MMCSS task name registered by the preview render thread."),
        Int("SUSSUDIO_PREVIEW_RENDER_MMCSS_PRIORITY", 1, -2, 2, TuningKnobApply.PreviewRestart, Renderer,
            "MMCSS thread priority (-2 very low to 2 critical) for the preview render thread."),
        Int("SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS", 1000, 100, 10000, TuningKnobApply.PreviewRestart, Renderer,
            "How long renderer stop waits for in-flight native calls before giving up."),
        Int("SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS", 3000, 500, 30000, TuningKnobApply.PreviewRestart, Renderer,
            "How long renderer stop waits for the render thread to exit."),

        // MJPEG preview jitter buffer: constructed per preview start.
        Mmcss("SUSSUDIO_PREVIEW_JITTER_MMCSS_TASK", "Playback", TuningKnobApply.PreviewRestart, Jitter,
            "MMCSS task name registered by the MJPEG preview jitter-buffer emit thread."),
        Int("SUSSUDIO_PREVIEW_JITTER_MMCSS_PRIORITY", 1, -2, 2, TuningKnobApply.PreviewRestart, Jitter,
            "MMCSS thread priority for the jitter-buffer emit thread."),
        Bool("SUSSUDIO_PREVIEW_DISPLAY_CLOCK_PACING", false, TuningKnobApply.PreviewRestart, Jitter,
            "Pace jitter-buffer submits against the display's vblank clock instead of the source cadence."),
        Dbl("SUSSUDIO_PREVIEW_DISPLAY_CLOCK_SUBMIT_DELAY_MS", "0.25", 0, 4, TuningKnobApply.PreviewRestart, Jitter,
            "Delay after a vblank before display-clock pacing submits a frame."),
        Dbl("SUSSUDIO_PREVIEW_DISPLAY_CLOCK_MIN_LEAD_MS", "2", 0.25, 6, TuningKnobApply.PreviewRestart, Jitter,
            "Minimum lead before a vblank for display-clock pacing to target it."),
        Int("SUSSUDIO_PREVIEW_JITTER_MIN_TARGET_DEPTH", 2, 1, 60, TuningKnobApply.PreviewRestart, Jitter,
            "Lowest target queue depth the adaptive jitter buffer may shrink to."),
        Int("SUSSUDIO_PREVIEW_JITTER_MAX_TARGET_DEPTH", 8, 1, 60, TuningKnobApply.PreviewRestart, Jitter,
            "Highest target queue depth the adaptive jitter buffer may grow to."),
        Int("SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH", 3, 1, 60, TuningKnobApply.PreviewRestart, Jitter,
            "Initial jitter-buffer target depth in frames (clamped to the adaptive min/max)."),
        Int("SUSSUDIO_PREVIEW_JITTER_MAX_DEPTH", 12, 2, 90, TuningKnobApply.PreviewRestart, Jitter,
            "Hard queue depth limit of the jitter buffer (the reader raises it to at least target depth + 1)."),

        // MJPEG decode pipeline: constructed per capture start.
        Bool("SUSSUDIO_MJPEG_PREVIEW_EARLY_FORK", true, TuningKnobApply.PreviewRestart, MjpegPipeline,
            "Fork decoded frames to preview before strict-order reorder."),
        Bool("SUSSUDIO_MJPEG_LOAD_SHED", true, TuningKnobApply.PreviewRestart, MjpegPipeline,
            "Shed old compressed frames under decode overload when no strict consumer needs the full sequence."),
        Mmcss("SUSSUDIO_MJPEG_DECODE_MMCSS_TASK", "Playback", TuningKnobApply.PreviewRestart, MjpegPipeline,
            "MMCSS task name registered by MJPEG decode workers."),
        Int("SUSSUDIO_MJPEG_DECODE_MMCSS_PRIORITY", 0, -2, 2, TuningKnobApply.PreviewRestart, MjpegPipeline,
            "MMCSS thread priority for MJPEG decode workers."),
        Int("SUSSUDIO_MJPEG_COMPRESSED_BUDGET_MB", 0, 0, 4096, TuningKnobApply.PreviewRestart, MjpegPipeline,
            "Compressed-queue byte budget in MB; 0 keeps the built-in 64 MB."),
        Int("SUSSUDIO_MJPEG_REORDER_SLOTS", 0, 0, 240, TuningKnobApply.PreviewRestart, MjpegPipeline,
            "Decoded-frame reorder window in slots; 0 keeps the built-in 16."),

        // Capture: UnifiedVideoCapture and the MF source reader are created per preview (and per recording-owned capture).
        Bool("SUSSUDIO_CAPTURE_POOLED_FANOUT", true, TuningKnobApply.PreviewRestart, UnifiedCapture,
            "Fan CPU frames out through pooled buffers instead of per-consumer copies. Also read when a recording-owned capture starts."),
        new TuningKnobDefinition(
            "SUSSUDIO_MJPEG_GPU_NATIVE_DECODE", TuningKnobType.Bool, "0 above 60 fps, otherwise 1", 0, 1, null,
            TuningKnobApply.PreviewRestart, UnifiedCapture,
            "Prefer Media Foundation's D3D-backed MJPEG decode over the parallel software pipeline."),
        Mmcss("SUSSUDIO_CAPTURE_READLOOP_MMCSS_TASK", "Capture", TuningKnobApply.PreviewRestart, ReadLoop,
            "MMCSS task name registered by the source-reader read loop."),
        Int("SUSSUDIO_CAPTURE_READLOOP_MMCSS_PRIORITY", 1, -2, 2, TuningKnobApply.PreviewRestart, ReadLoop,
            "MMCSS thread priority for the source-reader read loop."),

        // Flashback playback: the controller is created with the Flashback backend.
        Mmcss("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK", "Playback", TuningKnobApply.FlashbackRestart, FlashbackPlayback,
            "MMCSS task name registered by the Flashback playback thread."),
        Int("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY", 1, -2, 2, TuningKnobApply.FlashbackRestart, FlashbackPlayback,
            "MMCSS thread priority for the Flashback playback thread."),

        // HDR: consulted when preview, reinit, recording or Flashback start (and by health snapshots, which compare two
        // settings under the same value). Those parts share the capture's pixel format: a preview start with no recording
        // rebuilds the capture and Flashback when it changes, and a recording or Flashback started on a capture of the
        // other format refuses to run. The override is loud: the policy logs that HDR was requested but forced off,
        // never a silent fallback.
        new TuningKnobDefinition(
            "SUSSUDIO_HDR_OUTPUT_FORCE_OFF", TuningKnobType.Bool, "0", 0, 1, null,
            TuningKnobApply.PipelineRestart, "Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs",
            "Force the HDR pipeline off even when HDR10 output is requested (logged at each decision)."),

        // Automation and diagnostics.
        Int("SUSSUDIO_DIAGNOSTICS_POLL_MS", 500, 100, 10000, TuningKnobApply.Live, Hub,
            "Diagnostics hub poll interval; also bounds how fresh cached snapshots are."),
        Dbl("SUSSUDIO_PERF_CAPTURE_DROP_PCT", "0.1", 0, 50, TuningKnobApply.AppRestart, Hub,
            "Estimated capture drop percent above which the performance score is penalized."),
        Dbl("SUSSUDIO_PERF_CAPTURE_P95_MULT", "1.3", 1, 10, TuningKnobApply.AppRestart, Hub,
            "Capture p95 interval ratio (to nominal) above which the performance score is penalized."),
        Dbl("SUSSUDIO_PERF_PREVIEW_SLOW_PCT", "2", 0, 100, TuningKnobApply.AppRestart, Hub,
            "Slow display-cadence frame percent above which the performance score is penalized."),
        Dbl("SUSSUDIO_PERF_VERIFY_DROP_PCT", "0.25", 0, 100, TuningKnobApply.AppRestart, Hub,
            "Recording-verification cadence drop percent above which the performance score is penalized."),
        Int("SUSSUDIO_AUTOMATION_REQUEST_TIMEOUT_MS", 300000, 1000, 300000, TuningKnobApply.AppRestart,
            "Sussudio/Services/Automation/NamedPipeAutomationServer.cs",
            "Per-request timeout of the automation pipe server."),
        Int("SUSSUDIO_PREVIEW_START_TIMEOUT_MS", 10000, 1000, 15000, TuningKnobApply.AppRestart,
            "Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs",
            "Preview startup first-visual watchdog; read once on the first preview start of the process."),

        // Telemetry sensors: auto follows demand (an open perf window or a running frame trace). Both are read at most
        // once a second by their owners and re-evaluated at once when a window, a trace or a knob changes, so they are live.
        AutoOnOffKnob(TelemetrySensorModes.SensorsKnob, Sensors,
            "Demand-driven sensors (UI responsiveness probe, GPU engine counters): auto runs them while a perf window is open or a frame trace runs, on forces them on, off forces them off."),
        AutoOnOffKnob(TelemetrySensorModes.GpuTimingKnob, GpuTiming,
            "D3D11 GPU timestamp queries around the preview render passes: auto follows the sensor demand, on always measures, off never creates queries. Ignored on the WARP software adapter."),

        // Shutdown timeouts: read each time the shutdown path runs, so a change applies to the next shutdown.
        Int("SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS", 30000, 1000, 300000, TuningKnobApply.Live,
            "Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs",
            "Overall ViewModel dispose timeout (read at shutdown)."),
        Int("SUSSUDIO_CAPTURE_SERVICE_DISPOSE_TIMEOUT_MS", 30000, 1000, 300000, TuningKnobApply.Live,
            "Sussudio/Services/Capture/CaptureService.cs",
            "Capture service synchronous dispose timeout (read at shutdown)."),
        Int("SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS", 15000, 1000, 300000, TuningKnobApply.Live,
            "Sussudio/Services/Capture/CaptureSessionCoordinator.cs",
            "Session coordinator worker drain timeout at dispose (read at shutdown)."),
        Int("SUSSUDIO_COORDINATOR_DISPOSE_CANCEL_TIMEOUT_MS", 1000, 100, 300000, TuningKnobApply.Live,
            "Sussudio/Services/Capture/CaptureSessionCoordinator.cs",
            "Session coordinator worker cancellation wait at dispose (read at shutdown)."),
    };

    /// <summary>Variables the app reads that are deliberately not settable through the tool, with the reason.</summary>
    public static IReadOnlyList<TuningKnobExclusion> Excluded { get; } = new[]
    {
        new TuningKnobExclusion(
            "SUSSUDIO_LOG_ROOT",
            "Log directory path read once by the logger before any command can run; not a tuning value."),
        new TuningKnobExclusion(
            "SUSSUDIO_AUTOMATION_PIPE",
            "Names the automation pipe this process already serves; changing it cannot move the live pipe."),
        new TuningKnobExclusion(
            "SUSSUDIO_AUTOMATION_TOKEN",
            "Automation secret; never listed or settable through the tool."),
        new TuningKnobExclusion(
            "SUSSUDIO_CAPTURE_FAULT_PREVIEW_START_FAILURES",
            "Fault injection for the preview start failure live check; it deliberately fails video preview starts."),
        new TuningKnobExclusion(
            "SUSSUDIO_RECOVERY_DIRECTORY",
            "Recording recovery directory path; changing it at run time could orphan recovery markers."),
        new TuningKnobExclusion(
            "SUSSUDIO_PREVIEW_FAULT_WEDGE_RENDERER_START",
            "Fault injection for the wedged-renderer live check; it deliberately holds a preview render thread."),
    };

    private static TuningKnobDefinition Int(string name, int defaultValue, int min, int max, TuningKnobApply apply, string owner, string description) =>
        new(name, TuningKnobType.Int, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture), min, max, null, apply, owner, description);

    private static TuningKnobDefinition Dbl(string name, string defaultText, double min, double max, TuningKnobApply apply, string owner, string description) =>
        new(name, TuningKnobType.Double, defaultText, min, max, null, apply, owner, description);

    private static TuningKnobDefinition Bool(string name, bool defaultOn, TuningKnobApply apply, string owner, string description) =>
        new(name, TuningKnobType.Bool, defaultOn ? "1" : "0", 0, 1, null, apply, owner, description);

    private static TuningKnobDefinition AutoOnOffKnob(string name, string owner, string description) =>
        new(name, TuningKnobType.Choice, "auto", null, null, AutoOnOff, TuningKnobApply.Live, owner, description);

    private static TuningKnobDefinition Mmcss(string name, string defaultTask, TuningKnobApply apply, string owner, string description) =>
        new(name, TuningKnobType.Choice, defaultTask, null, null, MmcssTasks, apply, owner, description);
}
