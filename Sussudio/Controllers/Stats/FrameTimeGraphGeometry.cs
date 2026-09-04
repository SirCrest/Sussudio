using System;
using System.Numerics;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal enum FrameTimeGraphPanel { FramesPerSecond, FrameTime }

internal readonly record struct FrameTimeGraphScale(double ExpectedFps)
{
    public double FrameBudgetMs => 1000 / ExpectedFps;
    public double MaximumFrameTimeMs => FrameBudgetMs * 3.25;
    public double MaximumFps => ExpectedFps * 1.25;

    public static FrameTimeGraphScale FromExpectedFps(double value)
        => new(double.IsFinite(value) && value > 0 ? Math.Clamp(value, 1, 1000) : 60);
}

internal readonly record struct FrameTimeGraphSegment(Vector2 Start, Vector2 End, bool OutOfRange);

internal static class FrameTimeGraphGeometry
{
    public static float ProjectX(long timestampQpc, long originQpc, long frequency, float width)
        => width + (float)((timestampQpc - originQpc) / (double)frequency /
            PreviewFrameTimeHistory.WindowSeconds * width);

    public static float ProjectY(double intervalMs, FrameTimeGraphScale scale,
        FrameTimeGraphPanel panel, float height, out bool outOfRange)
    {
        var value = panel == FrameTimeGraphPanel.FrameTime ? intervalMs : 1000 / intervalMs;
        var maximum = panel == FrameTimeGraphPanel.FrameTime ? scale.MaximumFrameTimeMs : scale.MaximumFps;
        outOfRange = !double.IsFinite(value) || value < 0 || value > maximum;
        return height * (1 - (float)Math.Clamp(double.IsFinite(value) ? value / maximum : 1, 0, 1));
    }

    public static bool TryProjectSegment(PreviewFrameTimeSample? previous,
        PreviewFrameTimeSample sample, long originQpc, long frequency, float width, float height,
        FrameTimeGraphScale scale, FrameTimeGraphPanel panel, out FrameTimeGraphSegment segment)
    {
        segment = default;
        if (!sample.IsCadenceSample || width <= 0 || height <= 0 || frequency <= 0) return false;
        var x = ProjectX(sample.TimestampQpc, originQpc, frequency, width);
        var y = ProjectY(sample.IntervalMs, scale, panel, height, out var overflow);
        var end = new Vector2(x, y);
        var canJoin = previous is { IsCadenceSample: true } prior &&
            prior.Epoch == sample.Epoch && prior.Sequence + 1 == sample.Sequence &&
            prior.TimestampQpc <= sample.TimestampQpc &&
            (sample.Flags & PreviewFrameTimeSampleFlags.GapBefore) == 0;
        if (canJoin)
        {
            var priorSample = previous!.Value;
            var priorY = ProjectY(priorSample.IntervalMs, scale, panel, height, out var priorOverflow);
            segment = new(new(ProjectX(priorSample.TimestampQpc, originQpc, frequency, width), priorY),
                end, overflow || priorOverflow);
        }
        else
        {
            // An isolated sample gets a small tick. Never bridge an epoch reset,
            // overwritten history, or a frame excluded from cadence measurement.
            segment = overflow
                ? new(end, new(x, Math.Min(height, y + 5)), true)
                : new(new(x - 0.75f, y), new(x + 0.75f, y), false);
        }
        return true;
    }
}
