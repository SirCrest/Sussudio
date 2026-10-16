using System;

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

    /// <summary>
    /// One PDH GPU counter call on the <c>telemetry.gpu_engines</c> sensor thread (id -1): arg 1 is the first open of the
    /// counters, arg 0 a sample read (the collect that expands the <c>GPU Engine</c> wildcard). It puts the sensor's own
    /// cost on the timeline so a stall elsewhere can be compared with it.
    /// </summary>
    GpuEngineSample = 33,
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
        FrameTraceEventKind.GpuEngineSample => "telemetry.gpu_engines",
        _ => "kind-" + (int)kind,
    };

    // A dump asks for the category of every event, so the declared kinds' categories are derived once instead of cut
    // out of the name (a new string) each time.
    private static readonly string[] CategoryByKind = BuildCategories();

    public static string Category(FrameTraceEventKind kind) =>
        (uint)kind < (uint)CategoryByKind.Length ? CategoryByKind[(int)kind] : CategoryOf(Name(kind));

    private static string[] BuildCategories()
    {
        var highest = 0;
        foreach (var kind in Enum.GetValues<FrameTraceEventKind>())
        {
            highest = Math.Max(highest, (int)kind);
        }

        var categories = new string[highest + 1];
        for (var value = 0; value < categories.Length; value++)
        {
            categories[value] = CategoryOf(Name((FrameTraceEventKind)value));
        }

        return categories;
    }

    private static string CategoryOf(string name)
    {
        var dot = name.IndexOf('.');
        return dot > 0 ? name.Substring(0, dot) : "perf";
    }
}
