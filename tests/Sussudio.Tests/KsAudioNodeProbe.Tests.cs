using System.Threading.Tasks;

static partial class Program
{
    private static Task KsAudioNodeProbe_SourceOwnership_IsSplit()
    {
        var programText = ReadRepoFile("tools/KsAudioNodeProbe/Program.cs");
        var constantsText = ReadRepoFile("tools/KsAudioNodeProbe/Program.Constants.cs");
        var nativeInteropText = ReadRepoFile("tools/KsAudioNodeProbe/Program.NativeInterop.cs");
        var nativeTypesText = ReadRepoFile("tools/KsAudioNodeProbe/Program.NativeTypes.cs");

        AssertContains(programText, "using static KsAudioNodeProbeNative;");
        AssertDoesNotContain(programText, "const uint IoctlKsProperty");
        AssertDoesNotContain(programText, "struct KsProperty");
        AssertDoesNotContain(programText, "DllImport(");
        AssertDoesNotContain(programText, "static List<string> EnumerateKsInterfaces");
        AssertDoesNotContain(programText, "static bool TryAudioGetLong");
        AssertDoesNotContain(programText, "var anyHit = false");
        AssertContains(constantsText, "public const uint IoctlKsProperty = 0x002F0003;");
        AssertContains(constantsText, "public const int ErrorMoreData = 234;");
        AssertContains(nativeInteropText, "using static KsAudioNodeProbeConstants;");
        AssertContains(nativeInteropText, "static class KsAudioNodeProbeNative");
        AssertContains(nativeInteropText, "public static List<string> EnumerateKsInterfaces");
        AssertContains(nativeInteropText, "private static extern bool DeviceIoControl");
        AssertContains(nativeTypesText, "struct KsProperty");
        AssertContains(nativeTypesText, "struct SP_DEVICE_INTERFACE_DETAIL_DATA");

        return Task.CompletedTask;
    }

    private static Task EgavdsAudioProbe_SourceOwnership_IsSplit()
    {
        var programText = ReadRepoFile("tools/EgavdsAudioProbe/Program.cs");
        var nativeInteropText = ReadRepoFile("tools/EgavdsAudioProbe/Program.NativeInterop.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(programText, "static partial class EgavdsProbe");
        AssertContains(programText, "static string? FindElgato4KXDevicePath()");
        AssertContains(programText, "EGAVDS_SetAudioInputSelection(handleRef, targetInput)");
        AssertContains(programText, "EGAVDS_SetLineInAudioGain(handleRef, setGain.Value)");
        AssertDoesNotContain(programText, "const string DLL = \"EGAVDeviceSupport\"");
        AssertDoesNotContain(programText, "SWIGRegisterExceptionCallbacks_EGAVDS");
        AssertDoesNotContain(programText, "DllImport(");
        AssertDoesNotContain(programText, "struct SP_DEVICE_INTERFACE_DATA");

        AssertContains(nativeInteropText, "static partial class EgavdsProbe");
        AssertContains(nativeInteropText, "private const string DLL = \"EGAVDeviceSupport\"");
        AssertContains(nativeInteropText, "private static void RegisterSwigCallbacks()");
        AssertContains(nativeInteropText, "private static extern int EGAVDS_OpenDevice");
        AssertContains(nativeInteropText, "private static extern bool SetupDiEnumDeviceInterfaces");
        AssertContains(nativeInteropText, "private struct SP_DEVICE_INTERFACE_DATA");
        AssertContains(agentMapText, "`tools/EgavdsAudioProbe/Program.cs` owns EGAVDS audio probe command flow,");
        AssertContains(agentMapText, "`Program.NativeInterop.cs`");
        AssertContains(cleanupPlanText, "`tools/EgavdsAudioProbe/Program.NativeInterop.cs`");

        return Task.CompletedTask;
    }
}
