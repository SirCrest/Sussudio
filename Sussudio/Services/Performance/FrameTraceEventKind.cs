namespace Sussudio.Services.Performance;

/// <summary>
/// Stage that produced a frame-trace event. The numeric value is internal to the ring (append, never reuse; it
/// must fit 16 bits, the ring packs it beside a 16-bit arg); a dump always emits the stage name from
/// <see cref="FrameTraceEventKinds"/>, never the number. Stages that belong to one frame share the frame's arrival
/// QPC timestamp as the event id so an analyzer can join a frame from the read loop to the present.
/// </summary>
public enum FrameTraceEventKind
{
    /// <summary>Reserved. Markers are kept beside the ring and emitted from there; this value is never recorded by the core.</summary>
    Marker = 0,

    CaptureRead = 1,
    CaptureDeliver = 2,
    CaptureVisualScan = 3,

    MjpegQueueWait = 4,
    MjpegDecode = 5,
    MjpegPipeline = 6,

    PreviewJitterResidence = 7,
    PreviewQueueDwell = 8,
    PreviewFrameLatencyWait = 9,
    PreviewInputUpload = 10,
    PreviewRenderSubmit = 11,
    PreviewPresent = 12,
    PreviewRenderTotal = 13,
    PreviewGpuRender = 14,
    PreviewPipeline = 15,

    /// <summary>Instant event: a frame dropped by the renderer. The arg is the <see cref="PreviewDropReason"/> code; the dump also names it.</summary>
    PreviewDrop = 16,

    RecordingQueueDwell = 17,
    RecordingEncodeSubmit = 18,
    RecordingMuxWrite = 19,

    FlashbackQueueDwell = 20,
    FlashbackEncodeSubmit = 21,
    FlashbackMuxWrite = 22,
    FlashbackSegmentRotation = 23,
    FlashbackPlaybackDecode = 24,
    FlashbackPlaybackPresent = 25,

    AudioCapture = 26,
    AudioRender = 27,

    UiDispatcherLatency = 28,
    GcPause = 29,
    AutomationCommand = 30,

    /// <summary>
    /// Instant event at the Media Foundation sample time converted to QPC (only while the ingress clock is the QPC
    /// domain), with the frame's arrival timestamp as the id. Lets an analyzer compute ingress-to-visible exactly.
    /// </summary>
    CaptureSampleTime = 31,

    /// <summary>Diagnostics hub snapshot refresh span (id -1).</summary>
    HubRefresh = 32,
}

/// <summary>Stage names and categories used in trace dumps.</summary>
public static class FrameTraceEventKinds
{
    public static string Name(FrameTraceEventKind kind) => kind switch
    {
        FrameTraceEventKind.Marker => "marker",
        FrameTraceEventKind.CaptureRead => "capture.read",
        FrameTraceEventKind.CaptureDeliver => "capture.deliver",
        FrameTraceEventKind.CaptureVisualScan => "capture.visual_scan",
        FrameTraceEventKind.MjpegQueueWait => "mjpeg.queue_wait",
        FrameTraceEventKind.MjpegDecode => "mjpeg.decode",
        FrameTraceEventKind.MjpegPipeline => "mjpeg.pipeline",
        FrameTraceEventKind.PreviewJitterResidence => "preview.jitter_residence",
        FrameTraceEventKind.PreviewQueueDwell => "preview.queue_dwell",
        FrameTraceEventKind.PreviewFrameLatencyWait => "preview.frame_latency_wait",
        FrameTraceEventKind.PreviewInputUpload => "preview.input_upload",
        FrameTraceEventKind.PreviewRenderSubmit => "preview.render_submit",
        FrameTraceEventKind.PreviewPresent => "preview.present",
        FrameTraceEventKind.PreviewRenderTotal => "preview.render_total",
        FrameTraceEventKind.PreviewGpuRender => "preview.gpu_render",
        FrameTraceEventKind.PreviewPipeline => "preview.pipeline",
        FrameTraceEventKind.PreviewDrop => "preview.drop",
        FrameTraceEventKind.RecordingQueueDwell => "recording.queue_dwell",
        FrameTraceEventKind.RecordingEncodeSubmit => "recording.encode_submit",
        FrameTraceEventKind.RecordingMuxWrite => "recording.mux_write",
        FrameTraceEventKind.FlashbackQueueDwell => "flashback.queue_dwell",
        FrameTraceEventKind.FlashbackEncodeSubmit => "flashback.encode_submit",
        FrameTraceEventKind.FlashbackMuxWrite => "flashback.mux_write",
        FrameTraceEventKind.FlashbackSegmentRotation => "flashback.segment_rotation",
        FrameTraceEventKind.FlashbackPlaybackDecode => "flashback.playback_decode",
        FrameTraceEventKind.FlashbackPlaybackPresent => "flashback.playback_present",
        FrameTraceEventKind.AudioCapture => "audio.capture",
        FrameTraceEventKind.AudioRender => "audio.render",
        FrameTraceEventKind.UiDispatcherLatency => "ui.dispatcher_latency",
        FrameTraceEventKind.GcPause => "gc.pause",
        FrameTraceEventKind.AutomationCommand => "automation.command",
        FrameTraceEventKind.CaptureSampleTime => "capture.sample_time",
        FrameTraceEventKind.HubRefresh => "telemetry.hub_refresh",
        _ => "kind-" + (int)kind,
    };

    public static string Category(FrameTraceEventKind kind)
    {
        var name = Name(kind);
        var dot = name.IndexOf('.');
        return dot > 0 ? name.Substring(0, dot) : "perf";
    }
}
