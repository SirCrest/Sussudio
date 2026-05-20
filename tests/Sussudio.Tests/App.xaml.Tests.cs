using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task App_Xaml_WiresUnhandledExceptionPolicy()
    {
        var appType = RequireType("Sussudio.App");
        var appRootSource = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");
        var exceptionPolicySource = ReadRepoFile("Sussudio/App.ExceptionPolicy.cs")
            .Replace("\r\n", "\n");
        var launchLifecycleSource = ReadRepoFile("Sussudio/App.LaunchLifecycle.cs")
            .Replace("\r\n", "\n");

        // Verify crash handler methods exist on App
        var uiHandler = appType.GetMethod(
            "App_UnhandledException",
            BindingFlags.Instance | BindingFlags.NonPublic);
        AssertNotNull(uiHandler, "App.App_UnhandledException handler");

        var domainHandler = appType.GetMethod(
            "CurrentDomain_UnhandledException",
            BindingFlags.Instance | BindingFlags.NonPublic);
        AssertNotNull(domainHandler, "App.CurrentDomain_UnhandledException handler");

        // Verify recoverable-exception triage method exists
        var isRecoverable = appType.GetMethod(
            "IsRecoverableUnhandled",
            BindingFlags.Static | BindingFlags.NonPublic);
        AssertNotNull(isRecoverable, "App.IsRecoverableUnhandled triage");

        // Behavioral: OperationCanceledException should be recoverable
        var recoverable = (bool)isRecoverable!.Invoke(null, new object[] { new OperationCanceledException() })!;
        AssertEqual(true, recoverable, "OperationCanceledException is recoverable");

        // Behavioral: InvalidOperationException should NOT be recoverable
        var nonRecoverable = (bool)isRecoverable.Invoke(null, new object[] { new InvalidOperationException() })!;
        AssertEqual(false, nonRecoverable, "InvalidOperationException is not recoverable");

        AssertContains(appRootSource, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(appRootSource, "UnhandledException += App_UnhandledException;");
        AssertContains(appRootSource, "AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;");
        AssertContains(exceptionPolicySource, "private static bool IsRecoverableUnhandled(Exception ex)");
        AssertContains(exceptionPolicySource, "private void App_UnhandledException(");
        AssertContains(exceptionPolicySource, "private void CurrentDomain_UnhandledException(");
        AssertContains(exceptionPolicySource, "private void TryEmergencyStopRecording(string source)");
        AssertContains(exceptionPolicySource, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(launchLifecycleSource, "private const string SingleInstanceMutexName");
        AssertContains(launchLifecycleSource, "protected override void OnLaunched(");
        AssertContains(launchLifecycleSource, "SINGLE_INSTANCE_GUARD second instance detected");
        AssertContains(launchLifecycleSource, "\"APP_START \" +");
        AssertDoesNotContain(appRootSource, "private static bool IsRecoverableUnhandled(Exception ex)");
        AssertDoesNotContain(appRootSource, "protected override void OnLaunched(");

        return Task.CompletedTask;
    }
}
