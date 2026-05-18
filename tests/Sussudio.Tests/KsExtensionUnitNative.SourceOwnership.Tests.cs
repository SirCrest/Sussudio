using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task KsExtensionUnitNative_SourceOwnership_IsSplitByNativeBoundary()
    {
        var rootText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.cs");
        var interfacesText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.Interfaces.cs");
        var handlesText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.Handles.cs");
        var topologyText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.Topology.cs");
        var transfersText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.Transfers.cs");
        var interopText = ReadKsExtensionUnitNativeFile("KsExtensionUnitNative.Interop.cs");

        AssertContains(rootText, "internal static partial class KsExtensionUnitNative");
        AssertContains(rootText, "internal readonly record struct KsInterfacePath");
        AssertContains(rootText, "internal readonly record struct KsTopologyNode");
        AssertDoesNotContain(rootText, "DeviceIoControl(");
        AssertDoesNotContain(rootText, "[DllImport(");
        AssertDoesNotContain(rootText, "TryReadTopologyNodes(");
        AssertDoesNotContain(rootText, "TryXuGetDirect(");

        AssertContains(interfacesText, "internal static IReadOnlyList<KsInterfacePath> EnumerateKsInterfaces(");
        AssertContains(handlesText, "internal static SafeFileHandle? TryOpen(");
        AssertContains(topologyText, "internal static bool TryReadTopologyNodes(");
        AssertContains(transfersText, "internal static bool TryXuGetDirect(");
        AssertContains(transfersText, "internal static bool TryXuSetViaOutput(");
        AssertContains(transfersText, "internal static bool TryXuSetViaInput(");
        AssertContains(interopText, "[DllImport(\"setupapi.dll\", SetLastError = true)]");
        AssertContains(interopText, "[DllImport(\"kernel32.dll\", SetLastError = true)]");
        AssertContains(interopText, "[StructLayout(LayoutKind.Sequential)]");

        var probeIncludes = ReadCompileIncludes(Path.Combine(
            GetRepoRoot(),
            "tools",
            "NativeXuAudioProbe",
            "NativeXuAudioProbe.csproj"));
        foreach (var include in new[]
        {
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.Handles.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.Interfaces.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.Interop.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.Topology.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\KsExtensionUnitNative.Transfers.cs",
            @"..\..\Sussudio\Services\Capture\NativeXu\NativeXuDeviceSupport.cs"
        })
        {
            AssertEqual(1, CountCompileInclude(probeIncludes, include), $"NativeXuAudioProbe links {include}");
        }

        return Task.CompletedTask;
    }

    private static string ReadKsExtensionUnitNativeFile(string fileName) =>
        ReadRepoFile($"Sussudio/Services/Capture/NativeXu/{fileName}");
}
