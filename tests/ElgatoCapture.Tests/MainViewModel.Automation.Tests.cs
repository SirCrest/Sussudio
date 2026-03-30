using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook()
    {
        var vmType = RequireType("ElgatoCapture.ViewModels.MainViewModel");

        // SavePreviewVolume must exist as the persistence hook
        var savePreviewVolume = vmType.GetMethod(
            "SavePreviewVolume",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        AssertNotNull(savePreviewVolume, "MainViewModel.SavePreviewVolume");

        // PreviewVolume observable property must exist
        var previewVolume = vmType.GetProperty("PreviewVolume", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(previewVolume, "MainViewModel.PreviewVolume");

        // ShowAllCaptureOptions observable property must exist
        var showAll = vmType.GetProperty("ShowAllCaptureOptions", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(showAll, "MainViewModel.ShowAllCaptureOptions");

        // IsAudioPreviewEnabled observable property must exist
        var audioPreview = vmType.GetProperty("IsAudioPreviewEnabled", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(audioPreview, "MainViewModel.IsAudioPreviewEnabled");

        // Automation interface method must exist
        var getOptionsSnapshot = vmType.GetMethod(
            "GetAutomationOptionsSnapshotAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(getOptionsSnapshot, "MainViewModel.GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }
}
