using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static class DiagnosticSessionJsonArtifacts
{
    internal static JsonElement CreateEmptyJsonObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    internal static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, ToolJsonOptions.Pretty);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    internal static object BuildFrameLedgerTrace(string sessionId, IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var events = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sample in samples)
        {
            if (!sample.Snapshot.TryGetProperty("FrameLedgerRecentEvents", out var recentEvents) ||
                recentEvents.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in recentEvents.EnumerateArray())
            {
                var key =
                    $"{Get(item, "SourceSequence")}|{Get(item, "Stage")}|{Get(item, "QpcTimestamp")}";
                if (seen.Add(key))
                {
                    events.Add(item.Clone());
                }
            }
        }

        return new
        {
            SessionId = sessionId,
            SampleCount = samples.Count,
            EventCount = events.Count,
            Events = events
        };
    }

    internal static int GetMaxSnapshotInt(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot,
        string propertyName)
    {
        var max = GetInt(lastSnapshot, propertyName);
        foreach (var sample in samples)
        {
            max = Math.Max(max, GetInt(sample.Snapshot, propertyName));
        }

        return max;
    }

    internal static bool TryGetSnapshot(JsonElement response, out JsonElement snapshot)
    {
        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Snapshot", out snapshot) &&
            snapshot.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        snapshot = default;
        return false;
    }

    internal static bool TryGetVerification(JsonElement response, out JsonElement verification)
    {
        verification = default;
        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("Verification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return response.TryGetProperty("Snapshot", out var snapshot) &&
               snapshot.ValueKind == JsonValueKind.Object &&
               snapshot.TryGetProperty("LastVerification", out verification) &&
               verification.ValueKind == JsonValueKind.Object;
    }
}
