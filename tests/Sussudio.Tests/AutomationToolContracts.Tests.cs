using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;
using Xunit;

// Shared reflection helpers and contract checks for automation tool contract tests.
static partial class Program
{
    internal static Task NvmlSnapshot_ComputedProperties_ConvertUnits()
    {
        var snapshotType = RequireType("Sussudio.Services.Gpu.NvmlSnapshot");
        // Constructor: GpuName, GpuUtil%, MemUtil%, NvdecUtil%, NvencUtil%, PcieTxKB, PcieRxKB,
        //              VramUsedB, VramTotalB, TempC, PowerMw, ClockMHz, MemClockMHz
        var snapshot = Activator.CreateInstance(snapshotType,
            "RTX 4090",        // GpuName
            (uint?)85,         // GpuUtilizationPercent
            (uint?)40,         // GpuMemoryUtilizationPercent
            (uint?)50,         // NvdecUtilizationPercent
            (uint?)75,         // NvencUtilizationPercent
            (uint?)1024,       // PcieTxKBps (1024 KB/s = 1.0 MB/s)
            (uint?)2048,       // PcieRxKBps (2048 KB/s = 2.0 MB/s)
            (ulong?)2147483648,// VramUsedBytes (2 GB)
            (ulong?)25769803776,// VramTotalBytes (24 GB)
            (uint?)65,         // GpuTemperatureC
            (uint?)350000,     // GpuPowerMilliwatts (350W)
            (uint?)2520,       // GpuClockMHz
            (uint?)10501)!;    // GpuMemClockMHz

        var powerW = GetPropertyValue(snapshot, "GpuPowerW");
        AssertEqual(350.0, (double)powerW!, "GpuPowerW");

        var txMB = GetPropertyValue(snapshot, "PcieTxMBps");
        AssertEqual(1.0, (double)txMB!, "PcieTxMBps");

        var rxMB = GetPropertyValue(snapshot, "PcieRxMBps");
        AssertEqual(2.0, (double)rxMB!, "PcieRxMBps");

        var usedMB = GetPropertyValue(snapshot, "VramUsedMB");
        AssertEqual(2048UL, (ulong)usedMB!, "VramUsedMB");

        return Task.CompletedTask;
    }

