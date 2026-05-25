using System.Threading.Tasks;

static partial class Program
{
    internal static Task KsAudioNodeProbe_SourceOwnership_IsConsolidated()
    {
        var programText = ReadRepoFile("tools/KsAudioNodeProbe/Program.cs");
        var scanWorkflowsText = ReadRepoFile("tools/KsAudioNodeProbe/Program.ScanWorkflows.cs");

        AssertContains(programText, "using static KsAudioNodeProbeNative;");
        AssertContains(programText, "KsAudioNodeProbeScanWorkflows.RunSetAndHold(handle)");
        AssertContains(programText, "KsAudioNodeProbeScanWorkflows.RunFullProbe(handle)");
        AssertContains(programText, "static class KsAudioNodeProbeNative");
        AssertContains(programText, "private const uint IoctlKsProperty = 0x002F0003;");
        AssertContains(programText, "private const int ErrorMoreData = 234;");
        AssertContains(programText, "public static List<string> EnumerateKsInterfaces");
        AssertContains(programText, "private static extern bool DeviceIoControl");
        AssertContains(programText, "private struct KsProperty");
        AssertContains(programText, "private struct SP_DEVICE_INTERFACE_DETAIL_DATA");
        AssertDoesNotContain(programText, "var anyHit = false");
        AssertDoesNotContain(programText, "== Extended node tests ==");
        AssertDoesNotContain(programText, "== ADC volume probe ==");
        AssertContains(scanWorkflowsText, "static class KsAudioNodeProbeScanWorkflows");
        AssertDoesNotContain(scanWorkflowsText, "static partial class KsAudioNodeProbeScanWorkflows");
        AssertContains(scanWorkflowsText, "public static int RunSetAndHold(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "public static void RunFullProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void EnumerateTopologyNodes(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunBruteForceNodePropertyScan(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunExtendedNodeTests(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunExtendedSetTest(");
        AssertContains(scanWorkflowsText, "private static void RunAdcVolumeProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunMuxProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunMuteProbe(SafeFileHandle handle)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "KsAudioNodeProbe", "Program.ScanWorkflows.Extended.cs")),
            "KS audio node scan workflow probes live with the main scan workflow owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "KsAudioNodeProbe", "Program.NativeInterop.cs")),
            "KS audio node probe private interop declarations live with the command entry point");

        return Task.CompletedTask;
    }

    internal static Task EgavdsAudioProbe_SourceOwnership_IsConsolidated()
    {
        var programText = ReadRepoFile("tools/EgavdsAudioProbe/Program.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(programText, "static class EgavdsProbe");
        AssertDoesNotContain(programText, "static partial class EgavdsProbe");
        AssertContains(programText, "static string? FindElgato4KXDevicePath()");
        AssertContains(programText, "EGAVDS_SetAudioInputSelection(handleRef, targetInput)");
        AssertContains(programText, "EGAVDS_SetLineInAudioGain(handleRef, setGain.Value)");
        AssertContains(programText, "private const string DLL = \"EGAVDeviceSupport\"");
        AssertContains(programText, "private static void RegisterSwigCallbacks()");
        AssertContains(programText, "SWIGRegisterExceptionCallbacks_EGAVDS");
        AssertContains(programText, "private static extern int EGAVDS_OpenDevice");
        AssertContains(programText, "private static extern bool SetupDiEnumDeviceInterfaces");
        AssertContains(programText, "private struct SP_DEVICE_INTERFACE_DATA");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "EgavdsAudioProbe", "Program.NativeInterop.cs")),
            "EGAVDS probe private interop declarations live with the probe command flow");
        AssertContains(agentMapText, "`tools/EgavdsAudioProbe/Program.cs` owns EGAVDS audio probe command flow,");
        AssertDoesNotContain(agentMapText, "`Program.NativeInterop.cs` owns EGAVDS");
        AssertDoesNotContain(cleanupPlanText, "`tools/EgavdsAudioProbe/Program.NativeInterop.cs`");

        return Task.CompletedTask;
    }
}
