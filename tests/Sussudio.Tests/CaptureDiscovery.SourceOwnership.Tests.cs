using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureDiscoverySourceOwnership_LivesInFocusedPartials()
    {
        var deviceRootText = ReadRepoFile("Sussudio/Services/Capture/DeviceService.cs").Replace("\r\n", "\n");
        var deviceScoringText = ReadRepoFile("Sussudio/Services/Capture/DeviceService.Scoring.cs").Replace("\r\n", "\n");
        var sourceReaderRootText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs").Replace("\r\n", "\n");
        var sourceReaderNegotiationText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs").Replace("\r\n", "\n");
        var sourceReaderInteropText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Interop.cs").Replace("\r\n", "\n");

        AssertContains(deviceRootText, "var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);");
        AssertContains(deviceRootText, "foreach (var candidate in selected.OrderByDescending(GetDevicePriority))");
        AssertContains(deviceScoringText, "private static int GetDevicePriority(DeviceCandidate candidate)");
        AssertContains(deviceScoringText, "if (candidate.PreferredByName) priority += 400;");
        AssertContains(deviceScoringText, "if (candidate.LikelyByCapability) priority += 200;");
        AssertContains(deviceScoringText, "if (candidate.HasEnumeratedFormats) priority += 50;");
        AssertContains(deviceScoringText, "private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)");
        AssertDoesNotContain(deviceRootText, "private static int GetDevicePriority(DeviceCandidate candidate)");
        AssertDoesNotContain(deviceRootText, "private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)");
        AssertDoesNotContain(deviceRootText, "priority += 400");

        AssertContains(sourceReaderNegotiationText, "private bool TrySetSourceReaderD3DManager(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaSource CreateMediaSource(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectMediaType(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectConvertedMediaType(");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameSize(");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameRate(");
        AssertContains(sourceReaderNegotiationText, "private static void CopyOptionalUInt64(");
        AssertContains(sourceReaderInteropText, "private static class MfInterop");
        AssertContains(sourceReaderInteropText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");
        AssertContains(sourceReaderInteropText, "internal interface IMFSourceReader");
        AssertContains(sourceReaderInteropText, "internal interface IMFMediaBuffer");
        AssertContains(sourceReaderInteropText, "internal interface IMFDXGIBuffer");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaSource CreateMediaSource(");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaType SelectMediaType(");
        AssertDoesNotContain(sourceReaderRootText, "private static class MfInterop");
        AssertDoesNotContain(sourceReaderRootText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");

        return Task.CompletedTask;
    }
}
