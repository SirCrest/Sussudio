using System.Reflection;
using System.Threading.Tasks;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static readonly object RtkI2cProbeConsoleLock = new();

    internal static Task RtkI2cProbe_GuardsUnsafeNativePaths()
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine(
            "tools",
            "NativeXuAudioProbe",
            "bin",
            "Debug",
            "net8.0-windows10.0.19041.0",
            "win-x64",
            "NativeXuAudioProbe.dll"));
        var probeType = assembly.GetType("RtkI2cProbe")
            ?? throw new InvalidOperationException("RtkI2cProbe type not found.");
        var run = probeType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.Run method not found.");
        var getRtkDeviceName = probeType.GetMethod("GetRtkDeviceName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.GetRtkDeviceName method not found.");
        var rtkProbeSource = ReadRepoFile("tools/NativeXuAudioProbe/RtkI2cProbe.cs");

        var missingPathDevice = CreateNativeXuProbeDevice(assembly, "capture-1", "Elgato 4K X (PID 0x0070)", null);
        var missingPath = CaptureConsole(() => InvokeRtkRun(run, [], missingPathDevice));
        AssertEqual(1, missingPath.ExitCode, "RtkI2cProbe missing native XU path exit code");
        AssertContains(rtkProbeSource, "requires a selected native XU interface path");

        var selectedPathDevice = CreateNativeXuProbeDevice(assembly, "capture-2", "Elgato 4K X (PID 0x0070)", @"\\?\hid#vid_0fd9&pid_0070#xu");
        var disabledSwitch = CaptureConsole(() => InvokeRtkRun(run, ["switch", "analog"], selectedPathDevice));
        AssertEqual(1, disabledSwitch.ExitCode, "RtkI2cProbe disabled switch exit code");
        AssertContains(rtkProbeSource, "RTK I2C switch is disabled");
        AssertContains(rtkProbeSource, "Use the native XU service/probe path");

        var trimmedName = getRtkDeviceName.Invoke(null, [selectedPathDevice]) as string;
        AssertEqual("Elgato 4K X", trimmedName, "RtkI2cProbe strips PID suffix for RTK device name");
        var defaultNameDevice = CreateNativeXuProbeDevice(assembly, "capture-3", string.Empty, @"\\?\hid#vid_0fd9&pid_0070#xu");
        var defaultName = getRtkDeviceName.Invoke(null, [defaultNameDevice]) as string;
        AssertEqual("Elgato 4K X", defaultName, "RtkI2cProbe default RTK device name");

        return Task.CompletedTask;
    }

    private static void AssertServiceNamespaceNativeXuProbeOwnership(string repoRoot)
    {
        var nativeXuProbeProjectText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuAudioProbe.csproj"));
        AssertDoesNotContain(nativeXuProbeProjectText, "<ProjectReference");
        AssertDoesNotContain(nativeXuProbeProjectText, "Sussudio.csproj");
        AssertContains(nativeXuProbeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuDeviceSupport.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AnalogGain.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AudioSwitch.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DiagnosticSummary.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DeviceCommandReads.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DeviceCommands.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.FullSnapshot.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.InterfaceRead.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.PayloadDecoding.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.Selector4.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Build.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs");
        AssertContains(File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Models", "Capture", "CaptureModels.cs")), "NativeXuInterfacePath");

        var nativeXuLocatorText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.cs"));
        AssertContains(nativeXuLocatorText, "NativeXuInterfacePath = interfacePath");
        AssertContains(nativeXuLocatorText, "matches.Length > 1");
        AssertDoesNotContain(nativeXuLocatorText, "return firstCandidate");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuProbeDeviceLocator.cs")),
            "NativeXu probe device lookup lives with top-level probe command routing");

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            AssertDoesNotContain(code, "Activator.CreateInstance");
            AssertDoesNotContain(code, "BindingFlags");
            AssertDoesNotContain(code, "GetMethod(");
            AssertDoesNotContain(code, "GetProperty(");
            AssertDoesNotContain(code, "ReadPreferredPayloadAsync");
            AssertDoesNotContain(code, "typeof(NativeXuAudioControlService)");
        }
        var probeProgramText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.cs"));
        AssertContains(probeProgramText, "Probe-local runtime shims used by linked app service sources.");
        AssertContains(probeProgramText, "NativeXuInterfacePath");
        AssertContains(probeProgramText, "EnumerateKsInterfaces(ElgatoVendorId");
        AssertContains(probeProgramText, "RTK_IO selects by name, not by native XU path");
        AssertContains(probeProgramText, "string.Equals(arg, \"--device\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(probeProgramText, "NativeXuProbeDeviceLocator.Find(null)");
        AssertContains(probeProgramText, "RtkI2cProbe.Run(rtkArgs, dev)");
        var probeDefaultExperimentText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.DefaultExperiment.cs"));
        var probeDefaultExperimentReportingText = probeDefaultExperimentText;
        var probeI2cCommandsText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.cs"));
        var probeI2cLegacyProbeText = probeI2cCommandsText;
        AssertDoesNotContain(probeProgramText, "sealed record GetterSpec");
        AssertDoesNotContain(probeProgramText, "sealed class ExperimentResult");
        AssertDoesNotContain(probeProgramText, "const int CmdAudioFormat");
        AssertDoesNotContain(probeProgramText, "PrintSnapshot(\"Baseline snapshot\"");
        AssertDoesNotContain(probeProgramText, "static async Task RunAnalogGainSequenceAsync");
        AssertDoesNotContain(probeProgramText, "Usage: i2c-cmd get|set|scan");
        AssertDoesNotContain(probeProgramText, "Tests whether rtk_sendI2CATCommand uses the same XU path");
        AssertDoesNotContain(probeProgramText, "I2C SET/verify via AT envelope");
        AssertDoesNotContain(probeProgramText, "static IEnumerable<SetExperiment> BuildShortExperiments");
        AssertDoesNotContain(probeProgramText, "static async Task<byte[]?> SendI2cAtGetAsync");
        AssertDoesNotContain(probeProgramText, "static byte[] BuildAtFrameWithPayload");
        AssertDoesNotContain(probeProgramText, "using static NativeXuProbeI2cTransport;");
        AssertContains(probeProgramText, "NativeXuProbeI2cCommands.RunAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeAtCommands.RunAtReadAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeAtCommands.RunAtWriteAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeAtCommands.RunAtSetInputAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeDefaultExperiment.RunAsync(device)");
        AssertContains(probeProgramText, "NativeXuProbeI2cLegacyProbe.Run()");
        AssertContains(probeProgramText, "NativeXuProbeI2cSwitch.RunAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeServiceProbe.RunServiceControlProbeAsync");
        AssertContains(probeProgramText, "NativeXuProbeServiceProbe.RunServiceSmokeAsync");
        AssertContains(probeProgramText, "static class NativeXuProbeServiceProbe");
        AssertContains(probeProgramText, "public static async Task<int> RunServiceControlProbeAsync");
        AssertContains(probeProgramText, "public static async Task<int> RunServiceSmokeAsync");
        AssertContains(probeProgramText, "ReadServiceStateAsync");
        AssertContains(probeProgramText, "Service payload snapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.Models.cs")),
            "old NativeXu probe model bucket removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.AtCommands.cs")),
            "NativeXu direct AT probe commands live with top-level probe command routing");
        AssertContains(probeProgramText, "static class NativeXuProbeAtCommands");
        AssertContains(probeProgramText, "public static async Task<int> RunAtReadAsync");
        AssertContains(probeProgramText, "public static async Task<int> RunAtWriteAsync");
        AssertContains(probeProgramText, "public static async Task<int> RunAtSetInputAsync");
        AssertContains(probeProgramText, "Usage: at-write <opcode_hex>");
        AssertContains(probeProgramText, "Before: InputSource=");
        AssertContains(probeDefaultExperimentText, "public const int CmdAudioFormat = 0x04;");
        AssertContains(probeDefaultExperimentText, "public const int CmdSetAuxOutVolume = 0x82;");
        AssertContains(probeDefaultExperimentText, "static class NativeXuProbeFormatting");
        AssertContains(probeDefaultExperimentText, "public static string FormatRaw");
        AssertContains(probeDefaultExperimentText, "static class NativeXuProbeDefaultExperiment");
        AssertDoesNotContain(probeDefaultExperimentText, "partial class NativeXuProbeDefaultExperiment");
        AssertContains(probeDefaultExperimentText, "sealed record GetterSpec");
        AssertContains(probeDefaultExperimentText, "sealed record SetterSpec");
        AssertContains(probeDefaultExperimentText, "sealed record SetExperiment");
        AssertContains(probeDefaultExperimentText, "public static async Task<int> RunAsync(CaptureDevice device)");
        AssertContains(probeDefaultExperimentText, "RunAnalogGainSequenceAsync");
        AssertContains(probeDefaultExperimentText, "private static IEnumerable<SetExperiment> BuildShortExperiments");
        AssertContains(probeDefaultExperimentText, "private static byte[] BuildPayload(int width, long value)");
        AssertContains(probeDefaultExperimentReportingText, "sealed record AtReadResult");
        AssertContains(probeDefaultExperimentReportingText, "sealed record ChangedValue");
        AssertContains(probeDefaultExperimentReportingText, "sealed class ExperimentResult");
        AssertContains(probeDefaultExperimentReportingText, "private static async Task<Dictionary<int, AtReadResult>> ReadAllAsync");
        AssertContains(probeDefaultExperimentReportingText, "private static AtReadResult Decode");
        AssertContains(probeDefaultExperimentReportingText, "private static void PrintInterestingChanges");
        AssertContains(probeDefaultExperimentReportingText, "private static void PrintSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.DefaultExperiment.Reporting.cs")),
            "NativeXu default experiment reporting folded into default experiment owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.ExperimentPayloads.cs")),
            "NativeXu probe experiment payload helpers folded into default experiment owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.Commands.cs")),
            "NativeXu probe command IDs and shared raw formatting live with default experiment support");
        AssertContains(probeI2cCommandsText, "static class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsText, "static class NativeXuProbeI2cTransport");
        AssertDoesNotContain(probeI2cCommandsText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunAsync");
        AssertContains(probeI2cCommandsText, "Usage: i2c-cmd get|set|scan");
        AssertContains(probeI2cCommandsText, "RunVerifyAsync(dev)");
        AssertContains(probeI2cCommandsText, "I2C SET/verify via AT envelope");
        AssertContains(probeI2cCommandsText, "RunTopologyProbe(dev)");
        AssertContains(probeI2cCommandsText, "Testing with own GUID as property set");
        AssertContains(probeI2cCommandsText, "RunSelectorProbeAsync(dev)");
        AssertContains(probeI2cCommandsText, "Full Selector 3 dump");
        AssertContains(probeI2cCommandsText, "RunHighSelectorProbeAsync(dev)");
        AssertContains(probeI2cCommandsText, "Probing selectors 18-40");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunHighSelectorProbeAsync");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunSelectorProbeAsync");
        AssertContains(probeI2cCommandsText, "public static int RunTopologyProbe");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunVerifyAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.HighSelectorProbe.cs")),
            "old NativeXu i2c high-selector partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.SelectorProbe.cs")),
            "old NativeXu i2c selector partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.TopologyProbe.cs")),
            "old NativeXu i2c topology partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.Verify.cs")),
            "old NativeXu i2c verify partial removed");
        AssertContains(probeI2cLegacyProbeText, "static class NativeXuProbeI2cLegacyProbe");
        AssertContains(probeI2cLegacyProbeText, "public static int Run()");
        AssertContains(probeI2cLegacyProbeText, "Tests whether rtk_sendI2CATCommand uses the same XU path");
        AssertContains(probeI2cLegacyProbeText, "ProbeRawI2cFrames");
        AssertContains(probeI2cLegacyProbeText, "ProbeAlternateSelectors");
        AssertContains(probeI2cLegacyProbeText, "ProbeAtWrappedI2cFrames");
        AssertContains(probeI2cCommandsText, "public static async Task<byte[]?> SendI2cAtGetAsync");
        AssertContains(probeI2cCommandsText, "public static byte[] BuildAtFrameWithPayload");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cLegacyProbe.cs")),
            "NativeXu legacy i2c-probe workflow lives with the I2C command family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cSwitch.cs")),
            "NativeXu captured audio-switch replay workflow lives with top-level probe command routing");
        AssertContains(probeProgramText, "static class NativeXuProbeI2cSwitch");
        AssertContains(probeProgramText, "public static async Task<int> RunAsync");
        AssertContains(probeProgramText, "Current I2C AT state");
        AssertContains(probeProgramText, "Sending audio switch sequence");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cTransport.cs")),
            "NativeXu I2C-over-AT transport helpers live with the I2C command family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.ServiceProbe.cs")),
            "NativeXu service-control smoke/payload workflows live with top-level probe command routing");
        var rtkProbeText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "RtkI2cProbe.cs"));
        AssertContains(rtkProbeText, "Run(string[] args, CaptureDevice device)");
        AssertContains(rtkProbeText, "RTK I2C switch is disabled");
        AssertDoesNotContain(rtkProbeText, "rtk_setCurrentDevice(\"Elgato 4K X\"");

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "Sussudio"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsPreserveLiterals(File.ReadAllText(file));
            AssertDoesNotContain(code, "InternalsVisibleTo(\"NativeXuAudioProbe\")");
        }
    }

    private static object CreateNativeXuProbeDevice(
        Assembly assembly,
        string id,
        string name,
        string? nativeXuInterfacePath)
    {
        var deviceType = assembly.GetType("CaptureDevice")
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

    private static (int ExitCode, string Output, string Error) CaptureConsole(Func<int> action)
    {
        lock (RtkI2cProbeConsoleLock)
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var output = new StringWriter();
            using var error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                var exitCode = action();
                return (exitCode, output.ToString(), error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
