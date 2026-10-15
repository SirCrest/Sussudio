namespace Sussudio.Services.Performance;

/// <summary>
/// Process-wide catalog of every performance metric. Each metric is a static field with a stable dotted name
/// (wire contract: append, never rename), a unit and a one-line description. Hot paths record straight into the
/// field, for example <c>PerfMetrics.PreviewPresentCall.RecordElapsedSince(start)</c>; nothing here allocates or locks
/// at record time. Time histograms record microseconds and are reported in milliseconds.
/// Contract: docs/architecture/performance-telemetry.md section 2.2.
/// </summary>
public static class PerfMetrics
{
    /// <summary>The registry every field below is registered in; reports and windows enumerate it.</summary>
    public static readonly PerfRegistry Registry = new();

    // capture
    public static readonly PerfHistogram CaptureIngressLag = Time("capture.ingress_lag", "read loop: now minus MF sample time, only when the sample clock is the QPC domain");
    public static readonly PerfHistogram CaptureReadSampleWait = Time("capture.readsample_wait", "read loop: time blocked in ReadSample");
    public static readonly PerfHistogram CaptureArrivalInterval = Time("capture.arrival_interval", "read loop: QPC interval between successful reads");
    public static readonly PerfHistogram CaptureDeliver = Time("capture.deliver", "read loop: DeliverFrame duration (all consumer fan-out on the read thread)");
    public static readonly PerfHistogram CaptureVisualScan = Time("capture.visual_scan", "luma visual-cadence scan duration");

    // mjpeg
    public static readonly PerfHistogram MjpegDecode = Time("mjpeg.decode", "decode worker, per frame");
    public static readonly PerfHistogram MjpegQueueWait = Time("mjpeg.queue_wait", "compressed enqueue to worker dequeue");
    public static readonly PerfHistogram MjpegPipeline = Time("mjpeg.pipeline", "arrival to strict-order emit");

    // preview
    public static readonly PerfHistogram PreviewJitterResidence = Time("preview.jitter_residence", "jitter buffer enqueue to submit to the renderer");
    public static readonly PerfHistogram PreviewQueueDwell = Time("preview.queue_dwell", "renderer submit until the render thread selects the frame for rendering (frames skipped as stale are dropped, not recorded)");
    public static readonly PerfHistogram PreviewFrameLatencyWait = Time("preview.frame_latency_wait", "waitable swap-chain wait");
    public static readonly PerfHistogram PreviewInputUpload = Time("preview.input_upload", "render CPU stage: input upload");
    public static readonly PerfHistogram PreviewRenderSubmit = Time("preview.render_submit", "render CPU stage: render submit");
    public static readonly PerfHistogram PreviewPresentCall = Time("preview.present_call", "Present call duration");
    public static readonly PerfHistogram PreviewRenderTotal = Time("preview.render_total", "total render CPU per frame");
    public static readonly PerfHistogram PreviewGpuRender = Time("preview.gpu_render", "D3D11 timestamp queries around the render passes (when GPU timing is enabled)");
    public static readonly PerfHistogram PreviewPresentInterval = Time("preview.present_interval", "present-to-present on cadence frames");
    public static readonly PerfHistogram PreviewPipelineLatency = Time("preview.pipeline_latency", "arrival to estimated visible (live frames only; Flashback playback frames are excluded)");
    public static readonly PerfHistogram PreviewIngressToVisible = Time("preview.ingress_to_visible", "MF sample time to estimated visible (QPC domain only)");

    // recording
    public static readonly PerfHistogram RecordingQueueDwell = Time("recording.queue_dwell", "video packet enqueue to encode-loop dequeue, all lanes (CPU, D3D11, CUDA), QPC");
    public static readonly PerfHistogram RecordingEncodeSubmit = Time("recording.encode_submit", "avcodec_send_frame plus the receive/drain loop, including the nested mux_write time");
    public static readonly PerfHistogram RecordingMuxWrite = Time("recording.mux_write", "video-packet av_interleaved_write_frame including I/O (nested inside encode_submit; audio packets are not recorded)");

    // flashback
    public static readonly PerfHistogram FlashbackQueueDwell = Time("flashback.queue_dwell", "video packet enqueue to encode-loop dequeue, QPC");
    public static readonly PerfHistogram FlashbackEncodeSubmit = Time("flashback.encode_submit", "avcodec_send_frame plus the receive/drain loop, including the nested mux_write time");
    public static readonly PerfHistogram FlashbackMuxWrite = Time("flashback.mux_write", "video-packet av_interleaved_write_frame including I/O (nested inside encode_submit; audio packets are not recorded)");
    public static readonly PerfHistogram FlashbackSegmentRotation = Time("flashback.segment_rotation", "RotateSegment total, success or failure (flush, trailer, new segment open, buffer bookkeeping)");
    public static readonly PerfHistogram FlashbackPlaybackDecode = Time("flashback.playback_decode", "steady-state continuous playback: hardware read-ahead plus next-frame read per paced iteration; seek, scrub, step and pause-forward decodes are not recorded");
    public static readonly PerfHistogram FlashbackPlaybackPresentInterval = Time("flashback.playback_present_interval", "interval between paced presents on the playback thread during continuous playback");