    internal static Task NvmlMonitor_NativeInteropLivesWithMonitorOwner()
    {
        var monitorText = ReadRepoFile("Sussudio/Services/Gpu/NvmlMonitor.cs");

        AssertContains(monitorText, "public sealed class NvmlMonitor : IDisposable");
        AssertContains(monitorText, "private void Poll(object? state)");
        AssertContains(monitorText, "public NvmlSnapshot? GetLatestSnapshot()");
        AssertContains(monitorText, "TryLoadNativeLibrary()");
        AssertContains(monitorText, "private static unsafe string? GetDeviceName(IntPtr device)");
        AssertContains(monitorText, "private struct NvmlUtilization");
        AssertContains(monitorText, "[DllImport(\"nvml.dll\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "NvmlMonitor.NativeInterop.cs")),
            "NvmlMonitor.NativeInterop.cs folded into NvmlMonitor.cs");

        return Task.CompletedTask;
    }

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
    }

    private static Type RequireAutomationContractType(string typeName)
    {
        var assembly = typeof(Sussudio.Tools.AutomationCommandCatalog).Assembly;
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the automation contracts assembly.");
    }

    private static T GetConstant<T>(Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");
        return (T)field.GetRawConstantValue()!;
    }

    private static MethodInfo RequireNonPublicStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static object[] GetCatalogEntries(Type catalogType)
    {
        var entriesProperty = catalogType.GetProperty("Entries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationCommandCatalog.Entries not found.");
        return ((System.Collections.IEnumerable)entriesProperty.GetValue(null)!)
            .Cast<object>()
            .ToArray();
    }

    private static object[] GetMetadataCollection(object metadata, string name)
        => ((System.Collections.IEnumerable)GetMetadataProperty(metadata, name)!)
            .Cast<object>()
            .ToArray();

    private static void AssertPayloadFieldsMatchShape(object entry, string commandName, string payloadShape)
    {
        var expectedFields = ParsePayloadShape(payloadShape);
        var actualFields = GetMetadataCollection(entry, "PayloadFields");
        AssertEqual(expectedFields.Length, actualFields.Length, $"{commandName} typed payload field count");

        for (var i = 0; i < expectedFields.Length; i++)
        {
            var actual = actualFields[i];
            AssertEqual(expectedFields[i].Name, (string)GetMetadataProperty(actual, "Name")!, $"{commandName} payload field {i} name");
            AssertEqual(expectedFields[i].Type, GetMetadataProperty(actual, "Type")!.ToString(), $"{commandName} payload field {i} type");
            AssertEqual(expectedFields[i].Required, (bool)GetMetadataProperty(actual, "Required")!, $"{commandName} payload field {i} required");
        }

        var distinctNames = actualFields
            .Select(field => (string)GetMetadataProperty(field, "Name")!)
            .Distinct(StringComparer.Ordinal)
            .Count();
        AssertEqual(actualFields.Length, distinctNames, $"{commandName} unique typed payload field names");
    }

    private static (string Name, string Type, bool Required)[] ParsePayloadShape(string payloadShape)
    {
        var trimmed = payloadShape.Trim();
        if (string.Equals(trimmed, "{}", StringComparison.Ordinal))
        {
            return Array.Empty<(string Name, string Type, bool Required)>();
        }

        if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
            !trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported payload shape '{payloadShape}'.");
        }

        var inner = trimmed[1..^1].Trim();
        if (string.IsNullOrWhiteSpace(inner))
        {
            return Array.Empty<(string Name, string Type, bool Required)>();
        }

        return inner
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(fieldShape =>
            {
                var parts = fieldShape.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Unsupported payload field shape '{fieldShape}'.");
                }

                var rawName = parts[0];
                var required = !rawName.EndsWith("?", StringComparison.Ordinal);
                var name = required ? rawName : rawName[..^1];
                return (name, NormalizePayloadFieldType(parts[1]), required);
            })
            .ToArray();
    }

    private static string NormalizePayloadFieldType(string payloadType)
        => payloadType.Trim().ToLowerInvariant() switch
        {
            "string" => "String",
            "bool" => "Boolean",
            "int" => "Integer",
            "double" => "Number",
            "array" => "Array",
            "object" => "Object",
            _ => throw new InvalidOperationException($"Unsupported payload field type '{payloadType}'.")
        };

    private static void AssertCatalogMetadata(
        Type catalogType,
        Type enumType,
        Type pathPolicyType,
        string commandName,
        int timeoutMs,
        bool requiresReadyDevices,
        string pathPolicy,
        string payloadShapeContains)
    {
        var get = RequireNonPublicStaticMethod(catalogType, "Get");
        var enumValue = Enum.Parse(enumType, commandName);
        var metadata = get.Invoke(null, new[] { enumValue })
            ?? throw new InvalidOperationException($"Catalog metadata for {commandName} was null.");
        AssertEqual(commandName, (string)GetMetadataProperty(metadata, "Name")!, $"{commandName} catalog name");
        AssertEqual(timeoutMs, (int)GetMetadataProperty(metadata, "ResponseTimeoutMs")!, $"{commandName} catalog timeout");
        AssertEqual(requiresReadyDevices, (bool)GetMetadataProperty(metadata, "RequiresReadyDevices")!, $"{commandName} catalog readiness");
        AssertEqual(
            Enum.Parse(pathPolicyType, pathPolicy).ToString(),
            GetMetadataProperty(metadata, "PathPolicy")!.ToString(),
            $"{commandName} catalog path policy");
        AssertContains((string)GetMetadataProperty(metadata, "PayloadShape")!, payloadShapeContains);
    }

    private static object? GetMetadataProperty(object metadata, string name)
        => metadata.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
               ?.GetValue(metadata)
           ?? throw new InvalidOperationException($"Metadata property '{name}' was not found.");

    private static void AssertNotEmpty(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: expected non-empty text.");
        }
    }

    private static void AssertThrows<TException>(Action action, string fieldName)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is TException)
        {
            return;
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Assertion failed for {fieldName}: expected {typeof(TException).Name}.");
    }

    private static Task AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var expectedCommands = ExpectedAutomationCommands();
        AssertEqual(expectedCommands.Length, Enum.GetValues(enumType).Length, "AutomationCommandKind value count");

        for (int i = 0; i < expectedCommands.Length; i++)
        {
            var (name, value) = expectedCommands[i];
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"AutomationCommandKind.{name}");
            if (!Enum.IsDefined(enumType, value))
            {
                throw new InvalidOperationException(
                    $"AutomationCommandKind missing sequential value {value}.");
            }
        }

        return Task.CompletedTask;
    }

    internal static (string Name, int Value)[] ExpectedAutomationCommands() =>
    [
        ("Authenticate", 0),
        ("GetSnapshot", 1),
        ("GetDiagnostics", 2),
        ("RefreshDevices", 3),
        ("SelectDevice", 4),
        ("SelectAudioInputDevice", 5),
        ("SetCustomAudioInput", 6),
        ("SetResolution", 7),
        ("SetFrameRate", 8),
        ("SetRecordingFormat", 9),
        ("SetQuality", 10),
        ("SetCustomBitrate", 11),
        ("SetHdrEnabled", 12),
        ("SetAudioEnabled", 13),
        ("SetAudioPreviewEnabled", 14),
        ("SetOutputPath", 15),
        ("SetPreviewEnabled", 16),
        ("SetRecordingEnabled", 17),
        ("ArmClose", 18),
        ("WindowAction", 19),
        ("WaitForCondition", 20),
        ("VerifyLastRecording", 21),
        ("AssertSnapshot", 22),
        ("SetTrueHdrPreviewEnabled", 23),
        ("ProbeVideoSource", 24),
        ("ProbePreviewColor", 25),
        ("CapturePreviewFrame", 26),
        ("CaptureWindowScreenshot", 27),
        ("SetVideoFormat", 28),
        ("GetCaptureOptions", 29),
        ("SetPreset", 30),
        ("SetSplitEncodeMode", 31),
        ("SetMjpegDecoderCount", 32),
        ("SetShowAllCaptureOptions", 33),
        ("SetPreviewVolume", 34),
        ("SetStatsVisible", 35),
        ("SetDeviceAudioMode", 36),
        ("GetPerformanceTimeline", 37),
        ("SetStatsSectionVisible", 38),
        ("SetAnalogAudioGain", 39),
        ("SetSettingsVisible", 40),
        ("FlashbackAction", 41),
        ("FlashbackExport", 42),
        ("FlashbackGetSegments", 43),
        ("VerifyFile", 44),
        ("RestartFlashback", 45),
        ("SetMicrophoneEnabled", 46),
        ("SetFlashbackEnabled", 47),
        ("GetAudioRampTrace", 48),
        ("SetFrameTimeOverlayVisible", 49),
        ("SetFlashbackTimelineVisible", 50),
        ("GetAutomationManifest", 51),
        ("SetFullScreenEnabled", 52),
        ("OpenRecordingsFolder", 53)
    ];

    internal static Task AutomationCommandCatalog_CoversCommandsAndPolicyMetadata()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var catalogEntriesText = catalogText;
        var enumType = RequireAutomationContractType("Sussudio.Models.AutomationCommandKind");
        var pathPolicyType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandPathPolicy");
        var entriesProperty = catalogType.GetProperty("Entries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationCommandCatalog.Entries not found.");
        var entries = ((System.Collections.IEnumerable)entriesProperty.GetValue(null)!)
            .Cast<object>()
            .ToArray();
        var enumValues = Enum.GetValues(enumType).Cast<object>().ToArray();
        AssertEqual(enumValues.Length, entries.Length, "AutomationCommandCatalog entry count");

        foreach (var enumValue in enumValues)
        {
            var entry = entries.Single(candidate =>
                Convert.ToInt32(GetMetadataProperty(candidate, "Kind")) == Convert.ToInt32(enumValue));
            var payloadShape = (string)GetMetadataProperty(entry, "PayloadShape")!;
            AssertEqual(enumValue.ToString(), (string)GetMetadataProperty(entry, "Name")!, $"Catalog name for {enumValue}");
            AssertNotEmpty(payloadShape, $"Catalog payload shape for {enumValue}");
            AssertPayloadFieldsMatchShape(entry, enumValue.ToString()!, payloadShape);
            AssertEqual(true, (int)GetMetadataProperty(entry, "ResponseTimeoutMs")! > 0, $"Catalog timeout for {enumValue}");
            AssertNotEmpty((string)GetMetadataProperty(entry, "CliHelp")!, $"Catalog CLI help for {enumValue}");
            var mcpDescription = (string)GetMetadataProperty(entry, "McpDescription")!;
            AssertNotEmpty(mcpDescription, $"Catalog MCP description for {enumValue}");
            AssertEqual(false, mcpDescription == $"Automation command {enumValue}.", $"Catalog explicit MCP description for {enumValue}");
        }

        AssertContains(catalogText, "public static class AutomationCommandCatalog");
        AssertContains(catalogEntriesText, "private static IReadOnlyList<AutomationCommandMetadata> BuildEntries()");
        AssertContains(catalogEntriesText, "RegisterCaptureEntries(entries);");
        AssertContains(catalogEntriesText, "private static void RegisterCaptureEntries(");
        AssertContains(catalogEntriesText, "private static void RegisterFlashbackEntries(");
        AssertContains(catalogEntriesText, "Set(entries, AutomationCommandKind.SetRecordingEnabled");
        AssertContains(catalogEntriesText, "Set(entries, AutomationCommandKind.FlashbackExport");
        AssertContains(catalogText, "public enum AutomationCommandPathPolicy");
        AssertContains(catalogText, "public static AutomationManifest CreateManifest()");
        AssertContains(catalogText, "public static string CreateManifestJson()");
        AssertContains(catalogText, "public static string ValidatePath(");

        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetRecordingEnabled",
            timeoutMs: 150000,
            requiresReadyDevices: true,
            pathPolicy: "None",
            payloadShapeContains: "enabled");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "FlashbackExport",
            timeoutMs: 305000,
            requiresReadyDevices: false,
            pathPolicy: "WriteFile",
            payloadShapeContains: "outputPath");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "VerifyFile",
            timeoutMs: 60000,
            requiresReadyDevices: false,
            pathPolicy: "ReadFile",
            payloadShapeContains: "filePath");
        var verifyEntry = entries.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, "VerifyFile", StringComparison.Ordinal));
        AssertContains((string)GetMetadataProperty(verifyEntry, "CliHelp")!, "--profile");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetOutputPath",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "Directory",
            payloadShapeContains: "outputPath");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetResolution",
            timeoutMs: 15000,
            requiresReadyDevices: true,
            pathPolicy: "None",
            payloadShapeContains: "resolution");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetFlashbackEnabled",
            timeoutMs: 305000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "enabled");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "GetAutomationManifest",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "{}");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetFullScreenEnabled",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "enabled");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "OpenRecordingsFolder",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "{}");
        AssertOptionalPayloadField(entries, "WindowAction", "action");
        AssertOptionalPayloadField(entries, "WaitForCondition", "condition");

        return Task.CompletedTask;
    }

    private static void AssertOptionalPayloadField(
        object[] entries,
        string commandName,
        string fieldName)
    {
        var entry = entries.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, commandName, StringComparison.Ordinal));
        var fields = GetMetadataCollection(entry, "PayloadFields");
        var field = fields.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, fieldName, StringComparison.Ordinal));
        AssertEqual(false, (bool)GetMetadataProperty(field, "Required")!, $"{commandName}.{fieldName} catalog optional field");
    }

    internal static Task AutomationManifest_CoversCatalogMetadata()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var entries = GetCatalogEntries(catalogType);
        var createManifest = RequireNonPublicStaticMethod(catalogType, "CreateManifest");
        var manifest = createManifest.Invoke(null, Array.Empty<object>())
            ?? throw new InvalidOperationException("AutomationCommandCatalog.CreateManifest returned null.");

        AssertEqual(1, (int)GetMetadataProperty(manifest, "SchemaVersion")!, "Automation manifest schema version");
        var commands = GetMetadataCollection(manifest, "Commands");
        AssertEqual(entries.Length, commands.Length, "Automation manifest command count");

        foreach (var entry in entries)
        {
            var id = Convert.ToInt32(GetMetadataProperty(entry, "Kind"));
            var manifestCommand = commands.Single(command => (int)GetMetadataProperty(command, "Id")! == id);
            AssertEqual(id, (int)GetMetadataProperty(manifestCommand, "Id")!, $"Manifest id for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "Name")!, (string)GetMetadataProperty(manifestCommand, "Name")!, $"Manifest name for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "PayloadShape")!, (string)GetMetadataProperty(manifestCommand, "PayloadShape")!, $"Manifest payload shape for {id}");
            AssertEqual((int)GetMetadataProperty(entry, "ResponseTimeoutMs")!, (int)GetMetadataProperty(manifestCommand, "ResponseTimeoutMs")!, $"Manifest timeout for {id}");
            AssertEqual((bool)GetMetadataProperty(entry, "RequiresReadyDevices")!, (bool)GetMetadataProperty(manifestCommand, "RequiresReadyDevices")!, $"Manifest readiness for {id}");
            AssertEqual(GetMetadataProperty(entry, "PathPolicy")!.ToString(), (string)GetMetadataProperty(manifestCommand, "PathPolicy")!, $"Manifest path policy for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "CliHelp")!, (string)GetMetadataProperty(manifestCommand, "CliHelp")!, $"Manifest CLI help for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "McpDescription")!, (string)GetMetadataProperty(manifestCommand, "McpDescription")!, $"Manifest MCP description for {id}");

            var entryFields = GetMetadataCollection(entry, "PayloadFields");
            var manifestFields = GetMetadataCollection(manifestCommand, "PayloadFields");
            AssertEqual(entryFields.Length, manifestFields.Length, $"Manifest payload field count for {id}");
            for (var i = 0; i < entryFields.Length; i++)
            {
                AssertEqual((string)GetMetadataProperty(entryFields[i], "Name")!, (string)GetMetadataProperty(manifestFields[i], "Name")!, $"Manifest payload field name {id}[{i}]");
                AssertEqual(GetMetadataProperty(entryFields[i], "Type")!.ToString(), (string)GetMetadataProperty(manifestFields[i], "Type")!, $"Manifest payload field type {id}[{i}]");
                AssertEqual((bool)GetMetadataProperty(entryFields[i], "Required")!, (bool)GetMetadataProperty(manifestFields[i], "Required")!, $"Manifest payload field required {id}[{i}]");
            }
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var enumType = RequireAutomationContractType("Sussudio.Models.AutomationCommandKind");
        var entries = GetCatalogEntries(catalogType);
        var validatePath = RequireNonPublicStaticMethod(catalogType, "ValidatePath");
        var expectedPathFields = new Dictionary<string, (string Policy, string FieldName, bool Required)>
        {
            ["SetOutputPath"] = ("Directory", "outputPath", true),
            ["CapturePreviewFrame"] = ("WriteFile", "outputPath", false),
            ["CaptureWindowScreenshot"] = ("WriteFile", "outputPath", false),
            ["FlashbackExport"] = ("WriteFile", "outputPath", true),
            ["VerifyFile"] = ("ReadFile", "filePath", true)
        };

        var pathEntries = entries
            .Where(entry => !string.Equals(GetMetadataProperty(entry, "PathPolicy")!.ToString(), "None", StringComparison.Ordinal))
            .ToArray();
        AssertEqual(expectedPathFields.Count, pathEntries.Length, "Catalog path-policy command count");

        var dispatcherText = ReadAutomationCommandDispatcherFamilyText()
            .Replace("\r\n", "\n");
        foreach (var entry in pathEntries)
        {
            var commandName = (string)GetMetadataProperty(entry, "Name")!;
            if (!expectedPathFields.TryGetValue(commandName, out var expected))
            {
                throw new InvalidOperationException($"Unexpected path-bearing command '{commandName}'.");
            }

            AssertEqual(expected.Policy, GetMetadataProperty(entry, "PathPolicy")!.ToString(), $"{commandName} path policy");
            var fields = GetMetadataCollection(entry, "PayloadFields");
            var pathField = fields.SingleOrDefault(field =>
                string.Equals((string)GetMetadataProperty(field, "Name")!, expected.FieldName, StringComparison.Ordinal));
            if (pathField == null)
            {
                throw new InvalidOperationException($"{commandName} missing typed path payload field '{expected.FieldName}'.");
            }

            AssertEqual(expected.Required, (bool)GetMetadataProperty(pathField, "Required")!, $"{commandName} path field required flag");
            AssertEqual("String", GetMetadataProperty(pathField, "Type")!.ToString(), $"{commandName} path field type");
            AssertRegex(
                dispatcherText,
                $"ValidatePathPayload\\(\\n\\s*AutomationCommandKind\\.{commandName},\\n\\s*\"{expected.FieldName}\"",
                $"{commandName} dispatcher path validation");

            var enumValue = Enum.Parse(enumType, commandName);
            AssertThrows<InvalidOperationException>(
                () => validatePath.Invoke(null, new[] { enumValue, expected.FieldName, string.Empty }),
                $"{commandName} empty path validation");
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationManifest_SerializationIsStable()
    {
        const string ExpectedManifestSha256 = "2BBEEDA3AE61170E9BAC7A544069B7760640E598F69BF3012373B0ACB94997C4";
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var createManifestJson = RequireNonPublicStaticMethod(catalogType, "CreateManifestJson");
        var first = (string)createManifestJson.Invoke(null, Array.Empty<object>())!;
        var second = (string)createManifestJson.Invoke(null, Array.Empty<object>())!;

        AssertEqual(first, second, "Automation manifest repeated serialization");
        AssertDoesNotContain(first, "Timestamp");
        AssertDoesNotContain(first, "DateTime");
        AssertContains(first, "\"SchemaVersion\":1");
        AssertContains(first, "\"Name\":\"GetAutomationManifest\"");
        AssertContains(first, "\"PayloadFields\"");

        var actualSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first)));
        AssertEqual(ExpectedManifestSha256, actualSha256, "Automation manifest serialized SHA-256");

        return Task.CompletedTask;
    }

    internal static Task ReliabilityGates_RunToolsAndOfflineHarness()
    {
        var scriptText = ReadRepoFile("tools/reliability-gates.ps1")
            .Replace("\r\n", "\n");
        var diagnosticSessionText = ReadDiagnosticSessionRunnerSource();
        var diagnosticSessionCleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();

        AssertContains(scriptText, "$testProjectPath = Join-Path $repoRoot \"tests\\Sussudio.Tests\\Sussudio.Tests.csproj\"");
        AssertContains(scriptText, "$ssctlProjectPath = Join-Path $repoRoot \"tools\\ssctl\\ssctl.csproj\"");
        AssertContains(scriptText, "$mcpServerProjectPath = Join-Path $repoRoot \"tools\\McpServer\\McpServer.csproj\"");
        AssertContains(scriptText, "$nativeXuProbeProjectPath = Join-Path $repoRoot \"tools\\NativeXuAudioProbe\\NativeXuAudioProbe.csproj\"");
        AssertContains(scriptText, "-t:Rebuild");
        AssertContains(scriptText, "\"run\"");
        AssertContains(scriptText, "--no-build");
        AssertContains(scriptText, "$appAssemblyPath");
        AssertContains(scriptText, "Build, tool, and offline regression gates passed.");
        AssertDoesNotContain(scriptText, "docs/testing/README.md");

        AssertContains(diagnosticSessionCleanupActionsText, "var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);");
        AssertContains(diagnosticSessionCleanupActionsText, "CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs))");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = true }");
        return Task.CompletedTask;
    }
}

