using System.Threading.Tasks;
using System.Reflection;

static partial class Program
{
    internal static Task MfDeviceEnumerator_SourceOwnershipLivesInCohesiveEnumerator()
    {
        var rootText = ReadMfDeviceEnumeratorFile("MfDeviceEnumerator.cs");

        AssertContains(rootText, "internal static class MfDeviceEnumerator");
        AssertDoesNotContain(rootText, "partial class MfDeviceEnumerator");
        AssertContains(rootText, "private static extern int MFCreateAttributes(");
        AssertContains(rootText, "private static extern int MFCreateSourceReaderFromMediaSource(");
        AssertContains(rootText, "public static Task<List<MfVideoDeviceInfo>> EnumerateVideoDevicesAsync()");
        AssertContains(rootText, "MF video device enumeration failed");
        AssertContains(rootText, "MFEnumDeviceSources(attributes, out activateArray, out var activateCount)");
        AssertContains(rootText, "public static Task<List<AudioInputDevice>> EnumerateAudioCaptureEndpointsAsync()");
        AssertContains(rootText, "ReadAudioEndpointFriendlyName(endpoint, endpointId)");
        AssertContains(rootText, "private static string ReadAudioEndpointFriendlyName(");
        AssertContains(rootText, "public static Task<List<MediaFormat>> ProbeVideoFormatsAsync(string symbolicLink)");
        AssertContains(rootText, "private static string SubtypeGuidToName(Guid subtype)");
        AssertContains(rootText, "private static IMFMediaSource CreateMediaSource(string symbolicLink)");
        AssertContains(rootText, "private static IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(rootText, "MfInteropHelpers.MatchesSymbolicLink(targetSymbolicLink, candidateLink)");
        AssertContains(rootText, "MFCreateDeviceSource(attributes, out var mediaSource)");
        foreach (var removedFile in new[]
        {
            "MfDeviceEnumerator.VideoDevices.cs",
            "MfDeviceEnumerator.AudioEndpoints.cs",
            "MfDeviceEnumerator.FormatProbe.cs",
            "MfDeviceEnumerator.SourceOpening.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "DeviceDiscovery", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    internal static Task CaptureDiscoverySourceOwnership_LivesInFocusedPartials()
    {
        var deviceRootText = ReadRepoFile("Sussudio/Services/Capture/DeviceService.cs").Replace("\r\n", "\n");
        var sourceReaderRootText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs").Replace("\r\n", "\n");
        var sourceReaderNegotiationText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs").Replace("\r\n", "\n");
        var sourceReaderDeviceEnumerationText = sourceReaderNegotiationText;
        var sourceReaderComContractsText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs").Replace("\r\n", "\n");
        var mfInteropHelpersText = ReadRepoFile("Sussudio/Services/Capture/MfInteropHelpers.cs").Replace("\r\n", "\n");

        AssertContains(deviceRootText, "var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);");
        AssertContains(deviceRootText, "public async Task<DeviceDiscoveryResult> EnumerateCaptureDeviceDiscoveryAsync(");
        AssertContains(deviceRootText, "public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(");
        AssertContains(deviceRootText, "return discovery.CaptureDevices;");
        AssertContains(deviceRootText, "var audioTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();");
        AssertContains(deviceRootText, "return new DeviceDiscoveryResult(discovered, audioDevices);");
        AssertContains(deviceRootText, "foreach (var candidate in selected.OrderByDescending(GetDevicePriority))");
        AssertContains(deviceRootText, "private static int GetDevicePriority(DeviceCandidate candidate)");
        AssertContains(deviceRootText, "if (candidate.PreferredByName) priority += 400;");
        AssertContains(deviceRootText, "if (candidate.LikelyByCapability) priority += 200;");
        AssertContains(deviceRootText, "if (candidate.HasEnumeratedFormats) priority += 50;");
        AssertContains(deviceRootText, "private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)");

        AssertContains(sourceReaderNegotiationText, "private bool TrySetSourceReaderD3DManager(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaSource CreateMediaSource(");
        AssertContains(sourceReaderDeviceEnumerationText, "private IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(sourceReaderDeviceEnumerationText, "MfInterop.MFEnumDeviceSources(attrs, out activateArrayPtr, out var activateCount)");
        AssertContains(sourceReaderDeviceEnumerationText, "MfInteropHelpers.MatchesSymbolicLink(targetSymbolicLink, link)");
        AssertContains(mfInteropHelpersText, "public static bool MatchesSymbolicLink(string? target, string? candidate)");
        AssertContains(sourceReaderDeviceEnumerationText, "ReleaseRemainingActivateObjects(activateArrayPtr, activateCount, i + 1);");
        AssertContains(sourceReaderDeviceEnumerationText, "Marshal.ReleaseComObject(activated)");
        AssertContains(sourceReaderDeviceEnumerationText, "Marshal.FreeCoTaskMem(activateArrayPtr);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.DeviceEnumeration.cs")),
            "source-reader device enumeration fallback folded into negotiation/source-open owner");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectMediaType(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectConvertedMediaType(");
        AssertContains(sourceReaderNegotiationText, "SelectMediaType(");
        AssertContains(sourceReaderNegotiationText, "IMFMediaType.SetGUID(MF_MT_SUBTYPE");
        AssertContains(sourceReaderNegotiationText, "private static void CopyOptionalUInt64(");
        AssertContains(sourceReaderNegotiationText, "private static void CopyOptionalUInt32(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.ConvertedMediaType.cs")),
            "converted source-reader media type construction folded into negotiation owner");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameSize(");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameRate(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Interop.cs")),
            "source-reader MF P/Invokes and constants folded into COM contract owner");
        AssertContains(sourceReaderComContractsText, "private static class MfInterop");
        AssertContains(sourceReaderComContractsText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");
        AssertContains(sourceReaderComContractsText, "private static class MfConstants");
        AssertContains(sourceReaderComContractsText, "private static class MfHResults");
        AssertContains(sourceReaderComContractsText, "private static class MfGuids");
        AssertContains(sourceReaderComContractsText, "internal interface IMFSourceReader");
        AssertContains(sourceReaderComContractsText, "internal interface IMFMediaBuffer");
        AssertContains(sourceReaderComContractsText, "internal interface IMFDXGIBuffer");
        AssertContains(sourceReaderComContractsText, "internal interface IMFSample");
        AssertContains(sourceReaderComContractsText, "Flattened IMFSample COM interface");
        AssertContains(sourceReaderComContractsText, "does NOT use C# interface inheritance");
        AssertContains(sourceReaderComContractsText, "[PreserveSig] int _Attr_GetItem(ref Guid guidKey, IntPtr pValue);");
        AssertContains(sourceReaderComContractsText, "int GetSampleTime(out long phnsSampleTime);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.SampleBufferContracts.cs")),
            "source-reader sample/buffer COM declarations folded into COM contract owner");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaSource CreateMediaSource(");
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

    private static string ReadMfDeviceEnumeratorFile(string fileName) =>
        ReadRepoFile($"Sussudio/Services/Capture/DeviceDiscovery/{fileName}");
}
