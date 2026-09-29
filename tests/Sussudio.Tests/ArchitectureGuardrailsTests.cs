using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

static partial class Program
{

    private static string NormalizeRepoRelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string GetRepoFileName(string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var slashIndex = normalizedPath.LastIndexOf('/');
        return slashIndex < 0 ? normalizedPath : normalizedPath.Substring(slashIndex + 1);
    }


    private static IEnumerable<string> EnumerateSourceFiles(string root, SearchOption searchOption)
        => Directory.EnumerateFiles(root, "*.cs", searchOption)
            .Where(file => !HasIgnoredPathSegment(root, file));

    private static string NormalizeProjectInclude(string include)
        => include.Trim().Replace('\\', '/');

    private static bool HasIgnoredPathSegment(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(IsIgnoredPathSegment);
    }

    // Build output and tool-local state are not part of the documented source tree.
    // Dot-directories (.git, .desloppify, .claude, .vscode, ...) hold local state that
    // no architecture document references, and walking them is both slow and prone to
    // access-denied entries that would abort the whole enumeration.
    private static bool IsIgnoredPathSegment(string segment)
        => segment.StartsWith('.') ||
           string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(segment, "Generated Files", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateRtkDefExports(string defText)
    {
        var inExports = false;
        foreach (var line in defText.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(";", StringComparison.Ordinal))
            {
                continue;
            }

            if (!inExports)
            {
                inExports = trimmed.Equals("EXPORTS", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            yield return trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
        }
    }

    internal static Task RtkI2cProbe_GuardsUnsafeNativePaths()
    {
        var assembly = LoadToolAssemblyIsolated(global::Program.NativeXuAudioProbeAssemblyRelativePath);
        var probeType = assembly.GetType("RtkI2cProbe")
            ?? throw new InvalidOperationException("RtkI2cProbe type not found.");
        var run = probeType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.Run method not found.");
        var getRtkDeviceName = probeType.GetMethod("GetRtkDeviceName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.GetRtkDeviceName method not found.");
        var rtkProbeSource = ReadRepoFile("tools/NativeXuAudioProbe/Program.cs");

        var missingPathDevice = CreateNativeXuProbeDevice(assembly, "capture-1", "Elgato 4K X (PID 0x0070)", null);
        var missingPathExitCode = InvokeRtkRun(run, [], missingPathDevice);
        AssertEqual(1, missingPathExitCode, "RtkI2cProbe missing native XU path exit code");
        AssertContains(rtkProbeSource, "requires a selected native XU interface path");

        var selectedPathDevice = CreateNativeXuProbeDevice(assembly, "capture-2", "Elgato 4K X (PID 0x0070)", @"\\?\hid#vid_0fd9&pid_0070#xu");
        var disabledSwitchExitCode = InvokeRtkRun(run, ["switch", "analog"], selectedPathDevice);
        AssertEqual(1, disabledSwitchExitCode, "RtkI2cProbe disabled switch exit code");
        AssertContains(rtkProbeSource, "RTK I2C switch is disabled");
        AssertContains(rtkProbeSource, "Use the native XU service/probe path");

        var rtkShimSource = ReadRepoFile("tools/RtkIoShim/rtk_io_shim.cpp")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var rtkShimDef = ReadRepoFile("tools/RtkIoShim/rtk_io_shim.def")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var rtkShimBuildScript = ReadRepoFile("tools/RtkIoShim/build.bat")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var verifiedRtkAbiExports = new HashSet<string>(StringComparer.Ordinal)
        {
            "rtk_initialize",
            "rtk_uninitialize",
            "rtk_openPort",
            "rtk_closePort",
            "rtk_isOpen",
            "rtk_setUVCExtension",
            "rtk_setCurrentDevice",
            "rtk_sendI2CATCommand",
            "rtk_getCurrentDeviceName"
        };
        var verifiedRtkAbiImplementations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["rtk_initialize"] = "VERIFIED_FORWARD4(rtk_initialize)",
            ["rtk_uninitialize"] = "VERIFIED_FORWARD0(rtk_uninitialize)",
            ["rtk_openPort"] = "RESOLVE_TYPED_OR_RETURN(rtk_openPort, RtkFourArgsFn, -1);",
            ["rtk_closePort"] = "VERIFIED_FORWARD0(rtk_closePort)",
            ["rtk_isOpen"] = "VERIFIED_FORWARD4(rtk_isOpen)",
            ["rtk_setUVCExtension"] = "RESOLVE_TYPED_OR_RETURN(rtk_setUVCExtension, RtkFourArgsFn, -1);",
            ["rtk_setCurrentDevice"] = "RESOLVE_TYPED_OR_RETURN(rtk_setCurrentDevice, RtkSetCurrentDeviceFn, -1);",
            ["rtk_sendI2CATCommand"] = "RESOLVE_TYPED_OR_RETURN(rtk_sendI2CATCommand, RtkEightArgsFn, -1);",
            ["rtk_getCurrentDeviceName"] = "RESOLVE_TYPED_OR_RETURN(rtk_getCurrentDeviceName, RtkGetCurrentDeviceNameFn, nullptr);"
        };
        var mutatingRtkExports = new HashSet<string>(StringComparer.Ordinal)
        {
            "rtk_writeRbus",
            "rtk_rescueWriteRbus",
            "rtk_enterDebugMode",
            "rtk_exitDebugMode",
            "rtk_burnDPEDID",
            "rtk_burnEDID",
            "rtk_burnHDCP",
            "rtk_burnMultiFiles",
            "rtk_burnToFlash",
            "rtk_burnToFlashWithLog",
            "rtk_burnUSBDesription"
        };

        AssertContains(rtkShimSource, "RTK_SHIM_ALLOW_UNVERIFIED_ABI");
        AssertContains(rtkShimSource, "RTK_SHIM_ALLOW_REPEAT_MUTATION");
        AssertContains(rtkShimSource, "CreateMutexW(nullptr, FALSE, L\"Local\\\\Sussudio.RtkIoShim.Mutation.v1\")");
        AssertContains(rtkShimSource, "WaitForSingleObject(lock, 0)");
        AssertContains(rtkShimSource, "static bool TryBeginRtkMutationAttempt(const char* name, volatile LONG* attempted)");
        AssertContains(rtkShimSource, "static long long BlockUnverifiedAbiCall(const char* name)");
        AssertContains(rtkShimSource, "if (!AllowUnverifiedAbiForwarding()) return BlockUnverifiedAbiCall(\"rtk_sendATCommand\");");
        AssertContains(rtkShimSource, "RESOLVE_TYPED_OR_RETURN(rtk_sendI2CATCommand, RtkEightArgsFn, -1);");
        AssertContains(rtkShimSource, "__declspec(dllexport) long long __cdecl rtk_sendI2CATCommand(\n    long long a1, long long a2, long long a3, long long a4,\n    long long a5, long long a6, long long a7, long long a8)");
        AssertContains(rtkShimSource, "__declspec(dllexport) const char* __cdecl rtk_getCurrentDeviceName()");
        AssertContains(rtkShimSource, "MUTATING_UNVERIFIED_FORWARD(rtk_burnToFlash)");
        AssertContains(rtkShimSource, "MUTATING_UNVERIFIED_FORWARD(rtk_enterDebugMode)");
        AssertContains(rtkShimSource, "UNVERIFIED_FORWARD(rtk_readRbus)");
        AssertContains(rtkShimSource, "MUTATING_UNVERIFIED_FORWARD(rtk_writeRbus)");
        AssertDoesNotContain(rtkShimSource, "#define FORWARD(name)");
        AssertDoesNotContain(rtkShimSource, "SIMPLE_FORWARD(");
        AssertOccursBefore(rtkShimSource, "if (!AllowUnverifiedAbiForwarding()) return BlockUnverifiedAbiCall(\"rtk_sendATCommand\");", "Log(\"rtk_sendATCommand");
        foreach (var verifiedExport in verifiedRtkAbiExports)
        {
            AssertContains(rtkShimSource, verifiedRtkAbiImplementations[verifiedExport]);
            AssertDoesNotContain(rtkShimSource, $"UNVERIFIED_FORWARD({verifiedExport})");
            AssertDoesNotContain(rtkShimSource, $"MUTATING_UNVERIFIED_FORWARD({verifiedExport})");
        }

        foreach (var exportName in EnumerateRtkDefExports(rtkShimDef))
        {
            if (verifiedRtkAbiExports.Contains(exportName))
            {
                continue;
            }

            if (exportName == "rtk_sendATCommand")
            {
                AssertContains(rtkShimSource, "BlockUnverifiedAbiCall(\"rtk_sendATCommand\")");
                continue;
            }

            AssertContains(
                rtkShimSource,
                mutatingRtkExports.Contains(exportName)
                    ? $"MUTATING_UNVERIFIED_FORWARD({exportName})"
                    : $"UNVERIFIED_FORWARD({exportName})");
        }

        AssertContains(rtkShimBuildScript, "if /I not \"%VSCMD_ARG_TGT_ARCH%\"==\"x64\"");
        AssertContains(rtkShimBuildScript, "where cl.exe");
        AssertContains(rtkShimBuildScript, "where dumpbin.exe");
        AssertContains(rtkShimBuildScript, "del /q RTK_IO_x64.dll RTK_IO_x64.lib RTK_IO_x64.exp rtk_io_shim.obj");
        AssertContains(rtkShimBuildScript, "/MACHINE:X64");
        AssertContains(rtkShimBuildScript, "dumpbin /headers RTK_IO_x64.dll | findstr /C:\"machine (x64)\"");
        AssertContains(rtkShimBuildScript, "$ErrorActionPreference = 'Stop'");
        AssertContains(rtkShimBuildScript, "Get-FileHash -Algorithm SHA256 -LiteralPath 'RTK_IO_x64.dll'");
        AssertContains(rtkShimBuildScript, "Get-FileHash -Algorithm SHA256 -LiteralPath 'RTK_IO_x64.dll' -ErrorAction Stop");
        AssertContains(rtkShimBuildScript, "if ([string]::IsNullOrWhiteSpace($dll.Hash)) { throw 'RTK_IO_x64.dll hash was empty.' }");
        AssertContains(rtkShimBuildScript, "Get-FileHash -Algorithm SHA256 -LiteralPath 'RTK_IO_x64_real.dll' -ErrorAction Stop");
        AssertContains(rtkShimBuildScript, "if ([string]::IsNullOrWhiteSpace($real.Hash)) { throw 'RTK_IO_x64_real.dll hash was empty.' }");
        AssertContains(rtkShimBuildScript, "RTK_IO_x64_real.dll SHA256=");
        AssertContains(rtkShimBuildScript, "rename the vendor RTK_IO_x64.dll to RTK_IO_x64_real.dll first");
        AssertDoesNotContain(rtkShimBuildScript, "/MACHINE:X86");

        var trimmedName = getRtkDeviceName.Invoke(null, [selectedPathDevice]) as string;
        AssertEqual("Elgato 4K X", trimmedName, "RtkI2cProbe strips PID suffix for RTK device name");
        var defaultNameDevice = CreateNativeXuProbeDevice(assembly, "capture-3", string.Empty, @"\\?\hid#vid_0fd9&pid_0070#xu");
        var defaultName = getRtkDeviceName.Invoke(null, [defaultNameDevice]) as string;
        AssertEqual("Elgato 4K X", defaultName, "RtkI2cProbe default RTK device name");

        return Task.CompletedTask;
    }

    private static object CreateNativeXuProbeDevice(
        Assembly assembly,
        string id,
        string name,
        string? nativeXuInterfacePath)
    {
        var deviceType = assembly.GetType("Sussudio.Models.CaptureDevice")
            ?? throw new InvalidOperationException("NativeXuAudioProbe CaptureDevice type not found.");
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("Failed to create NativeXuAudioProbe CaptureDevice.");
        deviceType.GetProperty("Id")?.SetValue(device, id);
        deviceType.GetProperty("Name")?.SetValue(device, name);
        deviceType.GetProperty("NativeXuInterfacePath")?.SetValue(device, nativeXuInterfacePath);
        return device;
    }

    private static int InvokeRtkRun(MethodInfo run, string[] args, object device)
    {
        try
        {
            return (int)(run.Invoke(null, [args, device])
                         ?? throw new InvalidOperationException("RtkI2cProbe.Run returned null."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }


}
