using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeFormatting;

sealed record AtReadResult(string Label, byte[]? Payload, string DisplayValue, object? TypedValue);

sealed record ChangedValue(string Label, string Before, string After);

sealed class ExperimentResult
{
    public ExperimentResult(SetExperiment experiment, bool writeOk, IReadOnlyDictionary<int, AtReadResult> before, IReadOnlyDictionary<int, AtReadResult> after)
    {
        Experiment = experiment;
        WriteOk = writeOk;
        ChangedValues = before.Keys
            .Where(key => before[key].DisplayValue != after[key].DisplayValue || !AreEqual(before[key].Payload, after[key].Payload))
            .Select(key => new ChangedValue(before[key].Label, before[key].DisplayValue, after[key].DisplayValue))
            .ToArray();
    }

    public SetExperiment Experiment { get; }
    public bool WriteOk { get; }
    public IReadOnlyList<ChangedValue> ChangedValues { get; }
    public bool HasAnyChange => ChangedValues.Count > 0;

    private static bool AreEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}

static partial class NativeXuProbeDefaultExperiment
{
    private static async Task<Dictionary<int, AtReadResult>> ReadAllAsync(CaptureDevice device, IEnumerable<GetterSpec> specs)
    {
        var results = new Dictionary<int, AtReadResult>();
        foreach (var spec in specs)
        {
            var payload = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, spec.Cmd, spec.Name);
            results[spec.Cmd] = Decode(spec, payload);
        }

        return results;
    }

    private static AtReadResult Decode(GetterSpec spec, byte[]? payload)
    {
        if (payload == null || payload.Length == 0)
        {
            return new AtReadResult(spec.Name, payload, "unavailable", null);
        }

        return spec.Kind switch
        {
            ValueKind.Byte => new AtReadResult(spec.Name, payload, payload[0].ToString(CultureInfo.InvariantCulture), payload[0]),
            ValueKind.Int16 when payload.Length >= 2 => new AtReadResult(spec.Name, payload, BitConverter.ToInt16(payload, 0).ToString(CultureInfo.InvariantCulture), BitConverter.ToInt16(payload, 0)),
            ValueKind.Int32 when payload.Length >= 4 => new AtReadResult(spec.Name, payload, BitConverter.ToInt32(payload, 0).ToString(CultureInfo.InvariantCulture), BitConverter.ToInt32(payload, 0)),
            _ => new AtReadResult(spec.Name, payload, BitConverter.ToString(payload), null)
        };
    }

    private static void PrintReads(string title, IReadOnlyDictionary<int, AtReadResult> reads)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title} ==");
        foreach (var item in reads.Values)
        {
            Console.WriteLine($"{item.Label}: {item.DisplayValue} raw={FormatRaw(item.Payload)}");
        }
    }

    private static void PrintDiff(IReadOnlyDictionary<int, AtReadResult> before, IReadOnlyDictionary<int, AtReadResult> after)
    {
        foreach (var key in before.Keys.OrderBy(k => k))
        {
            var left = before[key];
            var right = after[key];
            if (left.DisplayValue != right.DisplayValue || !RawEqual(left.Payload, right.Payload))
            {
                Console.WriteLine($"  {left.Label}: {left.DisplayValue} -> {right.DisplayValue} ({FormatRaw(left.Payload)} -> {FormatRaw(right.Payload)})");
            }
        }
    }

    private static void PrintInterestingChanges(IReadOnlyCollection<ExperimentResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("== Interesting changes ==");
        foreach (var result in results.Where(r => r.HasAnyChange))
        {
            Console.WriteLine($"{result.Experiment.Setter.Name} -> {result.Experiment.DisplayValue} (write {(result.WriteOk ? "ok" : "failed")})");
            foreach (var changed in result.ChangedValues)
            {
                Console.WriteLine($"  {changed.Label}: {changed.Before} -> {changed.After}");
            }
        }

        if (results.All(r => !r.HasAnyChange))
        {
            Console.WriteLine("No getter-visible changes were observed from the current candidate payload set.");
        }
    }

    private static bool RawEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void PrintSnapshot(string title, SourceSignalTelemetrySnapshot snapshot)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title} ==");
        Console.WriteLine($"Availability: {snapshot.Availability}");
        Console.WriteLine($"Origin: {snapshot.Origin} ({snapshot.Confidence})");
        Console.WriteLine($"Source video: {snapshot.Width}x{snapshot.Height} @ {snapshot.FrameRateExact:0.###}");
        Console.WriteLine($"HDR: {snapshot.IsHdr}");
        Console.WriteLine($"Video format: {snapshot.VideoFormat}");
        Console.WriteLine($"Audio format: {snapshot.AudioFormat}");
        Console.WriteLine($"Audio sample rate: {snapshot.AudioSampleRate}");
        Console.WriteLine($"Input source: {snapshot.InputSource}");
        Console.WriteLine($"Diagnostic: {snapshot.DiagnosticSummary}");
    }
}
