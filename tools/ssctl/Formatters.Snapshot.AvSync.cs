using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotAvSyncSection(StringBuilder builder, JsonElement snapshot)
    {
        var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorr = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
        if (string.IsNullOrWhiteSpace(avSyncDrift) && string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== AV Sync ==");
        builder.AppendLine(
            $"Capture Drift: {(string.IsNullOrWhiteSpace(avSyncDrift) ? "N/A" : avSyncDrift + "ms")} | " +
            $"Rate: {(string.IsNullOrWhiteSpace(avSyncRate) ? "N/A" : avSyncRate + "ms/s")}");
        if (!string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            builder.AppendLine(
                $"Encoder Drift: {avSyncEncoder}ms | " +
                $"Correction Samples: {(string.IsNullOrWhiteSpace(avSyncCorr) ? "N/A" : avSyncCorr)}");
        }
    }
}
