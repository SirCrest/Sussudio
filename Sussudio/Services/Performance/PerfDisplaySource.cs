using System;
using System.Globalization;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// The preview swap chain's DXGI frame statistics at one instant, as the renderer last sampled them (it already
/// samples every second frame; reading them here adds no call on the present path). <see cref="PresentCount"/> counts
/// presents completed on the swap chain, <see cref="PresentRefreshCount"/> is the display refresh count the last
/// presented frame appeared at, and <see cref="SyncRefreshCount"/> with <see cref="SyncQpcTime"/> is the refresh count
/// at the most recent vertical sync and the QPC time of that sync. <see cref="Generation"/> changes whenever the
/// renderer re-creates its device or swap chain, which restarts <see cref="PresentCount"/>, so a delta across two
/// generations is meaningless and the report leaves it out.
/// </summary>
public sealed record PerfDisplayCounters(
    long Generation,
    long SampleCount,
    long PresentCount,
    long PresentRefreshCount,
    long SyncRefreshCount,
    long SyncQpcTime);

/// <summary>
/// Process-wide hook through which the preview renderer exposes its DXGI frame statistics to the perf report without
/// the performance code depending on the renderer. The renderer registers a reader when its render thread starts
/// and unregisters it when the thread ends; <see cref="Read"/> returns null when no renderer is active or it has no
/// sample yet. Control plane only (the reader takes the renderer's statistics lock briefly).
/// </summary>
public static class PerfDisplaySource
{
    private static Func<PerfDisplayCounters?>? _provider;

    public static void Register(Func<PerfDisplayCounters?> provider) => Volatile.Write(ref _provider, provider);

    /// <summary>Removes <paramref name="provider"/> if it is still the registered one (a newer renderer may have replaced it).</summary>
    public static void Unregister(Func<PerfDisplayCounters?> provider) =>
        Interlocked.CompareExchange(ref _provider, null, provider);

    /// <summary>Latest counters, or null when unavailable. Never throws.</summary>
    public static PerfDisplayCounters? Read()
    {
        var provider = Volatile.Read(ref _provider);
        if (provider is null)
        {
            return null;
        }

        try
        {
            return provider();
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>How the preview reaches the display, derived from the DWM and swap-chain rates of one report interval.</summary>
public readonly record struct DwmCompositionVerdict(string Path, string Interpretation);

/// <summary>
/// Reads the DWM composed rate in light of the preview's present rate. DWM counts a composition pass only when it
/// composes a frame; a swap chain that reaches the display by independent flip, direct flip or a hardware overlay
/// plane (MPO) is scanned out without DWM composing each of its frames, so on a 120 Hz display running a 60 fps
/// preview DWM can compose well under one frame a second while every frame is displayed. A low composed rate is
/// therefore not a drop signal by itself; this verdict says which situation the numbers describe.
/// Paths: <c>unknown</c> (no DWM reading or no present rate), <c>idle</c> (the preview presented nothing), <c>bypass</c> (composed far
/// below presented), <c>composed</c> (about one composition per present), <c>mixed</c> (in between).
/// </summary>
public static class DwmCompositionInterpreter
{
    private const double BypassBelow = 0.25;
    private const double ComposedAtLeast = 0.75;
    private const double MinActivePresentsPerSec = 1.0;

    public static DwmCompositionVerdict Classify(
        double? composedPerSec,
        double? presentsPerSec,
        double? refreshHz)
    {
        if (composedPerSec is not { } composed)
        {
            return new DwmCompositionVerdict("unknown", "DWM composition timing was unavailable for this interval.");
        }

        if (presentsPerSec is not { } presents)
        {
            return new DwmCompositionVerdict(
                "unknown",
                $"DWM composed {Format(composed)}/s, but no swap-chain present rate was available for this interval (no preview sampling DXGI frame statistics), so it cannot be interpreted.");
        }

        if (presents < MinActivePresentsPerSec)
        {
            return new DwmCompositionVerdict(
                "idle",
                $"The preview presented no frames in this interval; DWM composed {Format(composed)}/s, which only reflects other screen changes.");
        }

        var display = refreshHz is { } hz && hz > 0 ? $" on a {Format(hz)} Hz display" : string.Empty;
        var ratio = composed / presents;
        if (ratio < BypassBelow)
        {
            return new DwmCompositionVerdict(
                "bypass",
                $"The preview presented {Format(presents)} frames/s{display} while DWM composed {Format(composed)}/s. " +
                "Frames are reaching the screen without DWM composing each one (independent flip, direct flip or a hardware overlay plane), " +
                "so a low composed rate here is expected and is not dropped frames.");
        }

        if (ratio >= ComposedAtLeast)
        {
            return new DwmCompositionVerdict(
                "composed",
                $"DWM composed {Format(composed)}/s against {Format(presents)} presents/s{display}: each presented frame is composed by DWM.");
        }

        return new DwmCompositionVerdict(
            "mixed",
            $"DWM composed {Format(composed)}/s against {Format(presents)} presents/s{display}: some presented frames bypassed composition.");
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
