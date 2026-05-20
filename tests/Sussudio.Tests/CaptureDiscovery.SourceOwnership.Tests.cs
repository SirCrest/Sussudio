using System.Threading.Tasks;
using System.Reflection;

static partial class Program
{
    internal static Task CaptureDiscoverySourceOwnership_LivesInFocusedPartials()
    {
        var deviceRootText = ReadRepoFile("Sussudio/Services/Capture/DeviceService.cs").Replace("\r\n", "\n");
        var deviceScoringText = ReadRepoFile("Sussudio/Services/Capture/DeviceService.Scoring.cs").Replace("\r\n", "\n");
        var sourceReaderRootText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs").Replace("\r\n", "\n");
        var sourceReaderNegotiationText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs").Replace("\r\n", "\n");
        var sourceReaderConvertedMediaTypeText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ConvertedMediaType.cs").Replace("\r\n", "\n");
        var sourceReaderDeviceEnumerationText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.DeviceEnumeration.cs").Replace("\r\n", "\n");
        var sourceReaderInteropText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Interop.cs").Replace("\r\n", "\n");
        var sourceReaderComContractsText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs").Replace("\r\n", "\n");
        var sourceReaderSampleBufferContractsText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.SampleBufferContracts.cs").Replace("\r\n", "\n");
        var mfInteropHelpersText = ReadRepoFile("Sussudio/Services/Capture/MfInteropHelpers.cs").Replace("\r\n", "\n");

        AssertContains(deviceRootText, "var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);");
        AssertContains(deviceRootText, "public async Task<DeviceDiscoveryResult> EnumerateCaptureDeviceDiscoveryAsync(");
        AssertContains(deviceRootText, "public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(");
        AssertContains(deviceRootText, "return discovery.CaptureDevices;");
        AssertContains(deviceRootText, "var audioTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();");
        AssertContains(deviceRootText, "return new DeviceDiscoveryResult(discovered, audioDevices);");
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
        AssertContains(sourceReaderDeviceEnumerationText, "private IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(sourceReaderDeviceEnumerationText, "MfInterop.MFEnumDeviceSources(attrs, out activateArrayPtr, out var activateCount)");
        AssertContains(sourceReaderDeviceEnumerationText, "MfInteropHelpers.MatchesSymbolicLink(targetSymbolicLink, link)");
        AssertContains(mfInteropHelpersText, "public static bool MatchesSymbolicLink(string? target, string? candidate)");
        AssertContains(sourceReaderDeviceEnumerationText, "ReleaseRemainingActivateObjects(activateArrayPtr, activateCount, i + 1);");
        AssertContains(sourceReaderDeviceEnumerationText, "Marshal.ReleaseComObject(activated)");
        AssertContains(sourceReaderDeviceEnumerationText, "Marshal.FreeCoTaskMem(activateArrayPtr);");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectMediaType(");
        AssertDoesNotContain(sourceReaderNegotiationText, "private IMFMediaType SelectConvertedMediaType(");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameSize(");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameRate(");
        AssertContains(sourceReaderConvertedMediaTypeText, "private IMFMediaType SelectConvertedMediaType(");
        AssertContains(sourceReaderConvertedMediaTypeText, "SelectMediaType(");
        AssertContains(sourceReaderConvertedMediaTypeText, "IMFMediaType.SetGUID(MF_MT_SUBTYPE");
        AssertContains(sourceReaderConvertedMediaTypeText, "private static void CopyOptionalUInt64(");
        AssertContains(sourceReaderConvertedMediaTypeText, "private static void CopyOptionalUInt32(");
        AssertContains(sourceReaderInteropText, "private static class MfInterop");
        AssertContains(sourceReaderInteropText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");
        AssertContains(sourceReaderInteropText, "private static class MfConstants");
        AssertContains(sourceReaderInteropText, "private static class MfHResults");
        AssertContains(sourceReaderInteropText, "private static class MfGuids");
        AssertDoesNotContain(sourceReaderInteropText, "internal interface IMFSourceReader");
        AssertDoesNotContain(sourceReaderInteropText, "internal interface IMFMediaBuffer");
        AssertDoesNotContain(sourceReaderInteropText, "internal interface IMFDXGIBuffer");
        AssertContains(sourceReaderComContractsText, "internal interface IMFSourceReader");
        AssertDoesNotContain(sourceReaderComContractsText, "internal interface IMFMediaBuffer");
        AssertDoesNotContain(sourceReaderComContractsText, "internal interface IMFDXGIBuffer");
        AssertDoesNotContain(sourceReaderComContractsText, "internal interface IMFSample");
        AssertContains(sourceReaderSampleBufferContractsText, "internal interface IMFMediaBuffer");
        AssertContains(sourceReaderSampleBufferContractsText, "internal interface IMFDXGIBuffer");
        AssertContains(sourceReaderSampleBufferContractsText, "internal interface IMFSample");
        AssertContains(sourceReaderSampleBufferContractsText, "Flattened IMFSample COM interface");
        AssertContains(sourceReaderSampleBufferContractsText, "does NOT use C# interface inheritance");
        AssertContains(sourceReaderSampleBufferContractsText, "[PreserveSig] int _Attr_GetItem(ref Guid guidKey, IntPtr pValue);");
        AssertContains(sourceReaderSampleBufferContractsText, "int GetSampleTime(out long phnsSampleTime);");
        AssertDoesNotContain(sourceReaderComContractsText, "DllImport(");
        AssertDoesNotContain(sourceReaderComContractsText, "private static class MfInterop");
        AssertDoesNotContain(sourceReaderSampleBufferContractsText, "DllImport(");
        AssertDoesNotContain(sourceReaderSampleBufferContractsText, "private static class MfInterop");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaSource CreateMediaSource(");
        AssertDoesNotContain(sourceReaderNegotiationText, "MFEnumDeviceSources(attrs, out activateArrayPtr, out var activateCount)");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaType SelectMediaType(");
        AssertDoesNotContain(sourceReaderRootText, "private static class MfInterop");
        AssertDoesNotContain(sourceReaderRootText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");

        var matches = RequireType("Sussudio.Services.Capture.MfInteropHelpers")
            .GetMethod("MatchesSymbolicLink", BindingFlags.Static | BindingFlags.Public)
            ?? throw new System.InvalidOperationException("MfInteropHelpers.MatchesSymbolicLink was not found.");
        AssertEqual(true, (bool)matches.Invoke(null, new object?[] { "DEVICE_A", "device_a" })!, "symbolic-link exact case-insensitive match");
        AssertEqual(true, (bool)matches.Invoke(null, new object?[] { "core", "PREFIX-core-SUFFIX" })!, "symbolic-link candidate contains target");
        AssertEqual(true, (bool)matches.Invoke(null, new object?[] { "PREFIX-core-SUFFIX", "core" })!, "symbolic-link target contains candidate");
        AssertEqual(false, (bool)matches.Invoke(null, new object?[] { "abc", "xyz" })!, "symbolic-link mismatch");
        AssertEqual(false, (bool)matches.Invoke(null, new object?[] { "", "anything" })!, "symbolic-link empty target");
        AssertEqual(false, (bool)matches.Invoke(null, new object?[] { "anything", null })!, "symbolic-link null candidate");

        return Task.CompletedTask;
    }
}