namespace Sussudio.Tests
{
public sealed class AutomationToolContractsProtocolXunitTests
{
    [Fact]
    public void SendAutomationCommand_HelperTracksAutomationContractsInputs()
    {
        var scriptText = RuntimeContractSource.ReadRepoFile("tools/send-automation-command.ps1")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("Get-AutomationClientInputWriteTimeUtc", scriptText);
        Assert.Contains("(Join-Path $PSScriptRoot \"AutomationClient\")", scriptText);
        Assert.Contains("(Join-Path $PSScriptRoot \"Common\")", scriptText);
        Assert.Contains("(Join-Path $repoRoot \"Sussudio.Automation.Contracts\")", scriptText);
        Assert.Contains("$_.Extension -in @(\".cs\", \".csproj\", \".props\", \".targets\")", scriptText);
        Assert.Contains("$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"", scriptText);
        Assert.DoesNotContain("Sussudio\\Models\\AutomationCommandKind.cs", scriptText);
        Assert.DoesNotContain("Models\\AutomationCommandKind.cs", scriptText);
    }

    [Fact]
    public void AutomationClient_UsesCatalogTimeoutPolicy_ForRecordingAndFlashbackCommands()
    {
        var protocolText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var catalogEntriesText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var clientText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var pipeClientText = RuntimeContractSource.ReadAutomationPipeClientSource();

        Assert.Contains("public const int DefaultResponseTimeoutMs = 15000;", protocolText);
        Assert.Contains("public const int ExtendedResponseTimeoutMs = 60000;", protocolText);
        Assert.Contains("public const int RecordingResponseTimeoutMs = 150000;", protocolText);
        Assert.Contains("public const int FlashbackMutationResponseTimeoutMs = 305000;", protocolText);
        Assert.Contains("commandName = ResolveCanonicalCommandName(commandName);", protocolText);
        Assert.Contains("AutomationCommandCatalog.TryGet(commandName, out var metadata)", protocolText);
        Assert.Contains("? metadata.ResponseTimeoutMs", protocolText);
        Assert.Contains("AutomationCommandKind.SetRecordingEnabled", catalogEntriesText);
        Assert.Contains("AutomationPipeProtocol.RecordingResponseTimeoutMs", catalogEntriesText);
        Assert.Contains("AutomationCommandKind.FlashbackExport", catalogEntriesText);
        Assert.Contains("AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs", catalogEntriesText);
        Assert.DoesNotContain("AlignResponseTimeoutWithServerRequest", protocolText);
        Assert.DoesNotContain("AlignResponseTimeoutWithServerRequest", pipeClientText);
        Assert.Contains("AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)", clientText);
        Assert.Contains("AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName)", clientText);
        Assert.Contains("public int? ResponseTimeoutMs { get; set; }", clientText);

        foreach (var acceptedName in new[] { "SetRecordingEnabled", "setrecordingenabled", "set-recording-enabled", "17" })
        {
            Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout(acceptedName));
        }

        Assert.Equal(15000, AutomationPipeProtocol.GetDefaultResponseTimeout("GetSnapshot"));
        Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));

