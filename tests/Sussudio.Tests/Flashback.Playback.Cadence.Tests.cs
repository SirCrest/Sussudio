using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs")
            .Replace("\r\n", "\n");
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackTiming.cs")
            .Replace("\r\n", "\n");
        var playbackPtsCadenceText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackPtsCadence.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0))");
        AssertContains(sourceText, "frameDuration = ResolveFrameDuration(decoder);");
        AssertContains(sourceText, "private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = decoder.FrameRate;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = FallbackPlaybackFrameRate;\n        }");
        AssertContains(sourceText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(sourceText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(playbackTimingText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(playbackTimingText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertDoesNotContain(rootText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertDoesNotContain(rootText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(sourceText, "fps = Math.Min(fps, MaxPlaybackFrameRate);");
        AssertContains(sourceText, "_playbackTargetFps = fps;");
        AssertContains(sourceText, "public double PlaybackTargetFps => _playbackTargetFps;");
        AssertContains(sourceText, "return TimeSpan.FromSeconds(1.0 / fps);");
        AssertContains(sourceText, "TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);");
        AssertContains(playbackPtsCadenceText, "private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)");
        AssertContains(playbackPtsCadenceText, "private void ResetPlaybackPtsCadenceBaseline()");
        AssertContains(playbackPtsCadenceText, "private void RecordPlaybackPtsCadenceMismatch(");
        AssertContains(playbackPtsCadenceText, "private long _lastPlaybackCadencePtsTicks = -1;");
        AssertContains(playbackPtsCadenceText, "private long _playbackPtsCadenceMismatchCount;");
        AssertContains(playbackPtsCadenceText, "private long _lastPlaybackPtsCadenceMismatchUtcUnixMs;");
        AssertContains(playbackPtsCadenceText, "private double _lastPlaybackPtsCadenceDeltaMs;");
        AssertContains(playbackPtsCadenceText, "private double _lastPlaybackPtsCadenceExpectedMs;");
        AssertContains(playbackPtsCadenceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(playbackPtsCadenceText, "public long LastPlaybackPtsCadenceMismatchUtcUnixMs => Interlocked.Read(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs);");
        AssertContains(playbackPtsCadenceText, "public double LastPlaybackPtsCadenceDeltaMs => _lastPlaybackPtsCadenceDeltaMs;");
        AssertContains(playbackPtsCadenceText, "public double LastPlaybackPtsCadenceExpectedMs => _lastPlaybackPtsCadenceExpectedMs;");
        AssertContains(playbackPtsCadenceText, "FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH");
        AssertDoesNotContain(playbackTimingText, "private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)");
        AssertDoesNotContain(playbackTimingText, "FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH");
        AssertDoesNotContain(metricsText, "public long PlaybackPtsCadenceMismatchCount =>");
        AssertDoesNotContain(metricsText, "public double LastPlaybackPtsCadenceDeltaMs =>");
        AssertContains(sourceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackPtsCadenceMismatchCount, 0);");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var track = controllerType.GetMethod("TrackDecodedPtsCadence", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TrackDecodedPtsCadence not found.");
        var reset = controllerType.GetMethod("ResetPlaybackMetrics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResetPlaybackMetrics not found.");
        var expected = TimeSpan.FromMilliseconds(1000.0 / 120.0);

        try
        {
            track.Invoke(controller, new object[] { expected, expected });
            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 60.0), expected });
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "matching decoded PTS cadence count");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(1L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "slow decoded PTS cadence count");
            AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "slow decoded PTS cadence delta");
            AssertNearlyEqual(expected.TotalMilliseconds, GetDoubleProperty(controller, "LastPlaybackPtsCadenceExpectedMs"), 0.1, "decoded PTS expected cadence");
            if (GetLongProperty(controller, "LastPlaybackPtsCadenceMismatchUtcUnixMs") <= 0)
            {
                throw new InvalidOperationException("Expected decoded PTS cadence mismatch timestamp to be populated.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(2L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "duplicate decoded PTS cadence count");
            AssertNearlyEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "duplicate decoded PTS cadence delta");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(25.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "backward decoded PTS cadence count");
            if (GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs") >= 0)
            {
                throw new InvalidOperationException("Expected backward decoded PTS cadence delta to be negative.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 24.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "valid cadence after backward PTS remains clean");

            reset.Invoke(controller, null);
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "decoded PTS cadence reset count");
            AssertEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), "decoded PTS cadence reset delta");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackController_ResetClearsDecodeMetrics()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.MetricsCollection.cs")
            .Replace("\r\n", "\n");
        var metricResetText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.MetricReset.cs")
            .Replace("\r\n", "\n");
        var playbackCadenceMetricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackCadenceMetrics.cs")
            .Replace("\r\n", "\n");
        var playbackDecodeMetricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackDecodeMetrics.cs")
            .Replace("\r\n", "\n");
        var playbackDecodeMetricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackDecodeMetricsCollection.cs")
            .Replace("\r\n", "\n");

        var resetMetricsBlock = ExtractTextBetween(
            sourceText,
            "private void ResetPlaybackMetrics()",
            "private void RestoreAudioCallback");
        AssertContains(metricsCollectionText, "private long _playbackFrameCount;");
        AssertContains(metricsCollectionText, "private long _playbackDroppedFrames;");
        AssertContains(metricsCollectionText, "private readonly Stopwatch _playbackFpsClock = new();");
        AssertContains(metricsCollectionText, "private const int PlaybackCadenceSampleCapacity = 240;");
        AssertContains(metricsCollectionText, "private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertDoesNotContain(metricsCollectionText, "private long _lastPlaybackCadencePtsTicks = -1;");
        AssertDoesNotContain(metricsCollectionText, "private long _playbackPtsCadenceMismatchCount;");
        AssertContains(metricResetText, "private void ResetPlaybackMetrics()");
        AssertContains(metricResetText, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(metricResetText, "lock (_playbackDecodeLock)");
        AssertContains(metricResetText, "Array.Clear(_playbackDecodeDurationsMs);");
        AssertContains(metricResetText, "_playbackDecodeDurationHead = 0;");
        AssertContains(metricResetText, "_playbackDecodeDurationCount = 0;");
        AssertContains(playbackCadenceMetricsText, "public readonly record struct PlaybackCadenceMetrics(");
        AssertContains(playbackCadenceMetricsText, "public PlaybackCadenceMetrics GetPlaybackCadenceMetrics()");
        AssertContains(playbackCadenceMetricsText, "private static double PercentileFromSorted(double[] sortedSamples, double percentile)");
        AssertContains(playbackDecodeMetricsText, "public readonly record struct PlaybackDecodeMetrics(");
        AssertContains(playbackDecodeMetricsText, "public PlaybackDecodeMetrics GetPlaybackDecodeMetrics()");
        AssertContains(playbackDecodeMetricsCollectionText, "private readonly double[] _playbackDecodeDurationsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(playbackDecodeMetricsCollectionText, "private double _playbackMaxDecodeTotalMs;");
        AssertContains(playbackDecodeMetricsCollectionText, "private string _playbackMaxDecodePhase = string.Empty;");
        AssertContains(playbackDecodeMetricsCollectionText, "public string PlaybackMaxDecodePhase => Volatile.Read(ref _playbackMaxDecodePhase);");
        AssertContains(playbackDecodeMetricsCollectionText, "public double PlaybackMaxDecodeSendMs => _playbackMaxDecodeSendMs;");
        AssertContains(playbackDecodeMetricsCollectionText, "public long PlaybackMaxDecodePositionMs => Interlocked.Read(ref _playbackMaxDecodePositionMs);");
        AssertContains(playbackDecodeMetricsCollectionText, "private bool TryDecodeNextVideoFrameWithMetrics(");
        AssertContains(playbackDecodeMetricsCollectionText, "private void TrackPlaybackDecodeDuration(");
        AssertContains(playbackDecodeMetricsCollectionText, "private static string ResolveDominantDecodePhase(FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)");
        AssertDoesNotContain(metricsText, "public string PlaybackMaxDecodePhase =>");
        AssertDoesNotContain(metricsText, "public double PlaybackMaxDecodeSendMs =>");
        AssertDoesNotContain(metricsCollectionText, "private static double PercentileFromSorted(double[] sortedSamples, double percentile)");
        AssertDoesNotContain(metricsCollectionText, "private bool TryDecodeNextVideoFrameWithMetrics(");
        AssertDoesNotContain(metricsCollectionText, "private static string ResolveDominantDecodePhase(FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)");
        AssertDoesNotContain(metricsCollectionText, "private void ResetPlaybackMetrics()");
        AssertDoesNotContain(rootText, "private long _playbackFrameCount;");
        AssertDoesNotContain(rootText, "private readonly Stopwatch _playbackFpsClock = new();");
        AssertDoesNotContain(rootText, "private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertDoesNotContain(rootText, "private string _playbackMaxDecodePhase = string.Empty;");
        AssertContains(resetMetricsBlock, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(sourceText, "if (phaseTimings.FeedMs > max) { phase = \"feed\"; max = phaseTimings.FeedMs; }");

        return Task.CompletedTask;
    }

}
