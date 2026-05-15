using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddRecordingModelCaptureSettingsChecksAsync(List<CheckResult> results)
    {
        // --- CaptureSettings ---
        await AddCheckAsync(results,
            "Capture mode options preserve display text and metadata",
            CaptureModeOptions_PreserveDisplayTextAndMetadata);
        await AddCheckAsync(results,
            "Capture mode options builder builds resolution and video format options",
            CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions);
        await AddCheckAsync(results,
            "Capture settings defaults preserve output and pipeline contracts",
            CaptureSettings_DefaultsAndOutputContracts);
        await AddCheckAsync(results,
            "Capture settings MJPEG HFR mode handles force case and instance state",
            CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState);
        await AddCheckAsync(results,
            "Encoder support computes availability and preferred encoders",
            EncoderSupport_ComputesAvailabilityAndPreferredEncoders);
        await AddCheckAsync(results,
            "GetTargetBitrate scales by resolution and frame rate",
            CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate);
        await AddCheckAsync(results,
            "GetTargetBitrate applies codec efficiency for HEVC and AV1",
            CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency);
        await AddCheckAsync(results,
            "GetTargetBitrate clamps custom quality to range",
            CaptureSettings_GetTargetBitrate_ClampsCustomQuality);
        await AddCheckAsync(results,
            "GetOutputFileName includes format suffix",
            CaptureSettings_GetOutputFileName_IncludesFormatSuffix);
        await AddCheckAsync(results,
            "MJPEG HFR mode requires SDR and MJPG pixel format",
            CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat);
    }
}
