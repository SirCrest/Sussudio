using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiCaptureLastCallbackAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        builder.AppendLine(
            $"WASAPI Capture: callbacks={Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
    }
}
