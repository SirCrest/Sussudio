// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static void AssertServiceNamespaceNativeXuProbeOwnership(string repoRoot)
    {
        var nativeXuProbeProjectText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuAudioProbe.csproj"));
        AssertDoesNotContain(nativeXuProbeProjectText, "<ProjectReference");
        AssertDoesNotContain(nativeXuProbeProjectText, "Sussudio.csproj");
        AssertContains(nativeXuProbeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuDeviceSupport.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AnalogGain.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AudioSwitch.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DiagnosticSummary.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DeviceCommandReads.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DeviceCommands.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.FullSnapshot.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.InterfaceRead.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.PayloadDecoding.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.Selector4.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Build.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.cs");
        AssertContains(File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Models", "Capture", "CaptureModels.cs")), "NativeXuInterfacePath");
        AssertContains(File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "ToolCaptureDevice.cs")), "NativeXuInterfacePath");

        var nativeXuLocatorText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuProbeDeviceLocator.cs"));
        AssertContains(nativeXuLocatorText, "NativeXuInterfacePath = interfacePath");
        AssertContains(nativeXuLocatorText, "matches.Length > 1");
        AssertDoesNotContain(nativeXuLocatorText, "return firstCandidate");
        AssertDoesNotContain(
            File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.cs")),
            "KsExtensionUnitNative.EnumerateKsInterfaces(");

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
        AssertDoesNotContain(probeProgramText, "KsExtensionUnitNative.EnumerateKsInterfaces(");
        AssertContains(probeProgramText, "RTK_IO selects by name, not by native XU path");
        AssertContains(probeProgramText, "string.Equals(arg, \"--device\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(probeProgramText, "NativeXuProbeDeviceLocator.Find(null)");
        AssertContains(probeProgramText, "RtkI2cProbe.Run(rtkArgs, dev)");
        var probeAtCommandsText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.AtCommands.cs"));
        var probeCommandsText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.Commands.cs"));
        var probeDefaultExperimentText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.DefaultExperiment.cs"));
        var probeDefaultExperimentReportingText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.DefaultExperiment.Reporting.cs"));
        var probeExperimentPayloadsText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.ExperimentPayloads.cs"));
        var probeI2cCommandsText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.cs"));
        var probeI2cCommandsHighSelectorProbeText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.HighSelectorProbe.cs"));
        var probeI2cCommandsSelectorProbeText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.SelectorProbe.cs"));
        var probeI2cCommandsTopologyProbeText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.TopologyProbe.cs"));
        var probeI2cCommandsVerifyText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.Verify.cs"));
        var probeI2cLegacyProbeText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cLegacyProbe.cs"));
        var probeI2cSwitchText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cSwitch.cs"));
        var probeI2cTransportText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cTransport.cs"));
        var probeServiceText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.ServiceProbe.cs"));
        AssertDoesNotContain(probeProgramText, "sealed record GetterSpec");
        AssertDoesNotContain(probeProgramText, "sealed class ExperimentResult");
        AssertDoesNotContain(probeProgramText, "const int CmdAudioFormat");
        AssertDoesNotContain(probeProgramText, "PrintSnapshot(\"Baseline snapshot\"");
        AssertDoesNotContain(probeProgramText, "static async Task RunAnalogGainSequenceAsync");
        AssertDoesNotContain(probeProgramText, "Usage: at-write <opcode_hex>");
        AssertDoesNotContain(probeProgramText, "Before: InputSource=");
        AssertDoesNotContain(probeProgramText, "Current I2C AT state");
        AssertDoesNotContain(probeProgramText, "Sending audio switch sequence");
        AssertDoesNotContain(probeProgramText, "Usage: i2c-cmd get|set|scan");
        AssertDoesNotContain(probeProgramText, "Tests whether rtk_sendI2CATCommand uses the same XU path");
        AssertDoesNotContain(probeProgramText, "I2C SET/verify via AT envelope");
        AssertDoesNotContain(probeProgramText, "static IEnumerable<SetExperiment> BuildShortExperiments");
        AssertDoesNotContain(probeProgramText, "static async Task<byte[]?> SendI2cAtGetAsync");
        AssertDoesNotContain(probeProgramText, "static byte[] BuildAtFrameWithPayload");
        AssertDoesNotContain(probeProgramText, "static async Task<int> RunServiceControlProbeAsync");
        AssertDoesNotContain(probeProgramText, "ReadServiceStateAsync");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.Models.cs")),
            "old NativeXu probe model bucket removed");
        AssertContains(probeAtCommandsText, "static class NativeXuProbeAtCommands");
        AssertContains(probeAtCommandsText, "public static async Task<int> RunAtReadAsync");
        AssertContains(probeAtCommandsText, "public static async Task<int> RunAtWriteAsync");
        AssertContains(probeAtCommandsText, "public static async Task<int> RunAtSetInputAsync");
        AssertContains(probeAtCommandsText, "using static NativeXuProbeFormatting;");
        AssertContains(probeCommandsText, "public const int CmdAudioFormat = 0x04;");
        AssertContains(probeCommandsText, "public const int CmdSetAuxOutVolume = 0x82;");
        AssertContains(probeCommandsText, "static class NativeXuProbeFormatting");
        AssertContains(probeCommandsText, "public static string FormatRaw");
        AssertContains(probeDefaultExperimentText, "static partial class NativeXuProbeDefaultExperiment");
        AssertContains(probeDefaultExperimentText, "sealed record GetterSpec");
        AssertContains(probeDefaultExperimentText, "sealed record SetterSpec");
        AssertContains(probeDefaultExperimentText, "sealed record SetExperiment");
        AssertContains(probeDefaultExperimentText, "public static async Task<int> RunAsync(CaptureDevice device)");
        AssertContains(probeDefaultExperimentText, "RunAnalogGainSequenceAsync");
        AssertDoesNotContain(probeDefaultExperimentText, "private static AtReadResult Decode(");
        AssertDoesNotContain(probeDefaultExperimentText, "private static void PrintSnapshot(");
        AssertContains(probeDefaultExperimentReportingText, "static partial class NativeXuProbeDefaultExperiment");
        AssertContains(probeDefaultExperimentReportingText, "sealed record AtReadResult");
        AssertContains(probeDefaultExperimentReportingText, "sealed record ChangedValue");
        AssertContains(probeDefaultExperimentReportingText, "sealed class ExperimentResult");
        AssertContains(probeDefaultExperimentReportingText, "private static async Task<Dictionary<int, AtReadResult>> ReadAllAsync");
        AssertContains(probeDefaultExperimentReportingText, "private static AtReadResult Decode");
        AssertContains(probeDefaultExperimentReportingText, "private static void PrintInterestingChanges");
        AssertContains(probeDefaultExperimentReportingText, "private static void PrintSnapshot");
        AssertContains(probeExperimentPayloadsText, "public static IEnumerable<SetExperiment> BuildShortExperiments");
        AssertContains(probeExperimentPayloadsText, "public static byte[] BuildPayload(int width, long value)");
        AssertContains(probeI2cCommandsText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunAsync");
        AssertContains(probeI2cCommandsText, "Usage: i2c-cmd get|set|scan");
        AssertContains(probeI2cCommandsText, "RunVerifyAsync(dev)");
        AssertDoesNotContain(probeI2cCommandsText, "I2C SET/verify via AT envelope");
        AssertContains(probeI2cCommandsText, "RunTopologyProbe(dev)");
        AssertDoesNotContain(probeI2cCommandsText, "Testing with own GUID as property set");
        AssertContains(probeI2cCommandsText, "RunSelectorProbeAsync(dev)");
        AssertDoesNotContain(probeI2cCommandsText, "Full Selector 3 dump");
        AssertContains(probeI2cCommandsText, "RunHighSelectorProbeAsync(dev)");
        AssertDoesNotContain(probeI2cCommandsText, "Probing selectors 18-40");
        AssertContains(probeI2cCommandsHighSelectorProbeText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsHighSelectorProbeText, "public static async Task<int> RunHighSelectorProbeAsync");
        AssertContains(probeI2cCommandsHighSelectorProbeText, "Probing selectors 18-40");
        AssertContains(probeI2cCommandsSelectorProbeText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsSelectorProbeText, "public static async Task<int> RunSelectorProbeAsync");
        AssertContains(probeI2cCommandsSelectorProbeText, "Full Selector 3 dump");
        AssertContains(probeI2cCommandsTopologyProbeText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsTopologyProbeText, "public static int RunTopologyProbe");
        AssertContains(probeI2cCommandsTopologyProbeText, "Testing with own GUID as property set");
        AssertContains(probeI2cCommandsVerifyText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsVerifyText, "public static async Task<int> RunVerifyAsync");
        AssertContains(probeI2cCommandsVerifyText, "I2C SET/verify via AT envelope");
        AssertContains(probeI2cLegacyProbeText, "static class NativeXuProbeI2cLegacyProbe");
        AssertContains(probeI2cLegacyProbeText, "public static int Run()");
        AssertContains(probeI2cLegacyProbeText, "Tests whether rtk_sendI2CATCommand uses the same XU path");
        AssertContains(probeI2cLegacyProbeText, "ProbeRawI2cFrames");
        AssertContains(probeI2cLegacyProbeText, "ProbeAlternateSelectors");
        AssertContains(probeI2cLegacyProbeText, "ProbeAtWrappedI2cFrames");
        AssertContains(probeI2cSwitchText, "static class NativeXuProbeI2cSwitch");
        AssertContains(probeI2cSwitchText, "public static async Task<int> RunAsync");
        AssertContains(probeI2cSwitchText, "Sending audio switch sequence");
        AssertContains(probeI2cTransportText, "static class NativeXuProbeI2cTransport");
        AssertContains(probeI2cTransportText, "public static async Task<byte[]?> SendI2cAtGetAsync");
        AssertContains(probeI2cTransportText, "public static byte[] BuildAtFrameWithPayload");
        AssertContains(probeServiceText, "static class NativeXuProbeServiceProbe");
        AssertContains(probeServiceText, "public static async Task<int> RunServiceControlProbeAsync");
        AssertContains(probeServiceText, "public static async Task<int> RunServiceSmokeAsync");
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
}
