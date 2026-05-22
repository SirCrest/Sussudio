using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task KsExtensionUnitNative_SourceOwnership_IsCohesiveNativeBridge()
    {
        var rootText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.cs");

        AssertContains(rootText, "internal static class KsExtensionUnitNative");
        AssertDoesNotContain(rootText, "partial class KsExtensionUnitNative");
        AssertContains(rootText, "internal readonly record struct KsInterfacePath");
        AssertContains(rootText, "internal readonly record struct KsTopologyNode");
        AssertContains(rootText, "internal static IReadOnlyList<KsInterfacePath> EnumerateKsInterfaces(");
        AssertContains(rootText, "internal static SafeFileHandle? TryOpen(");
        AssertContains(rootText, "internal static bool TryReadTopologyNodes(");
        AssertContains(rootText, "internal static bool TryXuGetDirect(");
        AssertContains(rootText, "internal static bool TryXuSetViaOutput(");
        AssertContains(rootText, "internal static bool TryXuSetViaInput(");
        AssertContains(rootText, "DeviceIoControl(");
        AssertContains(rootText, "[DllImport(\"setupapi.dll\", SetLastError = true)]");
        AssertContains(rootText, "[DllImport(\"kernel32.dll\", SetLastError = true)]");
        AssertContains(rootText, "[StructLayout(LayoutKind.Sequential)]");

        var probeIncludes = ReadCompileIncludes(Path.Combine(
            GetRepoRoot(),
            "tools",
            "NativeXuAudioProbe",
            "NativeXuAudioProbe.csproj"));
        foreach (var include in new[]
        {
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\NativeXuDeviceSupport.cs"
        })
        {
            AssertEqual(1, CountCompileInclude(probeIncludes, include), $"NativeXuAudioProbe links {include}");
        }

        foreach (var removedFile in new[]
        {
            "KsExtensionUnitNative.Handles.cs",
            "KsExtensionUnitNative.Interfaces.cs",
            "KsExtensionUnitNative.Interop.cs",
            "KsExtensionUnitNative.Topology.cs",
            "KsExtensionUnitNative.Transfers.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "NativeXu", removedFile)),
                $"{removedFile} removed");
            AssertEqual(
                0,
                CountCompileInclude(probeIncludes, $@"..\..\Sussudio\Services\Capture\NativeXu\{removedFile}"),
                $"NativeXuAudioProbe no longer links {removedFile}");
        }

        return Task.CompletedTask;
    }

    private static string ReadKsExtensionUnitNativeFile(string fileName) =>
        ReadRepoFile($"Sussudio/Services/Capture/NativeXu/{fileName}");
}
