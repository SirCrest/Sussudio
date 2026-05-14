using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendAvSyncSection(StringBuilder builder, JsonElement snapshot)
    {
        var avSyncDrift = Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorrectionSamples = Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
        if (string.IsNullOrWhiteSpace(avSyncDrift) && string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== AV Sync ==");
        builder.AppendLine(
            $"Capture Drift: {(string.IsNullOrWhiteSpace(avSyncDrift) ? "N/A" : avSyncDrift + "ms")} | " +
            $"Rate: {(string.IsNullOrWhiteSpace(avSyncRate) ? "N/A" : avSyncRate + "ms/s")}");
        if (string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

        builder.AppendLine(
            $"Encoder Drift: {avSyncEncoder}ms | " +
            $"Correction Samples: {(string.IsNullOrWhiteSpace(avSyncCorrectionSamples) ? "N/A" : avSyncCorrectionSamples)}");
    }
}
