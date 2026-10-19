namespace Sussudio.Tools.Performance;

// Shared capture state folded from trace events. The reader and analysis builder consume this bounded accumulator
// model; time conversion waits until metadata at the end of the dump has been read.

internal readonly record struct AmbientEvent(double StartUs, double EndUs, string Name, string? Text);

internal sealed class AmbientAcc
{
    public int Count;
    public double TotalUs;
    public double MaxUs;
}

/// <summary>Everything one frame did, folded from its events: per-stage duration, first start and last end.</summary>
internal sealed class FrameAcc
{
    private const int InitialStages = 12;

    public FrameAcc(long id)
    {
        Id = id;
        DurUs = new double[InitialStages];
        StartUs = new double[InitialStages];
        EndUs = new double[InitialStages];
        Array.Fill(StartUs, double.PositiveInfinity);
        Array.Fill(EndUs, double.NegativeInfinity);
    }

    public long Id { get; }

    public double FirstSpanUs { get; private set; } = double.PositiveInfinity;

    public double FirstAnyUs { get; private set; } = double.PositiveInfinity;

    public double LastEndUs { get; private set; } = double.NegativeInfinity;

    public double SampleUs { get; set; }

    public bool HasSample { get; set; }

    public int Drops { get; set; }

    /// <summary>The first drop reason recorded for this frame (args.reason of its preview.drop instant), if any.</summary>
    public string? DropReason { get; set; }

    public double ArrivalUs { get; set; }

    public double ArrivalIdUs { get; set; }

    public bool IdPlausible { get; set; }

    public double[] DurUs { get; private set; }

    public double[] StartUs { get; private set; }

    public double[] EndUs { get; private set; }

    public bool Has(int stage) => stage < StartUs.Length && !double.IsPositiveInfinity(StartUs[stage]);

    public void AddSpan(int stage, double startUs, double durUs)
    {
        if (stage >= StartUs.Length)
        {
            Grow(stage + 1);
        }

        DurUs[stage] += durUs;
        if (startUs < StartUs[stage])
        {
            StartUs[stage] = startUs;
        }

        var end = startUs + durUs;
        if (end > EndUs[stage])
        {
            EndUs[stage] = end;
        }

        Touch(startUs, end, isSpan: true);
    }

    public void TouchInstant(double us) => Touch(us, us, isSpan: false);

    private void Touch(double startUs, double endUs, bool isSpan)
    {
        if (startUs < FirstAnyUs)
        {
            FirstAnyUs = startUs;
        }

        if (isSpan && startUs < FirstSpanUs)
        {
            FirstSpanUs = startUs;
        }

        if (endUs > LastEndUs)
        {
            LastEndUs = endUs;
        }
    }

    private void Grow(int needed)
    {
        var size = Math.Max(needed, StartUs.Length * 2);
        var oldLength = StartUs.Length;
        DurUs = Resize(DurUs, size, 0d);
        StartUs = Resize(StartUs, size, double.PositiveInfinity);
        EndUs = Resize(EndUs, size, double.NegativeInfinity);
        _ = oldLength;
    }

    private static double[] Resize(double[] source, int size, double fill)
    {
        var grown = new double[size];
        Array.Copy(source, grown, source.Length);
        Array.Fill(grown, fill, source.Length, size - source.Length);
        return grown;
    }
}

internal sealed class FrameTraceCapture
{
    public const int MaxRetainedAmbientEvents = 500_000;

    public long ParsedEvents { get; set; }

    public long SkippedEvents { get; set; }

    public bool SawMetadataBlock { get; set; }

    public bool SawOrigin { get; set; }

    public double QpcFrequency { get; set; }

    public long OriginQpc { get; set; }

    public string? StartedUtc { get; set; }

    public long? Capacity { get; set; }

    public long? Recorded { get; set; }

    public long? Retained { get; set; }

    public long Overwritten { get; set; }

    /// <summary>Events the recorder counted but never stored; zero in a dump from an app that predates the field.</summary>
    public long Abandoned { get; set; }

    public bool? Running { get; set; }

    public double MinUs { get; set; } = double.PositiveInfinity;

    public double MaxUs { get; set; } = double.NegativeInfinity;

    /// <summary>qpcFrequency and originQpc both present and usable, so a frame id can be placed on the trace time axis.</summary>
    public bool HasMetadata => SawMetadataBlock && SawOrigin && QpcFrequency > 0;

    public Dictionary<long, FrameAcc> Frames { get; } = new();

    public List<string> StageNames { get; } = new();

    public List<bool> StageAggregate { get; } = new();

    public Dictionary<string, int> StageIndexByName { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, AmbientAcc> Ambient { get; } = new(StringComparer.Ordinal);

    public List<AmbientEvent> AmbientEvents { get; } = new();

    public bool AmbientTruncated { get; set; }

    public Dictionary<string, int> Drops { get; } = new(StringComparer.Ordinal);

    public FrameAcc GetFrame(long id)
    {
        if (!Frames.TryGetValue(id, out var frame))
        {
            frame = new FrameAcc(id);
            Frames[id] = frame;
        }

        return frame;
    }

    public int StageIndexFor(NameInfo info)
    {
        if (info.StageIndex >= 0)
        {
            return info.StageIndex;
        }

        if (!StageIndexByName.TryGetValue(info.Name, out var index))
        {
            index = StageNames.Count;
            StageNames.Add(info.Name);
            StageAggregate.Add(info.IsAggregate);
            StageIndexByName[info.Name] = index;
        }

        info.StageIndex = index;
        return index;
    }

    public void AddAmbient(string name, double startUs, double endUs, string? text)
    {
        if (!Ambient.TryGetValue(name, out var acc))
        {
            acc = new AmbientAcc();
            Ambient[name] = acc;
        }

        var durUs = Math.Max(0d, endUs - startUs);
        acc.Count++;
        acc.TotalUs += durUs;
        if (durUs > acc.MaxUs)
        {
            acc.MaxUs = durUs;
        }

        if (AmbientEvents.Count < MaxRetainedAmbientEvents)
        {
            AmbientEvents.Add(new AmbientEvent(startUs, endUs, name, text));
        }
        else
        {
            AmbientTruncated = true;
        }
    }

    /// <summary>Distinct reasons kept per drop name; a trace with more folds the rest into "other" so memory stays bounded.</summary>
    public const int MaxDropReasonsPerName = 32;

    public Dictionary<string, Dictionary<string, int>> DropReasons { get; } = new(StringComparer.Ordinal);

    public void AddDrop(string name, string? reason)
    {
        Drops.TryGetValue(name, out var count);
        Drops[name] = count + 1;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        if (!DropReasons.TryGetValue(name, out var reasons))
        {
            reasons = new Dictionary<string, int>(StringComparer.Ordinal);
            DropReasons[name] = reasons;
        }

        var key = reason.Length > 64 ? reason[..64] : reason;
        if (!reasons.ContainsKey(key) && reasons.Count >= MaxDropReasonsPerName)
        {
            key = "other";
        }

        reasons.TryGetValue(key, out var reasonCount);
        reasons[key] = reasonCount + 1;
    }
}
