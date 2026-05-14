using System.Threading.Tasks;

static partial class Program
{
    private static Task MfDeviceEnumerator_SourceOwnershipLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/MfDeviceEnumerator.cs");
        var videoDevicesText = ReadRepoFile("Sussudio/Services/Capture/MfDeviceEnumerator.VideoDevices.cs");
        var audioEndpointsText = ReadRepoFile("Sussudio/Services/Capture/MfDeviceEnumerator.AudioEndpoints.cs");
        var formatProbeText = ReadRepoFile("Sussudio/Services/Capture/MfDeviceEnumerator.FormatProbe.cs");

        AssertContains(rootText, "internal static partial class MfDeviceEnumerator");
        AssertContains(rootText, "private static extern int MFCreateAttributes(");
        AssertContains(rootText, "private static extern int MFCreateSourceReaderFromMediaSource(");
        AssertDoesNotContain(rootText, "EnumerateVideoDevicesAsync");
        AssertDoesNotContain(rootText, "EnumerateAudioCaptureEndpointsAsync");
        AssertDoesNotContain(rootText, "ProbeVideoFormatsAsync");
        AssertDoesNotContain(rootText, "ReadAudioEndpointFriendlyName");
        AssertDoesNotContain(rootText, "SubtypeGuidToName");

        AssertContains(videoDevicesText, "public static Task<List<MfVideoDeviceInfo>> EnumerateVideoDevicesAsync()");
        AssertContains(videoDevicesText, "MF video device enumeration failed");
        AssertContains(videoDevicesText, "MFEnumDeviceSources(attributes, out activateArray, out var activateCount)");

        AssertContains(audioEndpointsText, "public static Task<List<AudioInputDevice>> EnumerateAudioCaptureEndpointsAsync()");
        AssertContains(audioEndpointsText, "ReadAudioEndpointFriendlyName(endpoint, endpointId)");
        AssertContains(audioEndpointsText, "private static string ReadAudioEndpointFriendlyName(");

        AssertContains(formatProbeText, "public static Task<List<MediaFormat>> ProbeVideoFormatsAsync(string symbolicLink)");
        AssertContains(formatProbeText, "private static IMFMediaSource CreateMediaSource(string symbolicLink)");
        AssertContains(formatProbeText, "private static IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(formatProbeText, "private static string SubtypeGuidToName(Guid subtype)");

        return Task.CompletedTask;
    }
}
