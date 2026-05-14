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
}
