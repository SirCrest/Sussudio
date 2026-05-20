using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureRuntimeSnapshot ApplyVerificationProfile(
        CaptureRuntimeSnapshot runtimeSnapshot,
        string filePath,
        string? verificationProfile)
    {
        if (!string.Equals(verificationProfile, "flashback-export", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(runtimeSnapshot.FlashbackExportVerificationFormat))
        {
            return runtimeSnapshot;
        }

        return new CaptureRuntimeSnapshot
        {
            TimestampUtc = runtimeSnapshot.TimestampUtc,
            RequestedWidth = runtimeSnapshot.RequestedWidth,
            RequestedHeight = runtimeSnapshot.RequestedHeight,
            RequestedFrameRate = runtimeSnapshot.RequestedFrameRate,
            RequestedFrameRateArg = runtimeSnapshot.RequestedFrameRateArg,
            RequestedFrameRateNumerator = runtimeSnapshot.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = runtimeSnapshot.RequestedFrameRateDenominator,
            RequestedFormat = runtimeSnapshot.RequestedFormat,
            RequestedHdrEnabled = runtimeSnapshot.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = runtimeSnapshot.RequestedHdrMasteringMetadata,
            HdrOutputActive = runtimeSnapshot.HdrOutputActive,
            HdrAutoDowngraded = runtimeSnapshot.HdrAutoDowngraded,
            NegotiatedWidth = runtimeSnapshot.NegotiatedWidth,
            NegotiatedHeight = runtimeSnapshot.NegotiatedHeight,
            NegotiatedFrameRate = runtimeSnapshot.NegotiatedFrameRate,
            NegotiatedFrameRateArg = runtimeSnapshot.NegotiatedFrameRateArg,
            NegotiatedFrameRateNumerator = runtimeSnapshot.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = runtimeSnapshot.NegotiatedFrameRateDenominator,
            FlashbackExportOutputPath = filePath,
            FlashbackExportVerificationFormat = runtimeSnapshot.FlashbackExportVerificationFormat,
            FlashbackCodecDowngradeReason = runtimeSnapshot.FlashbackCodecDowngradeReason
        };
    }
}