        foreach (var acceptedName in new[] { "SetFlashbackEnabled", "set-flashback-enabled", "RestartFlashback" })
        {
            Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout(acceptedName));
        }
    }

    [Fact]
    public void AutomationClient_StaysAlignedWithAdvancedMcpCommandMap()
    {
        var protocolText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var scriptText = RuntimeContractSource.ReadRepoFile("tools/send-automation-command.ps1");

        foreach (var (kind, ordinal) in new[]
        {
            (AutomationCommandKind.GetCaptureOptions, 29),
            (AutomationCommandKind.SetPreset, 30),
            (AutomationCommandKind.SetSplitEncodeMode, 31),
            (AutomationCommandKind.SetMjpegDecoderCount, 32),
            (AutomationCommandKind.SetShowAllCaptureOptions, 33),
            (AutomationCommandKind.SetPreviewVolume, 34),
            (AutomationCommandKind.SetStatsVisible, 35)
        })
        {
            Assert.Equal(ordinal, (int)kind);
            Assert.Equal(ordinal, AutomationPipeProtocol.ResolveCommand(kind.ToString()));
        }

        Assert.Contains("Enum.GetValues<AutomationCommandKind>()", protocolText);

        Assert.Contains("AutomationClient\\AutomationClient.csproj", scriptText);
        Assert.Contains("Get-AutomationClientInputWriteTimeUtc", scriptText);
        Assert.Contains("Test-AutomationClientBuildFresh", scriptText);
        Assert.Contains("AutomationClient build failed with exit code $LASTEXITCODE.", scriptText);
        Assert.Contains("AutomationClient build output is stale after rebuild", scriptText);
        Assert.Contains("$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"", scriptText);
        Assert.Contains("\"--command\", $Command", scriptText);
        Assert.Contains("$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))", scriptText);
        Assert.Contains("\"--payload-base64\", $payloadBase64", scriptText);
        Assert.Contains("[int]$ResponseTimeoutMs = 0", scriptText);
        Assert.Contains("\"--response-timeout-ms\", $ResponseTimeoutMs", scriptText);
        Assert.DoesNotContain("function Resolve-AutomationCommand", scriptText);
    }

    [Fact]
    public void PipeClient_UsesSharedProtocol_ForCommandResolution()
    {
        var pipeClientText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs");

        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "McpServer", "PipeClient.cs")),
            "MCP PipeClient should stay with the host bootstrap owner instead of returning as a tiny adapter file.");
        Assert.Contains("AutomationPipeProtocol", pipeClientText);
        Assert.DoesNotContain("CommandMap = new", pipeClientText);
    }

    [Fact]
    public void UiAutomationAdapters_UseEnumCommands_WithoutChangingLabelsOrWireNames()
    {
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "PipeTransport.cs")),
            "ssctl PipeTransport should stay with the command-handler surface instead of returning as a tiny adapter file.");
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlTransportText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlUiText = ssctlTransportText;
        var ssctlFlashbackText = ssctlTransportText;
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var formatterText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Tools/ToolCommandFormatter.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var uiSettingsToolsText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("SendCommandAsync(\n        AutomationCommandKind kind,", ssctlPipeText);
        Assert.Contains("AutomationCommandTransport.SendCommandAsync(\n            _pipeName,\n            kind,", ssctlPipeText);
        Assert.DoesNotContain("AutomationCommandCatalog.Get(kind).Name", ssctlPipeText);
        Assert.Contains("HandleSimpleCommandAsync(\n        CommandContext context,\n        AutomationCommandKind kind,", ssctlTransportText);
        Assert.Contains("SendCommandAsync(\n            AutomationCommandKind kind,", mcpPipeText);
        Assert.Contains("AutomationCommandTransport.SendCommandAsync(\n                _pipeName,\n                kind,", mcpPipeText);
        Assert.DoesNotContain("AutomationCommandCatalog.Get(kind).Name", mcpPipeText);
        Assert.Contains("Optional(AutomationCommandKind kind, string label,", formatterText);
        Assert.Contains("ExecuteAndFormatResultAsync(\n        PipeClient pipeClient,\n        AutomationCommandKind kind,", formatterText);
        Assert.Contains("pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs)", formatterText);

        Assert.Contains("AutomationCommandKind.SetStatsVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetStatsSectionVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetSettingsVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetFrameTimeOverlayVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetFlashbackTimelineVisible", ssctlFlashbackText);
        Assert.DoesNotContain("\"SetStatsVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetStatsSectionVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetSettingsVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetFrameTimeOverlayVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetFlashbackTimelineVisible\"", ssctlFlashbackText);

        Assert.Contains("ToolCommandFormatter.Optional(AutomationCommandKind.SetStatsVisible, \"SetStatsVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetSettingsVisible, \"SetSettingsVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFrameTimeOverlayVisible, \"SetFrameTimeOverlayVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFlashbackTimelineVisible, \"SetFlashbackTimelineVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetStatsSectionVisible, \"SetStatsSectionVisible\"", uiSettingsToolsText);
    }

    [Fact]
    public void AutomationClient_UsesSharedProtocol_ForCommandResolution()
    {
        var entryText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.cs");
        var clientText = entryText;

        Assert.Contains("AutomationPipeProtocol", clientText);
        Assert.Contains("var options = ParseArgs(args);", entryText);
        Assert.Contains("var payload = BuildPayload(options);", entryText);
        Assert.Contains("public int? ResponseTimeoutMs { get; set; }", entryText);
        Assert.Contains("private static Options ParseArgs(string[] args)", entryText);
        Assert.Contains("private static void WriteHelp()", entryText);
        Assert.Contains("--payload-base64", entryText);
        Assert.Contains("private static object BuildPayload(Options options)", entryText);
        Assert.Contains("Convert.FromBase64String(options.PayloadBase64)", entryText);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "AutomationClient", "Program.Arguments.cs")),
            "AutomationClient argument parsing should stay with the low-level client entrypoint.");
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "AutomationClient", "Program.Payload.cs")),
            "AutomationClient payload construction should stay with the low-level client entrypoint.");
        Assert.DoesNotContain("CommandMap = new", clientText);
    }

    [Fact]
    public void AutomationPipeConnectFailures_AreClassifiedForCliAndMcp()
    {
        var sharedClientText = RuntimeContractSource.ReadAutomationPipeClientSource();
        var pipeClientText = RuntimeContractSource.ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "PipeTransport.cs")),
            "ssctl PipeTransport should stay with the command-handler surface instead of returning as a tiny adapter file.");
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionText = ReadDiagnosticSessionRunnerSource();
        var diagnosticSessionCommandChannelText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionPipeRetryText = diagnosticSessionCommandChannelText;
        var automationPipeProtocolText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("internal static class AutomationPipeClient", sharedClientText);
        Assert.DoesNotContain("internal static partial class AutomationPipeClient", sharedClientText);
        Assert.Contains("internal static async Task<string> SendRequestAsync(", sharedClientText);
        Assert.Contains("internal static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(", sharedClientText);
        Assert.Contains("AutomationCommandKind kind", sharedClientText);
        Assert.Contains("=> SendCommandWithResultAsync(\n            pipeName,\n            (int)kind,", sharedClientText);
        Assert.Contains("internal static bool TryReadResponseState(", sharedClientText);
        Assert.Contains("AutomationResponseState.TryRead(", sharedClientText);
        Assert.DoesNotContain("internal static class AutomationResponseState", sharedClientText);
        Assert.Contains("public static class AutomationResponseState", automationPipeProtocolText);
        Assert.Contains("public static bool TryRead(", automationPipeProtocolText);
        Assert.DoesNotContain("internal readonly record struct AutomationPipeCommandResult(", sharedClientText);
        Assert.Contains("public readonly record struct AutomationPipeCommandResult(", automationPipeProtocolText);
        Assert.Contains("public class AutomationPipeException : Exception", automationPipeProtocolText);
        Assert.Contains("public sealed class AutomationPipeConnectException : AutomationPipeException", automationPipeProtocolText);
        Assert.Contains("ConnectWithClassifiedErrorsAsync(", pipeClientText);
        Assert.Contains("await writer.WriteLineAsync(requestJson)", pipeClientText);
        Assert.Contains("private static async Task ConnectWithClassifiedErrorsAsync(", pipeClientText);
        Assert.Contains("await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);", pipeClientText);
        Assert.Contains("catch (TimeoutException ex)", pipeClientText);
        Assert.Contains("\"pipe-connect-timeout\"", pipeClientText);
        Assert.Contains("catch (OperationCanceledException)\n        {\n            throw;\n        }", pipeClientText);
        Assert.Contains("catch (UnauthorizedAccessException ex)", pipeClientText);
        Assert.Contains("\"pipe-access-denied\"", pipeClientText);
        Assert.Contains("AutomationPipeProtocol.AutomationKeyEnvVar", pipeClientText);
        Assert.Contains("catch (Exception ex)", pipeClientText);
        Assert.Contains("\"pipe-connect-failed\"", pipeClientText);
        Assert.Contains("public string ErrorCode { get; }", automationPipeProtocolText);

        Assert.Contains("AutomationCommandTransport.SendCommandAsync(", ssctlPipeText);
        Assert.Contains("kind,", ssctlPipeText);
        Assert.Contains("unknownCommandHandling: AutomationUnknownCommandHandling.ThrowArgumentException", ssctlPipeText);
        Assert.Contains("throw new UsageException(ex.Message);", ssctlPipeText);
        Assert.DoesNotContain("AutomationPipeClient.SendCommandWithResultAsync", ssctlPipeText);
        Assert.DoesNotContain("catch (AutomationPipeConnectException ex)", ssctlPipeText);
        Assert.DoesNotContain("AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode)", ssctlPipeText);
        Assert.DoesNotContain("private static JsonElement CreateSyntheticError", ssctlPipeText);
        Assert.DoesNotContain("Sussudio is not running or not responding. Start the app and try again.", ssctlPipeText);

        Assert.Contains("AutomationCommandTransport.SendCommandAsync(", mcpPipeText);
        Assert.Contains("kind,", mcpPipeText);
        Assert.Contains("unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError", mcpPipeText);
        Assert.DoesNotContain("AutomationPipeClient.SendCommandWithResultAsync", mcpPipeText);
        Assert.DoesNotContain("catch (AutomationPipeConnectException ex)", mcpPipeText);
        Assert.DoesNotContain("AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode)", mcpPipeText);
        Assert.DoesNotContain("private static JsonElement CreateSyntheticError", mcpPipeText);
        Assert.DoesNotContain("Sussudio is not running or not responding. Start the app and try again.", mcpPipeText);
        Assert.Contains("internal static class AutomationCommandTransport", sharedClientText);
        Assert.DoesNotContain("internal enum AutomationUnknownCommandHandling", sharedClientText);
        Assert.Contains("public enum AutomationUnknownCommandHandling", automationPipeProtocolText);
        Assert.Contains("ReturnSyntheticError", automationPipeProtocolText);
        Assert.Contains("ThrowArgumentException", automationPipeProtocolText);
        Assert.Contains("AutomationPipeProtocol.GetDefaultResponseTimeout(kind)", sharedClientText);
        Assert.Contains("AutomationSyntheticErrorResponse.Create(ex.Message, \"unknown-command\")", sharedClientText);
        Assert.Contains("catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))", sharedClientText);
        Assert.Contains("AutomationSyntheticErrorResponse.Create(ex)", sharedClientText);
        Assert.DoesNotContain("internal static class AutomationSyntheticErrorResponse", sharedClientText);
        Assert.Contains("public static class AutomationSyntheticErrorResponse", automationPipeProtocolText);
        Assert.Contains("[\"CommandLifecycle\"] = \"failed\"", automationPipeProtocolText);
        Assert.Contains("[\"Snapshot\"] = null", automationPipeProtocolText);
        Assert.Contains("public static bool CanCreateFromException(Exception exception)", automationPipeProtocolText);
        Assert.Contains("public static JsonElement Create(Exception exception)", automationPipeProtocolText);
        Assert.Contains("AutomationPipeConnectException ex => Create(ex.Message, ex.ErrorCode)", automationPipeProtocolText);
        Assert.Contains("AutomationPipeResponseTimeoutException ex => Create(ex.Message, \"pipe-response-timeout\")", automationPipeProtocolText);
        Assert.Contains("AutomationPipeProtocolException ex => Create(ex.Message, \"pipe-protocol-error\")", automationPipeProtocolText);
        Assert.Contains("\"pipe-invalid-json\"", automationPipeProtocolText);
        Assert.Contains("\"pipe-io-error\"", automationPipeProtocolText);
        Assert.Contains("\"pipe-canceled\"", automationPipeProtocolText);

        Assert.Contains("using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;", diagnosticSessionCommandChannelText);
        Assert.Contains("SendCommandWithConnectRetryAsync(", diagnosticSessionCommandChannelText);
        Assert.DoesNotContain("using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;", diagnosticSessionText);
        Assert.Contains("internal static class DiagnosticSessionPipeRetryPolicy", diagnosticSessionPipeRetryText);
        Assert.Contains("internal static async Task<JsonElement?> SendCommandWithConnectRetryAsync(", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-connect-failed\"", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-connect-timeout\"", diagnosticSessionPipeRetryText);
        Assert.Contains("IsPermanentPipeConnectFailure(ex.ErrorCode)", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-access-denied\"", diagnosticSessionPipeRetryText);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "Common", "DiagnosticSessionPipeRetryPolicy.cs")),
            "diagnostic-session pipe retry policy should stay with the command channel transport owner.");
        Assert.DoesNotContain("private static async Task<JsonElement?> SendCommandWithConnectRetryAsync(", diagnosticSessionText);
    }

    [Fact]
    public void AutomationSyntheticErrorResponse_CreatesStableErrorEnvelope()
    {
        var response = AutomationSyntheticErrorResponse.Create("boom", "pipe-boom");

        Assert.False(response.GetProperty("Success").GetBoolean());
        Assert.Equal("error", response.GetProperty("Status").GetString());
        Assert.Equal("failed", response.GetProperty("CommandLifecycle").GetString());
        Assert.Equal("boom", response.GetProperty("Message").GetString());
        Assert.Equal("pipe-boom", response.GetProperty("ErrorCode").GetString());
        Assert.Equal(JsonValueKind.Null, response.GetProperty("RetryAfterMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("ElapsedMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("Data").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("Snapshot").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(response.GetProperty("CorrelationId").GetString()));
        Assert.Equal(JsonValueKind.String, response.GetProperty("TimestampUtc").ValueKind);
    }

    [Fact]
    public void AutomationResponseState_ParsesStatusAndRetryContracts()
    {
        var responseStateType = RequireSharedToolType("Sussudio.Tools.AutomationResponseState");
        var tryRead = RequireNonPublicStaticMethod(responseStateType, "TryRead");

        AssertResponseState(
            tryRead,
            "{\"Success\":true,\"Status\":\"ready\",\"RetryAfterMs\":250}",
            expectedRead: true,
            expectedSuccess: true,
            expectedStatus: "ready",
            expectedRetryAfterMs: 250,
            "numeric retry");
        AssertResponseState(
            tryRead,
            "{\"Success\":false,\"RetryAfterMs\":\"500\"}",
            expectedRead: true,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: 500,
            "string retry");
        AssertResponseState(
            tryRead,
            "{\"Success\":\"true\",\"Status\":42,\"RetryAfterMs\":\"soon\"}",
            expectedRead: true,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: null,
            "malformed values");
        AssertResponseState(
            tryRead,
            "[]",
            expectedRead: false,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: null,
            "non-object response");
    }

    private static string ReadDiagnosticSessionRunnerSource()
        => string.Join(
            "\n",
            Directory.GetFiles(Path.Combine(FindRepoRoot(), "tools", "Common"), "DiagnosticSessionRunner*.cs")
                .Concat(Directory.GetFiles(Path.Combine(FindRepoRoot(), "tools", "Common"), "DiagnosticSessionRun*.cs"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)));

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = ToolFormatterTestAssembly.Load(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var type = assembly.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        var assemblyDirectory = Path.GetDirectoryName(assembly.Location)
                                ?? throw new InvalidOperationException("Shared tool assembly directory was not found.");
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            var referencePath = Path.Combine(assemblyDirectory, $"{reference.Name}.dll");
            if (!File.Exists(referencePath))
            {
                continue;
            }

            var referenceAssembly = Assembly.LoadFrom(referencePath);
            type = referenceAssembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly or its references.");
    }

    private static MethodInfo RequireNonPublicStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static void AssertResponseState(
        MethodInfo tryRead,
        string json,
        bool expectedRead,
        bool expectedSuccess,
        string? expectedStatus,
        int? expectedRetryAfterMs,
        string fieldName)
    {
        using var document = JsonDocument.Parse(json);
        var args = new object?[] { document.RootElement, null, null, null };
        Assert.Equal(expectedRead, (bool)tryRead.Invoke(null, args)!);
        Assert.Equal(expectedSuccess, (bool)args[1]!);
        Assert.Equal(expectedStatus, (string?)args[2]);
        var actualRetryAfterMs = args[3] is null ? (int?)null : Convert.ToInt32(args[3]);
        Assert.Equal(expectedRetryAfterMs, actualRetryAfterMs);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
}
