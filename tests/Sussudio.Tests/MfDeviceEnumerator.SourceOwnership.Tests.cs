using System.Threading.Tasks;

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
                System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "DeviceDiscovery", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    private static string ReadMfDeviceEnumeratorFile(string fileName) =>
        ReadRepoFile($"Sussudio/Services/Capture/DeviceDiscovery/{fileName}");
}
