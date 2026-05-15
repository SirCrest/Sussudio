using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelChecksAsync(List<CheckResult> results)
    {
        await AddRecordingModelLibAvSinkChecksAsync(results);
        await AddRecordingModelCaptureRuntimeChecksAsync(results);
        await AddRecordingModelRecordingContractChecksAsync(results);
        await AddRecordingModelCaptureSettingsChecksAsync(results);
        await AddRecordingModelFlashbackBufferChecksAsync(results);
        await AddRecordingModelRecordingContextChecksAsync(results);
        await AddRecordingModelDeviceModelChecksAsync(results);
        await AddRecordingModelMediaFormatChecksAsync(results);
        await AddRecordingModelAutomationContractChecksAsync(results);
        await AddRecordingModelRuntimePathChecksAsync(results);
        await AddRecordingModelSourceSignalTelemetryChecksAsync(results);
        await AddRecordingModelHdrOutputPolicyChecksAsync(results);
    }
}