    // audio
    public static readonly PerfHistogram AudioCaptureInterval = Time("audio.capture_interval", "WASAPI capture event interval, QPC");
    public static readonly PerfHistogram AudioCaptureDrain = Time("audio.capture_drain", "capture packet drain duration");
    public static readonly PerfHistogram AudioRenderInterval = Time("audio.render_interval", "WASAPI render event interval, QPC");
    public static readonly PerfHistogram AudioRenderQueueMs = Registry.Histogram("audio.render_queue_ms", PerfUnit.Milliseconds, "monitor queue depth in milliseconds, sampled per render event");

    // ui, runtime, gpu
    public static readonly PerfHistogram UiDispatcherLatency = Time("ui.dispatcher_latency", "UI responsiveness probe: dispatcher enqueue to run");
    public static readonly PerfHistogram GcPause = Time("gc.pause", "each GC suspension, from the runtime event listener");
    public static readonly PerfHistogram GpuEngine3d = Percent("gpu.engine.3d", "PDH GPU Engine 3D utilization for this process, 1 Hz while sensors run");
    public static readonly PerfHistogram GpuEngineVideoEncode = Percent("gpu.engine.video_encode", "PDH GPU Engine video encode utilization for this process, 1 Hz while sensors run");
    public static readonly PerfHistogram GpuEngineVideoDecode = Percent("gpu.engine.video_decode", "PDH GPU Engine video decode utilization for this process, 1 Hz while sensors run");
    public static readonly PerfHistogram GpuEngineCopy = Percent("gpu.engine.copy", "PDH GPU Engine copy utilization for this process, 1 Hz while sensors run");
    public static readonly PerfHistogram GpuNvmlGpuUtil = Percent("gpu.nvml.gpu_util", "NVML GPU utilization at each NVML poll");
    public static readonly PerfHistogram GpuNvmlEncoderUtil = Percent("gpu.nvml.encoder_util", "NVML encoder utilization at each NVML poll");
    public static readonly PerfHistogram GpuNvmlDecoderUtil = Percent("gpu.nvml.decoder_util", "NVML decoder utilization at each NVML poll");

    // telemetry and automation self-cost
    public static readonly PerfHistogram TelemetryHubRefresh = Time("telemetry.hub_refresh", "diagnostics hub snapshot refresh");
    public static readonly PerfHistogram TelemetryHubAlloc = Registry.Histogram("telemetry.hub_alloc", PerfUnit.Bytes, "process-wide allocated bytes during one hub refresh (an upper bound: other threads allocate meanwhile)");
    public static readonly PerfHistogram AutomationCommand = Time("automation.command", "automation command dispatch");

    // counters
    /// <summary><c>preview.drop.&lt;reason&gt;</c>: renderer drops by reason (fixed table, see <see cref="PreviewDropReasons"/>).</summary>
    public static readonly PerfReasonCounters PreviewDrops =
        PerfReasonCounters.Create(Registry, "preview.drop.", "renderer drops with reason", PreviewDropReasons.CounterNames);

    /// <summary><c>preview.jitter_drop.&lt;reason&gt;</c>: MJPEG jitter-buffer drops by reason.</summary>
    public static readonly PerfReasonCounters PreviewJitterDrops =
        PerfReasonCounters.Create(Registry, "preview.jitter_drop.", "jitter buffer drops with reason", JitterReasons.DropCounterNames);

    /// <summary><c>preview.jitter_underflow.&lt;reason&gt;</c>: jitter-buffer emit misses by reason.</summary>
    public static readonly PerfReasonCounters PreviewJitterUnderflows =
        PerfReasonCounters.Create(Registry, "preview.jitter_underflow.", "jitter buffer underflows with reason", JitterReasons.UnderflowCounterNames);

    public static readonly PerfCounter CaptureReadErrors = Registry.Counter("capture.read_errors", "read-loop exceptions");
    public static readonly PerfCounter UiStall50Ms = Registry.Counter("ui.stall_50ms", "UI probe waits over 50 ms");
    public static readonly PerfCounter AudioRenderUnderrun = Registry.Counter("audio.render_underrun", "render events with an empty queue");

    /// <summary>
    /// Counter <c>preview.drop.&lt;reason&gt;</c> for a renderer drop reason string. The reason maps into the fixed
    /// <see cref="PreviewDropReasons"/> table (unknown strings land in <c>other</c>), so the set stays bounded.
    /// Hot paths should classify once and call <c>PreviewDrops.Increment</c> with the code.
    /// </summary>
    public static PerfCounter PreviewDrop(string reason) =>
        PreviewDrops.Counter((int)PreviewDropReasons.Classify(reason));

    private static PerfHistogram Time(string name, string description) =>
        Registry.Histogram(name, PerfUnit.Microseconds, description);

    private static PerfHistogram Percent(string name, string description) =>
        Registry.Histogram(name, PerfUnit.Percent, description);
}
