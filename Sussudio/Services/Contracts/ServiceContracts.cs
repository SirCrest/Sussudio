using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Contracts
{
    // Window operations that automation can request without reaching into WinUI
    // implementation details.
    public interface IAutomationWindowControl
    {
        Task MinimizeAsync(CancellationToken cancellationToken = default);
        Task MaximizeAsync(CancellationToken cancellationToken = default);
        Task RestoreAsync(CancellationToken cancellationToken = default);
        Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
        Task OpenRecordingsFolderAsync(CancellationToken cancellationToken = default);
        Task CloseAsync(CancellationToken cancellationToken = default);
        Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default);
        Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default);
        Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default);
        Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(string outputPath, CancellationToken cancellationToken = default);
    }

    // Diagnostics facade consumed by the command dispatcher and MCP/ssctl tools.
    public interface IAutomationDiagnosticsHub : IDisposable, IAsyncDisposable
    {
        AutomationSnapshot GetLatestSnapshot();
        Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default);
        IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline(int maxEntries = 240);
        IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100);
        Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default);
        Task<RecordingVerificationResult> VerifyFileAsync(
            string filePath,
            string? verificationProfile = null,
            CancellationToken cancellationToken = default);
        void Start();
        Task StopAsync(CancellationToken cancellationToken = default);
        event EventHandler<AutomationSnapshot>? SnapshotUpdated;
    }

    // Executes one authenticated automation request and returns the protocol DTO.
    public interface IAutomationCommandDispatcher
    {
        Task<AutomationCommandResponse> ExecuteAsync(
            AutomationCommandRequest request,
            CancellationToken cancellationToken = default);
    }

    // Bundles the per-frame tracking metadata that every IPreviewFrameSink.Submit*
    // overload accepts. Collapses the prior 6-parameter trailing block (which had
    // drifted into different orderings between SubmitRawFrame and SubmitTexture)
    // into a single value with a stable field order. SourceSequenceNumber=-1 and
    // CountForPresentCadence=true match the old default-argument behavior; use
    // PreviewFrameTracking.Default as the starting point.
    public readonly record struct PreviewFrameTracking(
        long ArrivalTick,
        long SourceSequenceNumber,
        long PreviewPresentId,
        long SchedulerSubmitTick,
        long SourcePtsTicks,
        bool CountForPresentCadence)
    {
        public static PreviewFrameTracking Default { get; } = new(
            ArrivalTick: 0,
            SourceSequenceNumber: -1,
            PreviewPresentId: 0,
            SchedulerSubmitTick: 0,
            SourcePtsTicks: 0,
            CountForPresentCadence: true);

        public PreviewFrameTracking WithArrivalTick(long arrivalTick)
            => this with { ArrivalTick = arrivalTick };
    }

    internal interface IPreviewFrameSink
    {
        /// <summary>
        /// Submit a CPU-resident frame. Callee copies the data immediately;
        /// caller retains ownership and may free the buffer after return.
        /// </summary>
        void SubmitRawFrame(
            IntPtr data,
            int dataLength,
            int width,
            int height,
            bool isHdr,
            PreviewFrameTracking tracking);

        /// <summary>
        /// Submit a leased CPU-resident frame. Callee owns and disposes the lease.
        /// ArrivalTick and SourceSequenceNumber on <paramref name="tracking"/> are
        /// ignored - the lease's own ArrivalTick / SequenceNumber are authoritative.
        /// </summary>
        void SubmitRawFrameLease(
            PooledVideoFrameLease frame,
            bool isHdr,
            PreviewFrameTracking tracking);

        /// <summary>
        /// Submit a D3D11 texture. Callee calls AddRef on the COM pointer;
        /// caller may Release after return.
        /// </summary>
        void SubmitTexture(
            IntPtr d3dTexture,
            int subresourceIndex,
            int width,
            int height,
            bool isHdr,
            PreviewFrameTracking tracking);

        /// <summary>
        /// Submit split NV12 plane textures (Y + UV). Callee calls AddRef on
        /// both COM pointers; caller may Release after return.
        /// Pass <paramref name="isHdr"/> = true when the source content is HDR
        /// (e.g. NVDEC NV12 output from a P010 source) so the renderer can route
        /// the frame through the HDR shader path rather than the SDR VideoProcessor.
        /// </summary>
        void SubmitNv12PlaneTextures(
            IntPtr yTexturePtr,
            IntPtr uvTexturePtr,
            int width,
            int height,
            bool isHdr,
            PreviewFrameTracking tracking);
    }

    public readonly record struct GpuPipelineHandles(
        IntPtr D3D11DevicePtr,
        IntPtr D3D11DeviceContextPtr,
        IntPtr CudaHwDeviceCtxPtr,
        IntPtr CudaHwFramesCtxPtr)
    {
        public static GpuPipelineHandles None => default;
    }

    // Caller-built input describing the recording the user has asked for. Path
    // resolution and HDR-pipeline negotiation happen downstream and produce a
    // RecordingContext. Kept distinct so input shape and resolved-execution shape
    // don't leak into each other.
    public sealed class RecordingContextRequest
    {
        public required CaptureSettings Settings { get; init; }
        public string? AudioDeviceName { get; init; }
        public string? MicrophoneDeviceName { get; init; }
        public double EffectiveFrameRate { get; init; }
        public string FrameRateArg { get; init; } = "30";
        public uint EffectiveWidth { get; init; }
        public uint EffectiveHeight { get; init; }
        public string VideoInputPixelFormat { get; init; } = "nv12";
        public bool IsFullRangeInput { get; init; }
        public GpuPipelineHandles GpuHandles { get; init; }
        public RecordingFormat? FileNameFormatOverride { get; init; }
        public bool ReserveFinalOutputFile { get; init; } = true;
    }

    // Resolved recording-execution context. Flat record: every field is held
    // directly rather than forwarded through a wrapped Request, so consumers see
    // one boundary type with no duplicated surface.
    public sealed record RecordingContext
    {
        public required CaptureSettings Settings { get; init; }
        public string? AudioDeviceName { get; init; }
        public string? MicrophoneDeviceName { get; init; }
        public double EffectiveFrameRate { get; init; }
        public string FrameRateArg { get; init; } = "30";
        public uint EffectiveWidth { get; init; }
        public uint EffectiveHeight { get; init; }
        public string VideoInputPixelFormat { get; init; } = "nv12";
        public bool IsFullRangeInput { get; init; }
        public GpuPipelineHandles GpuHandles { get; init; }
        public RecordingFormat? FileNameFormatOverride { get; init; }

        public required string VideoOutputPath { get; init; }
        public required string FinalOutputPath { get; init; }
        public bool HdrPipelineActive { get; init; }

        // Expected output topology follows the user's selected recording
        // settings, not whether a display name happened to resolve. Capture
        // initialization is responsible for rejecting an enabled source that
        // cannot be opened before recording becomes active.
        public bool AudioEnabled => Settings.AudioEnabled;
        public bool MicrophoneEnabled => Settings.MicrophoneEnabled;

        public IntPtr D3D11DevicePtr => GpuHandles.D3D11DevicePtr;
        public IntPtr D3D11DeviceContextPtr => GpuHandles.D3D11DeviceContextPtr;
        public IntPtr CudaHwDeviceCtxPtr => GpuHandles.CudaHwDeviceCtxPtr;
        public IntPtr CudaHwFramesCtxPtr => GpuHandles.CudaHwFramesCtxPtr;
    }

    // Requested track names follow the recording settings. Shared by the recording
    // lifecycle, the LibAv sink, and the in-process structure verifier so failure
    // evidence cannot drift between them.
    public static class RecordingTracks
    {
        public static IReadOnlyList<string> BuildRequestedTracks(RecordingContext? context)
        {
            if (context == null) return Array.Empty<string>();
            var tracks = new List<string>(3) { "video" };
            if (context.AudioEnabled) tracks.Add("device_audio");
            if (context.MicrophoneEnabled) tracks.Add("microphone");
            return tracks;
        }
    }

    public enum RecordingLifecyclePhase
    {
        Idle = 0,
        Recording = 1,
        Finalizing = 2
    }

    public enum RecordingFinalizeOutcome
    {
        None = 0,
        Saved = 1,
        Failed = 2
    }

    public sealed record RecordingFinalizationProgress(
        string Stage,
        long ElapsedMs,
        long NoProgressMs,
        long ProgressVersion,
        bool NoProgressWarning);

    public sealed class FinalizeResult
    {
        private static readonly IReadOnlyList<string> EmptyArtifacts = Array.Empty<string>();

        public bool Succeeded { get; init; }
        public RecordingFinalizeOutcome Outcome { get; init; }
        public string OutputPath { get; init; } = string.Empty;
        public string StatusMessage { get; init; } = "Stopped";
        public IReadOnlyList<string> PreservedArtifacts { get; init; } = EmptyArtifacts;
        public bool VerificationCompleted { get; init; }
        public bool CleanupPending { get; init; }
        public string? RecoveryPath { get; init; }
        public string FailureCode { get; init; } = string.Empty;
        public long FinalizationElapsedMs { get; init; }
        public IReadOnlyList<string> RequestedTracks { get; init; } = EmptyArtifacts;
        public IReadOnlyList<string> ObservedTracks { get; init; } = EmptyArtifacts;

        public FinalizeResult WithTrackEvidence(
            IEnumerable<string>? requestedTracks,
            IEnumerable<string>? observedTracks)
        {
            return new FinalizeResult
            {
                Succeeded = Succeeded,
                Outcome = Outcome,
                OutputPath = OutputPath,
                StatusMessage = StatusMessage,
                PreservedArtifacts = PreservedArtifacts,
                VerificationCompleted = VerificationCompleted,
                CleanupPending = CleanupPending,
                RecoveryPath = RecoveryPath,
                FailureCode = FailureCode,
                FinalizationElapsedMs = FinalizationElapsedMs,
                RequestedTracks = NormalizeDistinct(requestedTracks),
                ObservedTracks = NormalizeDistinct(observedTracks)
            };
        }

        // Re-derives this result as a failure with a new status/failure code while
        // preserving every other field, including requested/observed track evidence.
        public FinalizeResult AsFailure(string statusMessage, string failureCode)
            => Failure(OutputPath, statusMessage, PreservedArtifacts, failureCode, CleanupPending, RecoveryPath, VerificationCompleted, FinalizationElapsedMs)
                .WithTrackEvidence(RequestedTracks, ObservedTracks);

        // Re-derives this result as a failure with replacement preserved artifacts and
        // recovery path, preserving every other field including track evidence. Routed
        // through Failure() so Outcome is normalized to Failed even for results that
        // were constructed directly (e.g. by FlashbackEncoderSink) without an Outcome.
        public FinalizeResult AsFailureWithArtifacts(IEnumerable<string>? preservedArtifacts, string? recoveryPath)
            => Failure(OutputPath, StatusMessage, preservedArtifacts, FailureCode, CleanupPending, recoveryPath, VerificationCompleted, FinalizationElapsedMs)
                .WithTrackEvidence(RequestedTracks, ObservedTracks);

        private static IReadOnlyList<string> NormalizeDistinct(IEnumerable<string>? values)
            => values == null
                ? EmptyArtifacts
                : new ReadOnlyCollection<string>(
                    values
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray());

        public static FinalizeResult Success(string outputPath, string statusMessage = "Stopped")
            => Success(outputPath, statusMessage, verificationCompleted: false, finalizationElapsedMs: 0);

        public static FinalizeResult Success(
            string outputPath,
            string statusMessage,
            bool verificationCompleted,
            long finalizationElapsedMs = 0)
        {
            return new FinalizeResult
            {
                Succeeded = true,
                Outcome = RecordingFinalizeOutcome.Saved,
                OutputPath = outputPath,
                StatusMessage = statusMessage,
                PreservedArtifacts = EmptyArtifacts,
                VerificationCompleted = verificationCompleted,
                FinalizationElapsedMs = finalizationElapsedMs
            };
        }

        public static FinalizeResult Failure(
            string outputPath,
            string statusMessage,
            IEnumerable<string>? preservedArtifacts = null)
            => Failure(
                outputPath,
                statusMessage,
                preservedArtifacts,
                failureCode: "recording-finalization-failed",
                cleanupPending: false,
                recoveryPath: null,
                verificationCompleted: false,
                finalizationElapsedMs: 0);

        public static FinalizeResult Failure(
            string outputPath,
            string statusMessage,
            IEnumerable<string>? preservedArtifacts,
            string failureCode,
            bool cleanupPending = false,
            string? recoveryPath = null,
            bool verificationCompleted = false,
            long finalizationElapsedMs = 0)
        {
            var artifacts = NormalizeDistinct(preservedArtifacts);

            return new FinalizeResult
            {
                Succeeded = false,
                Outcome = RecordingFinalizeOutcome.Failed,
                OutputPath = outputPath,
                StatusMessage = statusMessage,
                PreservedArtifacts = artifacts,
                FailureCode = failureCode,
                CleanupPending = cleanupPending,
                RecoveryPath = recoveryPath,
                VerificationCompleted = verificationCompleted,
                FinalizationElapsedMs = finalizationElapsedMs
            };
        }
    }

    /// <summary>
    /// Accepts D3D11 texture references for GPU-resident NVENC encoding.
    /// Callee does AddRef on the texture; caller may release after return.
    /// </summary>
    public interface IGpuVideoFrameEncoder
    {
        void EnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex);
    }

    public interface IGpuVideoFrameTryEncoder
    {
        bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex);
    }

    public interface IRawVideoFrameEncoder
    {
        void EnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize);
    }

    public interface IRawVideoFrameTryEncoder
    {
        bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize);
    }

    /// <summary>
    /// On either normal return, the callee owns the lease, including disposal
    /// when admission is rejected. The caller disposes its lease if the call throws.
    /// </summary>
    internal interface IRawVideoFrameLeaseTryEncoder
    {
        bool TryEnqueueRawVideoFrame(PooledVideoFrameLease frame);
    }

    public interface IRecordingSink : IDisposable, IAsyncDisposable
    {
        Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hot WASAPI callback write. Implementations must copy or enqueue
        /// synchronously, must not do blocking/async work, and must return a
        /// completed task. The Task return preserves the existing contract shape.
        /// </summary>
        Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default);

        Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default);
    }

    public interface IRecordingVerifier
    {
        Task<RecordingVerificationResult> VerifyAsync(
            string? outputPath,
            CaptureRuntimeSnapshot runtimeSnapshot,
            CancellationToken cancellationToken = default);
    }

    // Pixel formats carried by pooled decoded frames. The enum is deliberately
    // narrow because the pooled-frame path currently transports luma/chroma capture
    // buffers, not arbitrary RGB render targets.
    internal enum PooledVideoPixelFormat
    {
        Unknown = 0,
        Nv12 = 1,
        P010 = 2
    }

    // ArrayPool-backed decoded frame with reference-counted leases. The owner can
    // dispose immediately after fan-out; the buffer returns to the pool only after
    // every preview/recording consumer releases its lease.
    internal sealed class PooledVideoFrame : IDisposable
    {
        public static int GetFrameSizeBytes(int width, int height, bool isP010)
            => isP010 ? width * height * 3 : (width * height * 3) / 2;

        private readonly object _leaseSync = new();
        private readonly ArrayPool<byte> _pool;
        private readonly byte[] _buffer;
        private int _leaseCount = 1;
        private int _ownerReleased;
        private int _returned;

        private PooledVideoFrame(
            long sequenceNumber,
            long arrivalTick,
            long decodedTick,
            int width,
            int height,
            PooledVideoPixelFormat pixelFormat,
            int length,
            ArrayPool<byte> pool)
        {
            ArgumentNullException.ThrowIfNull(pool);
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));

            SequenceNumber = sequenceNumber;
            ArrivalTick = arrivalTick;
            DecodedTick = decodedTick;
            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
            Length = length;
            _pool = pool;
            _buffer = pool.Rent(length);
        }

        public long SequenceNumber { get; }
        public long ArrivalTick { get; }
        public long DecodedTick { get; internal set; }
        public int Width { get; }
        public int Height { get; }
        public PooledVideoPixelFormat PixelFormat { get; }
        public int Length { get; }
        public int LeaseCount
        {
            get
            {
                lock (_leaseSync)
                {
                    return _leaseCount;
                }
            }
        }

        public bool IsReturned
        {
            get
            {
                lock (_leaseSync)
                {
                    return _returned != 0;
                }
            }
        }
        public Memory<byte> Memory
        {
            get
            {
                ThrowIfOwnerAccessClosed();
                return new Memory<byte>(_buffer, 0, Length);
            }
        }

        public Span<byte> Span
        {
            get
            {
                ThrowIfOwnerAccessClosed();
                return _buffer.AsSpan(0, Length);
            }
        }

        public static PooledVideoFrame Rent(
            long sequenceNumber,
            long arrivalTick,
            long decodedTick,
            int width,
            int height,
            PooledVideoPixelFormat pixelFormat,
            int length)
            => new(sequenceNumber, arrivalTick, decodedTick, width, height, pixelFormat, length, ArrayPool<byte>.Shared);

        public PooledVideoFrameLease AddLease()
        {
            if (TryAddLease(out var lease))
            {
                return lease;
            }

            throw new ObjectDisposedException(nameof(PooledVideoFrame));
        }

        public bool TryAddLease([NotNullWhen(true)] out PooledVideoFrameLease? lease)
        {
            lock (_leaseSync)
            {
                if (_leaseCount <= 0 || _ownerReleased != 0 || _returned != 0)
                {
                    lease = default;
                    return false;
                }

                _leaseCount++;
                lease = new PooledVideoFrameLease(this);
                return true;
            }
        }

        public void Dispose()
        {
            lock (_leaseSync)
            {
                if (_ownerReleased != 0)
                {
                    return;
                }

                _ownerReleased = 1;
                ReleaseLeaseCore();
            }
        }

        internal void ReleaseLease()
        {
            lock (_leaseSync)
            {
                ReleaseLeaseCore();
            }
        }

        internal ReadOnlyMemory<byte> GetReadOnlyMemoryForLease()
        {
            lock (_leaseSync)
            {
                if (_returned != 0)
                {
                    throw new ObjectDisposedException(nameof(PooledVideoFrame));
                }
            }

            return new ReadOnlyMemory<byte>(_buffer, 0, Length);
        }

        private void ReleaseLeaseCore()
        {
            var remaining = --_leaseCount;
            if (remaining < 0)
            {
                throw new InvalidOperationException("Pooled video frame lease count went negative.");
            }

            if (remaining == 0 && _returned == 0)
            {
                _returned = 1;
                _pool.Return(_buffer);
            }
        }

        private void ThrowIfOwnerAccessClosed()
        {
            lock (_leaseSync)
            {
                if (_ownerReleased != 0 || _returned != 0)
                {
                    throw new ObjectDisposedException(nameof(PooledVideoFrame));
                }
            }
        }
    }

    // Read-only consumer lease over a PooledVideoFrame. Lease metadata is copied at
    // creation so diagnostics can still identify the frame after Dispose releases
    // the underlying pooled bytes.
    internal sealed class PooledVideoFrameLease : IDisposable
    {
        private PooledVideoFrame? _frame;

        internal PooledVideoFrameLease(PooledVideoFrame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            SequenceNumber = frame.SequenceNumber;
            ArrivalTick = frame.ArrivalTick;
            DecodedTick = frame.DecodedTick;
            Width = frame.Width;
            Height = frame.Height;
            PixelFormat = frame.PixelFormat;
            Length = frame.Length;
        }

        public long SequenceNumber { get; }
        public long ArrivalTick { get; }
        public long DecodedTick { get; }
        public int Width { get; }
        public int Height { get; }
        public PooledVideoPixelFormat PixelFormat { get; }
        public int Length { get; }
        public ReadOnlyMemory<byte> Memory =>
            (Volatile.Read(ref _frame) ?? throw new ObjectDisposedException(nameof(PooledVideoFrameLease)))
            .GetReadOnlyMemoryForLease();

        public void Dispose()
        {
            var frame = Interlocked.Exchange(ref _frame, null);
            frame?.ReleaseLease();
        }
    }
}
