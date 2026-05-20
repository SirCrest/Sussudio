using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var deviceAudioText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceAudio.cs").Replace("\r\n", "\n");

        AssertContains(deviceAudioText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(deviceAudioText, "public void ApplyDeviceAudioControlState()");
        AssertContains(deviceAudioText, "public void EnsureDeviceAudioModeSelection()");
        AssertDoesNotContain(controllerText, "public void ApplyDeviceAudioControlState()");

        return Task.CompletedTask;
    }
}
