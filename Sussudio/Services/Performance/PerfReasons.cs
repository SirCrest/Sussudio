using System;

namespace Sussudio.Services.Performance;

/// <summary>
/// A fixed set of counters, one per reason code, registered once at startup under <c>prefix + name</c>. Reason
/// vocabularies in the app are open-ended strings; each owner maps its string to a code from a closed enum, so the
/// counter set stays bounded and an increment is an array index plus one interlocked add: no lookup, no allocation.
/// Code 0 means "none" and has no counter; codes past the table clamp to the last entry, which every table
/// reserves for "other".
/// </summary>
public sealed class PerfReasonCounters
{
    private readonly PerfCounter[] _counters;

    private PerfReasonCounters(PerfCounter[] counters)
    {
        _counters = counters;
    }

    /// <summary>Registers <c>prefix + names[i]</c> for code <c>i + 1</c>.</summary>
    public static PerfReasonCounters Create(PerfRegistry registry, string prefix, string descriptionPrefix, string[] names)
    {
        var counters = new PerfCounter[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            counters[i] = registry.Counter(prefix + names[i], descriptionPrefix + " '" + names[i] + "'");
        }

        return new PerfReasonCounters(counters);
    }

    public int Count => _counters.Length;

    /// <summary>The counter for a code (1-based). Zero, negative and oversized codes map to the last ("other") entry.</summary>
    public PerfCounter Counter(int code)
    {
        var index = code - 1;
        if ((uint)index >= (uint)_counters.Length)
        {
            index = _counters.Length - 1;
        }

        return _counters[index];
    }

    public void Increment(int code) => Counter(code).Increment();
}

/// <summary>Why the preview renderer dropped a frame. Wire names are in <see cref="PreviewDropReasons"/>; append, never renumber.</summary>
public enum PreviewDropReason : ushort
{
    None = 0,
    RendererStopped = 1,
    RendererBacklog = 2,
    ExplicitDrain = 3,
    LivePreviewSuppressed = 4,
    LivePreviewResumed = 5,
    SharedDeviceReset = 6,
    StaleGeneration = 7,
    Stale = 8,
    RenderSkipped = 9,
    RenderFailed = 10,
    StaleBeforeRender = 11,
    RendererExit = 12,
    RendererStop = 13,
    DeviceLost = 14,

    /// <summary>Any reason string outside the table. Keeps <c>preview.drop.*</c> bounded.</summary>
    Other = 15,
}

/// <summary>
/// The fixed <c>preview.drop.&lt;reason&gt;</c> table and the mapping from the renderer's caller-supplied reason
/// strings. A reason of the form <c>{x}:stale</c> (a frame skipped because its submission generation was
/// superseded) maps to <c>stale</c>.
/// </summary>
public static class PreviewDropReasons
{
    internal static readonly string[] CounterNames =
    {
        "renderer-stopped",
        "renderer-backlog",
        "explicit-drain",
        "live-preview-suppressed",
        "live-preview-resumed",
        "shared-device-reset",
        "stale-generation",
        "stale",
        "render-skipped",
        "render-failed",
        "stale-before-render",
        "renderer-exit",
        "renderer-stop",
        "device-lost",
        "other",
    };

    public static string Name(PreviewDropReason reason)
    {
        var index = (int)reason - 1;
        return index >= 0 && index < CounterNames.Length ? CounterNames[index] : "none";
    }

    /// <summary>Maps a renderer drop reason string to its table entry without allocating.</summary>
    public static PreviewDropReason Classify(string? reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            return PreviewDropReason.Other;
        }

