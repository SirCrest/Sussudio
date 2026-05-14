using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModel_UsesDependencyCompositionSeam()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var dependenciesText = ReadRepoFile("Sussudio/ViewModels/MainViewModelDependencies.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "public MainViewModel()\n        : this(MainViewModelDependencies.CreateDefault())");
        AssertContains(rootText, "internal MainViewModel(MainViewModelDependencies dependencies)");
        AssertContains(rootText, "_deviceService = dependencies.DeviceService;");
        AssertContains(rootText, "_captureService = dependencies.CaptureService;");
        AssertContains(rootText, "_sessionCoordinator = dependencies.SessionCoordinator;");
        AssertContains(rootText, "_deviceAudioControlService = dependencies.DeviceAudioControlService;");
        AssertContains(rootText, "_dispatcherQueue = dependencies.DispatcherQueue;");
        AssertContains(rootText, "_audioDeviceWatcher = dependencies.AudioDeviceWatcher;");
        AssertDoesNotContain(rootText, "_deviceService = new DeviceService();");
        AssertDoesNotContain(rootText, "_captureService = new CaptureService();");
        AssertDoesNotContain(rootText, "_sessionCoordinator = new CaptureSessionCoordinator(_captureService);");
        AssertDoesNotContain(rootText, "_deviceAudioControlService = new NativeXuAudioControlService();");
        AssertDoesNotContain(rootText, "_audioDeviceWatcher = new AudioDeviceWatcher();");

        AssertContains(dependenciesText, "internal sealed class MainViewModelDependencies");
        AssertContains(dependenciesText, "public static MainViewModelDependencies CreateDefault()");
        AssertContains(dependenciesText, "var captureService = new CaptureService();");
        AssertContains(dependenciesText, "new CaptureSessionCoordinator(captureService)");
        AssertContains(dependenciesText, "DispatcherQueue.GetForCurrentThread()");
        AssertContains(dependenciesText, "new AudioDeviceWatcher()");

        return Task.CompletedTask;
    }
}
