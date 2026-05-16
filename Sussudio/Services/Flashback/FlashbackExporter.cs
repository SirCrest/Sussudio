using System;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Exports Flashback time ranges by remuxing finalized segment artifacts to .mp4.
/// No re-encoding — just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe partial class FlashbackExporter : IDisposable
{
    // Export reads finalized segment artifacts only. Live capture continues via
    // FlashbackEncoderSink while this class remuxes packets into the target MP4.
    private delegate bool CompletedOutputValidator(string outputPath, out long outputBytes, out string failureMessage);

    private const int MaxSupportedInputStreams = 64;
    private const int ProgressHeartbeatIntervalMs = 1_000;
    private const int ExportLockWaitTimeoutSeconds = 30;
    private static readonly TimeSpan OrphanTempFileMinimumAge = TimeSpan.FromMinutes(15);

    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private readonly object _lifetimeSync = new();
    private CancellationTokenSource? _disposeCts = new();
    private AVFormatContext* _activeInputContext;
    private AVFormatContext* _activeOutputContext;
    private string? _activeTempPath;
    private bool _disposed;

}