        switch (reason)
        {
            case "renderer-stopped": return PreviewDropReason.RendererStopped;
            case "renderer-backlog": return PreviewDropReason.RendererBacklog;
            case "explicit-drain": return PreviewDropReason.ExplicitDrain;
            case "live-preview-suppressed": return PreviewDropReason.LivePreviewSuppressed;
            case "live-preview-resumed": return PreviewDropReason.LivePreviewResumed;
            case "shared-device-reset": return PreviewDropReason.SharedDeviceReset;
            case "stale-generation": return PreviewDropReason.StaleGeneration;
            case "stale": return PreviewDropReason.Stale;
            case "render-skipped": return PreviewDropReason.RenderSkipped;
            case "render-failed": return PreviewDropReason.RenderFailed;
            case "stale-before-render": return PreviewDropReason.StaleBeforeRender;
            case "renderer-exit": return PreviewDropReason.RendererExit;
            case "renderer-stop": return PreviewDropReason.RendererStop;
            case "device-lost": return PreviewDropReason.DeviceLost;
        }

        return reason.EndsWith(":stale", StringComparison.Ordinal) ? PreviewDropReason.Stale : PreviewDropReason.Other;
    }
}

/// <summary>Why the MJPEG preview jitter buffer dropped a frame (<c>preview.jitter_drop.&lt;reason&gt;</c>).</summary>
public enum JitterDropReason : ushort
{
    None = 0,
    QueueFull = 1,
    LateSequence = 2,
    MissingSequence = 3,
    SubmitFailed = 4,
    HardDeadline = 5,
    SoftDeadline = 6,

    /// <summary>The queue was cleared (stop, restart or a flush).</summary>
    Cleared = 7,

    /// <summary>The queue was cleared because the preview was suppressed.</summary>
    Suppressed = 8,

    Other = 9,
}

/// <summary>Why the jitter buffer found nothing to submit (<c>preview.jitter_underflow.&lt;reason&gt;</c>).</summary>
public enum JitterUnderflowReason : ushort
{
    None = 0,
    EmptyQueue = 1,
    WaitingForSequence = 2,
    SelectionBlocked = 3,

    /// <summary>A deliberate miss right after a preview resume; the buffer's own underflow count does not include these.</summary>
    ResumeReprime = 4,

    Other = 5,
}

/// <summary>The fixed jitter-buffer reason tables and the mapping from the buffer's reason strings.</summary>
public static class JitterReasons
{
    internal static readonly string[] DropCounterNames =
    {
        "queue-full",
        "late-sequence",
        "missing-sequence",
        "submit-failed",
        "hard-deadline",
        "soft-deadline",
        "cleared",
        "suppressed",
        "other",
    };

    internal static readonly string[] UnderflowCounterNames =
    {
        "empty-queue",
        "waiting-for-sequence",
        "selection-blocked",
        "resume-reprime",
        "other",
    };

    public static string Name(JitterDropReason reason)
    {
        var index = (int)reason - 1;
        return index >= 0 && index < DropCounterNames.Length ? DropCounterNames[index] : "none";
    }

    public static string Name(JitterUnderflowReason reason)
    {
        var index = (int)reason - 1;
        return index >= 0 && index < UnderflowCounterNames.Length ? UnderflowCounterNames[index] : "none";
    }

    public static JitterDropReason ClassifyDrop(string? reason)
    {
        switch (reason)
        {
            case "queue-full": return JitterDropReason.QueueFull;
            case "late-sequence": return JitterDropReason.LateSequence;
            case "missing-sequence": return JitterDropReason.MissingSequence;
            case "submit-failed": return JitterDropReason.SubmitFailed;
            case "hard-deadline": return JitterDropReason.HardDeadline;
            case "soft-deadline": return JitterDropReason.SoftDeadline;
            case "cleared": return JitterDropReason.Cleared;
            case "suppressed": return JitterDropReason.Suppressed;
            default: return JitterDropReason.Other;
        }
    }

    public static JitterUnderflowReason ClassifyUnderflow(string? reason)
    {
        switch (reason)
        {
            case "empty-queue": return JitterUnderflowReason.EmptyQueue;
            case "waiting-for-sequence": return JitterUnderflowReason.WaitingForSequence;
            case "selection-blocked": return JitterUnderflowReason.SelectionBlocked;
            case "resume-reprime": return JitterUnderflowReason.ResumeReprime;
            default: return JitterUnderflowReason.Other;
        }
    }
}
