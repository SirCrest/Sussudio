using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelCaptureRuntimeChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MJPG HFR mode only activates for SDR 4K120-style settings",
            CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest);
        await AddCheckAsync(results,
            "Strict HFR fatal handler clears active session state",
            CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState);
        await AddCheckAsync(results,
            "Capture errors refresh ViewModel runtime flags",
            CaptureErrors_RefreshViewModelRuntimeFlags);
    }
}
