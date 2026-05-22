using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");

        AssertContains(controllerText, "internal sealed class CaptureSelectionBindingController");
        AssertContains(controllerText, "public void ApplyDeviceAudioControlState()");
        AssertContains(controllerText, "public void EnsureDeviceAudioModeSelection()");

        return Task.CompletedTask;
    }
}
