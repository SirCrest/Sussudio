using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task KsExtensionUnitNative_SourceOwnership_IsSplitByNativeBoundary()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/KsExtensionUnitNative.cs");
        var interfacesText = ReadRepoFile("Sussudio/Services/Capture/KsExtensionUnitNative.Interfaces.cs");
        var handlesText = ReadRepoFile("Sussudio/Services/Capture/KsExtensionUnitNative.Handles.cs");
        var topologyText = ReadRepoFile("Sussudio/Services/Capture/KsExtensionUnitNative.Topology.cs");
        var transfersText = ReadRepoFile("Sussudio/Services/Capture/KsExtensionUnitNative.Transfers.cs");
        var interopText = ReadRepoFile("Sussudio/Services/Capture/KsExtensionUnitNative.Interop.cs");

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
            @"..\..\Sussudio\Services\Capture\KsExtensionUnitNative.cs",
            @"..\..\Sussudio\Services\Capture\KsExtensionUnitNative.Handles.cs",
            @"..\..\Sussudio\Services\Capture\KsExtensionUnitNative.Interfaces.cs",
            @"..\..\Sussudio\Services\Capture\KsExtensionUnitNative.Interop.cs",
            @"..\..\Sussudio\Services\Capture\KsExtensionUnitNative.Topology.cs",
            @"..\..\Sussudio\Services\Capture\KsExtensionUnitNative.Transfers.cs"
        })
        {
            AssertEqual(1, CountCompileInclude(probeIncludes, include), $"NativeXuAudioProbe links {include}");
        }

        return Task.CompletedTask;
    }
}
