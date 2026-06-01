using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
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

    internal static Task AutomationSnapshotFormatter_RendersFlashbackSections_WhenIncluded()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var formatSnapshot = RequireNonPublicStaticMethod(formatterType, "FormatSnapshot");

        using var snapshotDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Ready",
                "StatusText": "OK",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "FlashbackActive": true,
                "EncoderCodecName": "hevc_nvenc",
                "EncoderFrameRate": 120,
                "EncoderFrameRateNumerator": 120,
                "EncoderFrameRateDenominator": 1,
                "EncoderTargetBitRate": 12345678,
                "FlashbackBufferedDurationMs": 120000,
                "FlashbackDiskBytes": 1048576,
                "FlashbackTotalBytesWritten": 2097152,
                "FlashbackTempDriveFreeBytes": 2147483648,
                "FlashbackStartupCacheBudgetBytes": 104857600,
                "FlashbackStartupCacheBytes": 52428800,
                "FlashbackStartupCacheSessionCount": 2,
                "FlashbackStartupCacheDeletedSessionCount": 1,
                "FlashbackStartupCacheFreedBytes": 26214400,
                "FlashbackStartupCacheOverBudget": false,
                "FlashbackBackendSettingsStale": true,
                "FlashbackBackendSettingsStaleReason": "preset:P1->P5",
                "FlashbackBackendActiveFormat": "HevcMp4",
                "FlashbackBackendRequestedFormat": "HevcMp4",
                "FlashbackBackendActivePreset": "P1",
                "FlashbackBackendRequestedPreset": "P5",
                "FlashbackPlaybackCommandQueueCapacity": 256,
                "FlashbackPlaybackPendingCommands": 1,
                "FlashbackPlaybackMaxPendingCommands": 4,
                "FlashbackPlaybackLastCommandQueueLatencyMs": 12,
                "FlashbackPlaybackMaxCommandQueueLatencyMs": 87,
                "FlashbackPlaybackMaxCommandQueueLatencyCommand": "Play",
                "FlashbackPlaybackCommandsEnqueued": 12,
                "FlashbackPlaybackCommandsProcessed": 11,
                "FlashbackPlaybackCommandsDropped": 0,
                "FlashbackPlaybackCommandsSkippedNotReady": 2,
                "FlashbackPlaybackSubmitFailures": 3,
                "FlashbackPlaybackScrubUpdatesCoalesced": 9,
                "FlashbackPlaybackSeekCommandsCoalesced": 5,
                "FlashbackPlaybackThreadAlive": true,
                "FlashbackPlaybackLastCommandQueued": "UpdateScrub",
                "FlashbackPlaybackLastCommandProcessed": "BeginScrub",
                "FlashbackPlaybackLastCommandFailure": "not_ready:Pause",
                "FlashbackPlaybackLastCommandFailureUtcUnixMs": 123456789,
                "FlashbackPlaybackTargetFps": 120,
                "FlashbackPlaybackFivePercentLowFps": 118,
                "FlashbackPlaybackSampleDurationMs": 1000,
                "FlashbackPlaybackDecodeSampleCount": 120,
                "FlashbackPlaybackDecodeAvgMs": 1.25,
                "FlashbackPlaybackDecodeP95Ms": 2.5,
                "FlashbackPlaybackDecodeP99Ms": 3.5,
                "FlashbackPlaybackDecodeMaxMs": 4.5,
                "FlashbackPlaybackMaxDecodePhase": "audio",
                "FlashbackPlaybackMaxDecodeReceiveMs": 0.5,
                "FlashbackPlaybackMaxDecodeFeedMs": 4.0,
                "FlashbackPlaybackMaxDecodeReadMs": 0.75,
                "FlashbackPlaybackMaxDecodeSendMs": 3.5,
                "FlashbackPlaybackMaxDecodeAudioMs": 3.25,
                "FlashbackPlaybackMaxDecodeConvertMs": 0.25,
                "FlashbackPlaybackMaxDecodePositionMs": 2345,
                "FlashbackPlaybackSeekForwardDecodeCapHits": 2,
                "FlashbackPlaybackLastSeekHitForwardDecodeCap": true,
                "FlashbackExportActive": true,
                "FlashbackExportStatus": "Running",
                "FlashbackExportId": 7,
                "FlashbackExportPercent": 37.5,
                "FlashbackExportSegmentsProcessed": 3,
                "FlashbackExportTotalSegments": 8,
                "FlashbackExportInPointMs": 1000,
                "FlashbackExportOutPointMs": 9000,
                "FlashbackExportLastProgressUtcUnixMs": 123456,
                "FlashbackExportCompletedUtcUnixMs": 0,
                "FlashbackExportElapsedMs": 2500,
                "FlashbackExportLastProgressAgeMs": 150,
                "FlashbackExportOutputBytes": 1048576,
                "FlashbackExportThroughputBytesPerSec": 419430.4,
                "FlashbackExportOutputPath": "C:/tmp/flashback.mp4",
                "FlashbackExportMessage": "copying packets",
                "FlashbackExportFailureKind": "NoMediaWritten",
                "FlashbackExportForceRotateFallbacks": 1,
                "FlashbackExportLastForceRotateFallbackUtcUnixMs": 12345,
                "FlashbackExportLastForceRotateFallbackSegments": 2,
                "FlashbackExportLastForceRotateFallbackInPointMs": 1000,
                "FlashbackExportLastForceRotateFallbackOutPointMs": 9000,
                "LastExportId": 7
              }
            }
            """);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string formatted;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, true })!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(formatted, "== Flashback ==");
        AssertContains(formatted, "Encoder: hevc_nvenc 0x0 @ 120 fps (120/1) | Target: 12.3 Mbps");
        AssertContains(formatted, "Buffer: 120.0s | Disk: 1.0 MB | Written: 2 MB");
        AssertContains(formatted, "Temp Cache: cache=50 MB budget=100 MB free=2 GB sessions=2 deleted=1 freed=25 MB overBudget=false");
        AssertContains(formatted, "backendStale=true staleReason=preset:P1->P5 active=HevcMp4/P1 requested=HevcMp4/P5");
        AssertContains(formatted, "submitFailures=3");
        AssertContains(formatted, "Playback Commands: pending=1/256 maxPending=4 lastLatency=12ms maxLatency=87ms maxLatencyCommand=Play enq=12 proc=11 drop=0 skip=2 coalescedScrub=9 coalescedSeek=5 threadAlive=true lastQueued=UpdateScrub lastProcessed=BeginScrub failure=not_ready:Pause failureUtc=123456789");
        AssertContains(formatted, "Target: 120 fps");
        AssertContains(formatted, "5% Low: 118 fps");
        AssertContains(formatted, "Playback Decode: avg=1.25ms P95=2.5ms P99=3.5ms max=4.5ms phase=audio receive=0.5ms feed=4.0ms read=0.75ms send=3.5ms audio=3.25ms convert=0.25ms maxPos=2345ms samples=120 seekCapHits=2 lastSeekCap=true");
        AssertContains(formatted, "Export: active=true status=Running id=7 lastResultId=7 kind=NoMediaWritten progress=37.5% segments=3/8");
        AssertContains(formatted, "elapsed=2500ms progressAge=150ms bytes=1 MB throughput=409.6 KB/s");
        AssertContains(formatted, "forceRotateFallbacks=1 lastForceRotateFallbackSegments=2 lastForceRotateFallbackUtc=12345");

        var omittedFlashbackFormatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, false })!;
        AssertDoesNotContain(omittedFlashbackFormatted, "== Flashback ==");
        AssertDoesNotContain(omittedFlashbackFormatted, "Playback Commands:");
        AssertDoesNotContain(omittedFlashbackFormatted, "Flashback Failure:");

        using var failedFlashbackDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Error",
                "StatusText": "Flashback failed",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "FlashbackActive": false,
                "FlashbackEncodingFailed": true,
                "FlashbackEncodingFailureType": "InvalidOperationException",
                "FlashbackEncodingFailureMessage": "Flashback queue overloaded",
                "FlashbackForceRotateActive": true
              }
            }
            """);
        var failedFlashbackFormatted = (string)formatSnapshot.Invoke(null, new object[] { failedFlashbackDoc.RootElement, true })!;
        AssertContains(failedFlashbackFormatted, "== Flashback ==");
        AssertContains(failedFlashbackFormatted, "forceRotate=true");
        AssertContains(failedFlashbackFormatted, "Flashback Failure: active=true type=InvalidOperationException msg=Flashback queue overloaded");

        return Task.CompletedTask;
    }

    internal static Task AutomationSnapshotFormatter_RendersPreviewD3DSections()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var formatSnapshot = RequireNonPublicStaticMethod(formatterType, "FormatSnapshot");

        using var snapshotDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Ready",
                "StatusText": "OK",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "PreviewRendererMode": "D3D11VideoProcessor",
                "PreviewStartupState": "Rendering",
                "PreviewFirstVisualConfirmed": true,
                "PreviewD3DCpuTimingSampleCount": 120,
                "PreviewD3DInputUploadCpuAvgMs": 0.1,
                "PreviewD3DInputUploadCpuP95Ms": 0.2,
                "PreviewD3DInputUploadCpuP99Ms": 0.3,
                "PreviewD3DInputUploadCpuMaxMs": 0.4,
                "PreviewD3DRenderSubmitCpuAvgMs": 0.5,
                "PreviewD3DRenderSubmitCpuP95Ms": 0.6,
                "PreviewD3DRenderSubmitCpuP99Ms": 0.7,
                "PreviewD3DRenderSubmitCpuMaxMs": 0.8,
                "PreviewD3DPresentCallAvgMs": 0.9,
                "PreviewD3DPresentCallP95Ms": 1.0,
                "PreviewD3DPresentCallP99Ms": 1.1,
                "PreviewD3DPresentCallMaxMs": 1.2,
                "PreviewD3DTotalFrameCpuAvgMs": 1.3,
                "PreviewD3DTotalFrameCpuP95Ms": 1.4,
                "PreviewD3DTotalFrameCpuP99Ms": 1.5,
                "PreviewD3DTotalFrameCpuMaxMs": 1.6,
                "PreviewD3DPipelineLatencySampleCount": 120,
                "PreviewD3DPipelineLatencyAvgMs": 7.8,
                "PreviewD3DPipelineLatencyP95Ms": 8.9,
                "PreviewD3DPipelineLatencyP99Ms": 9.9,
                "PreviewD3DPipelineLatencyMaxMs": 12.3,
                "PreviewD3DLastRenderedPipelineLatencyMs": 8.4,
                "PreviewD3DFrameLatencyWaitEnabled": true,
                "PreviewD3DFrameLatencyWaitHandleActive": true,
                "PreviewD3DFrameLatencyWaitCallCount": 118,
                "PreviewD3DFrameLatencyWaitSignaledCount": 110,
                "PreviewD3DFrameLatencyWaitTimeoutCount": 8,
                "PreviewD3DFrameLatencyWaitUnexpectedResultCount": 0,
                "PreviewD3DFrameLatencyWaitLastResult": 0,
                "PreviewD3DFrameLatencyWaitLastMs": 0.05,
                "PreviewD3DFrameLatencyWaitSampleCount": 118,
                "PreviewD3DFrameLatencyWaitAvgMs": 0.2,
                "PreviewD3DFrameLatencyWaitP95Ms": 0.8,
                "PreviewD3DFrameLatencyWaitP99Ms": 1.4,
                "PreviewD3DFrameLatencyWaitMaxMs": 2.0,
                "PreviewD3DFrameStatsSampleCount": 120,
                "PreviewD3DFrameStatsSuccessCount": 119,
                "PreviewD3DFrameStatsFailureCount": 1,
                "PreviewD3DFrameStatsRecentFailureCount": 1,
                "PreviewD3DFrameStatsMissedRefreshCount": 4,
                "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                "PreviewD3DFrameStatsLastError": "DXGI_ERROR_WAS_STILL_DRAWING",
                "PreviewD3DLastSubmittedPreviewPresentId": 41,
                "PreviewD3DLastSubmittedSourceSequenceNumber": 9000,
                "PreviewD3DLastSubmittedSourcePtsTicks": 123456,
                "PreviewD3DLastRenderedPreviewPresentId": 42,
                "PreviewD3DLastRenderedSourceSequenceNumber": 9001,
                "PreviewD3DLastRenderedSourcePtsTicks": 123789,
                "PreviewD3DLastRenderedSchedulerToPresentMs": 7.7,
                "PreviewD3DLastDropReason": "none",
                "PreviewD3DLastDroppedSourcePtsTicks": 0,
                "PreviewD3DRecentSlowFrames": [
                  {
                    "PreviewPresentId": 42,
                    "SourceSequenceNumber": 9001,
                    "PresentIntervalMs": 9.2,
                    "InputUploadCpuMs": 1.1,
                    "RenderSubmitCpuMs": 2.2,
                    "PresentCallMs": 3.3,
                    "TotalFrameCpuMs": 6.6,
                    "SchedulerToPresentMs": 7.7,
                    "PipelineLatencyMs": 8.8,
                    "ExpectedIntervalMs": 8.33,
                    "DiagnosticThresholdMs": 8.5,
                    "WorstOverBudgetMs": 0.87,
                    "SlowReason": "present_interval",
                    "PendingFrameCount": 1,
                    "DxgiPresentDelta": 1,
                    "DxgiPresentRefreshDelta": 2,
                    "DxgiSyncRefreshDelta": 2
                  }
                ],
                "SourceWidth": 3840,
                "SourceHeight": 2160
              }
            }
            """);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string formatted;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, false })!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(formatted, "== Preview ==");
        AssertContains(formatted, "D3D CPU timing: input/upload avg=0.1ms P95=0.2ms P99=0.3ms max=0.4ms | render-submit avg=0.5ms P95=0.6ms P99=0.7ms max=0.8ms | present-call avg=0.9ms P95=1.0ms P99=1.1ms max=1.2ms | total-frame avg=1.3ms P95=1.4ms P99=1.5ms max=1.6ms samples=120");
        AssertContains(formatted, "D3D pipeline latency: avg=7.8ms P95=8.9ms P99=9.9ms max=12.3ms last=8.4ms samples=120");
        AssertContains(formatted, "D3D frame-latency wait: enabled=true handle=true calls=118 signaled=110 timeouts=8 unexpected=0 lastResult=0 last=0.05ms avg=0.2ms P95=0.8ms max=2.0ms samples=118");
        AssertContains(formatted, "D3D DXGI stats: ok=119/120 failures=1 recentFailures=1 missedRefresh=4 recentMissed=2 lastError=DXGI_ERROR_WAS_STILL_DRAWING");
        AssertContains(formatted, "D3D Ownership: submitted present=41 sourceSeq=9000 pts=123456 | rendered present=42 sourceSeq=9001 pts=123789 schedulerToPresent=7.7ms pipeline=8.4ms | lastDrop=none dropPts=0");
        AssertContains(formatted, "D3D Slow Frames: present=42 srcSeq=9001 reason=present_interval target=8.33ms over=0.87ms interval=9.20ms");
        AssertContains(formatted, "presentCall=3.30ms sched=7.70ms pipeline=8.80ms");
        AssertOccursBefore(formatted, "D3D CPU timing:", "D3D pipeline latency:");
        AssertOccursBefore(formatted, "D3D pipeline latency:", "D3D frame-latency wait:");
        AssertOccursBefore(formatted, "D3D frame-latency wait:", "D3D DXGI stats:");
        AssertOccursBefore(formatted, "D3D DXGI stats:", "D3D Ownership:");
        AssertOccursBefore(formatted, "D3D Ownership:", "D3D Slow Frames:");

        return Task.CompletedTask;
    }

    internal static Task AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var isSuccess = RequireNonPublicStaticMethod(formatterType, "IsSuccess");
        var get = RequireNonPublicStaticMethod(formatterType, "Get");
        var getInt = RequireNonPublicStaticMethod(formatterType, "GetInt");
        var getDouble = RequireNonPublicStaticMethod(formatterType, "GetDouble");
        var getLong = RequireNonPublicStaticMethod(formatterType, "GetLong");
        var computeTickAge = RequireNonPublicStaticMethod(formatterType, "ComputeTickAgeMs");
        var formatSnapshot = RequireNonPublicStaticMethod(formatterType, "FormatSnapshot");

        using var accessorsDoc = JsonDocument.Parse(
            "{\"Success\":true,\"Name\":\"Camera\",\"Count\":\"42\",\"Rate\":\"59.94\",\"Bytes\":123456789,\"Items\":[1],\"Empty\":[],\"Missing\":null}");
        var accessors = accessorsDoc.RootElement;
        AssertEqual(true, (bool)isSuccess.Invoke(null, new object[] { accessors })!, "AutomationSnapshotFormatter.IsSuccess true");
        AssertEqual("Camera", (string)get.Invoke(null, new object[] { accessors, "Name", "fallback" })!, "AutomationSnapshotFormatter.Get string");
        AssertEqual("true", (string)get.Invoke(null, new object[] { accessors, "Success", "fallback" })!, "AutomationSnapshotFormatter.Get bool");
        AssertEqual("fallback", (string)get.Invoke(null, new object[] { accessors, "Empty", "fallback" })!, "AutomationSnapshotFormatter.Get empty array fallback");
        AssertEqual("fallback", (string)get.Invoke(null, new object[] { accessors, "Missing", "fallback" })!, "AutomationSnapshotFormatter.Get null fallback");
        AssertEqual(42, (int)getInt.Invoke(null, new object[] { accessors, "Count", 0 })!, "AutomationSnapshotFormatter.GetInt string");
        AssertEqual(59.94d, (double)getDouble.Invoke(null, new object[] { accessors, "Rate", 0d })!, "AutomationSnapshotFormatter.GetDouble string");
        AssertEqual(123456789L, (long)getLong.Invoke(null, new object[] { accessors, "Bytes", 0L })!, "AutomationSnapshotFormatter.GetLong number");
        AssertEqual(-1L, (long)computeTickAge.Invoke(null, new object[] { 0L })!, "AutomationSnapshotFormatter.ComputeTickAgeMs non-positive");

        using var invalidDoc = JsonDocument.Parse("[]");
        AssertEqual(
            "Snapshot response was not a JSON object.",
            (string)formatSnapshot.Invoke(null, new object[] { invalidDoc.RootElement, false })!,
            "AutomationSnapshotFormatter non-object response");

        using var missingSnapshotDoc = JsonDocument.Parse("{\"Message\":\"Snapshot warming up\"}");
        AssertEqual(
            "Snapshot warming up",
            (string)formatSnapshot.Invoke(null, new object[] { missingSnapshotDoc.RootElement, false })!,
            "AutomationSnapshotFormatter missing snapshot message");

        using var snapshotDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Ready",
                "StatusText": "OK",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "IsInitialized": true,
                "IsPreviewing": true,
                "IsRecording": false,
                "SelectedResolution": "3840x2160",
                "SelectedFriendlyFrameRate": "59.94",
                "SelectedExactFrameRate": "59.940",
                "SelectedExactFrameRateArg": "60000/1001",
                "SelectedRecordingFormat": "HevcMp4",
                "SelectedQuality": "High",
                "SelectedPreset": "P5",
                "SelectedVideoFormat": "MJPG",
                "SelectedSplitEncodeMode": "Auto",
                "PreviewVolumePercent": 42.5,
                "IsStatsVisible": true,
                "IsHdrEnabled": false,
                "IsHdrAvailable": true,
                "HdrOutputActive": false,
                "HdrRuntimeState": "Inactive",
                "RequestedPipelineMode": "SDR",
                "ActivePipelineMode": "SDR",
                "PipelineModeMatched": true,
                "IsAudioEnabled": true,
                "IsAudioPreviewEnabled": false,
                "IsCustomAudioInputEnabled": false,
                "AudioPeak": 0,
                "AudioClipping": false,
                "AudioSignalPresent": false,
                "AudioReaderActive": false,
                "AudioFramesArrived": 0,
                "AudioFramesWrittenToSink": 0,
                "VideoReaderActive": true,
                "IngestVideoFramesArrived": 120,
                "IngestVideoFramesWrittenToSink": 120,
                "EncoderVideoFramesEnqueued": 0,
                "EncoderVideoFramesEncoded": 0,
                "FfmpegVideoQueueDepth": 0,
                "VideoDropsQueueSaturated": 0,
                "IngestLastVideoFrameAgeMs": 5,
                "EncoderLastEnqueueAgeMs": 0,
                "EncoderLastWriteAgeMs": 0,
                "MemoryPreference": "Gpu",
                "VideoRequestedSubtype": "MJPG",
                "VideoNegotiatedSubtype": "MJPG",
                "VideoIngestErrorCount": 0,
                "SourceReaderReadOutstanding": false,
                "SourceReaderReadOutstandingMs": 0,
                "SourceReaderLastFrameTickMs": 0,
                "SourceReaderFrameChannelDepth": 0,
                "WasapiCaptureCallbackCount": 0,
                "WasapiCaptureCallbackAvgIntervalMs": 0,
                "WasapiCaptureCallbackMaxIntervalMs": 0,
                "WasapiCaptureCallbackSilenceCount": 0,
                "WasapiCaptureLastCallbackTickMs": 0,
                "WasapiCaptureAudioLevelEventsFired": 0,
                "WasapiPlaybackRenderCallbackCount": 0,
                "WasapiPlaybackRenderSilenceCount": 0,
                "WasapiPlaybackQueueDepth": 0,
                "WasapiPlaybackQueueDropCount": 0,
                "WasapiPlaybackLastRenderTickMs": 0,
                "OutputPath": "",
                "RecordingTime": "00:00:00",
                "RecordingSizeInfo": "0 B",
                "RecordingBitrateInfo": "0 Mbps",
                "RecordingBackend": "None",
                "AudioPathMode": "None",
                "MuxResult": "NotAttempted",
                "LastOutputPath": "",
                "LastOutputSizeBytes": 0,
                "LastFinalizeStatus": "None",
                "FlashbackActive": true,
                "FlashbackEncodingFailed": true,
                "DiagnosticHealthStatus": "Healthy",
                "DiagnosticLikelyStage": "None",
                "DiagnosticSummary": "OK",
                "DiagnosticEvidence": "stable",
                "DiagnosticSourceLane": "ok",
                "DiagnosticDecodeLane": "ok",
                "DiagnosticPreviewLane": "ok",
                "DiagnosticRenderLane": "ok",
                "DiagnosticPresentLane": "ok",
                "DiagnosticRecordingLane": "idle",
                "DiagnosticAudioLane": "idle",
                "PerformanceScore": 100,
                "PerformancePerfectionMet": true,
                "PerformanceSummary": "OK",
                "EstimatedPipelineLatencyMs": 1,
                "ProcessCpuPercent": 1.5,
                "ProcessCpuTotalProcessorTimeMs": 1200,
                "MemoryWorkingSetMb": 256,
                "MemoryPrivateBytesMb": 128,
                "MemoryManagedHeapMb": 16,
                "MemoryTotalAllocatedMb": 64,
                "MemoryGcHeapSizeMb": 32,
                "MemoryGcGen0Collections": 1,
                "MemoryGcGen1Collections": 0,
                "MemoryGcGen2Collections": 0,
                "MemoryGcPauseTimePercent": 0,
                "MemoryGcFragmentationPercent": 0,
                "ThreadPoolWorkerAvailable": 32766,
                "ThreadPoolWorkerMax": 32767,
                "ThreadPoolIoAvailable": 1000,
                "ThreadPoolIoMax": 1000,
                "CaptureCadenceObservedFps": 120,
                "ExpectedCaptureFrameRate": 120,
                "CaptureCadenceSampleCount": 300,
                "CaptureCadenceAverageIntervalMs": 8.3,
                "CaptureCadenceP95IntervalMs": 8.5,
                "CaptureCadenceP99IntervalMs": 8.7,
                "CaptureCadenceMaxIntervalMs": 9.0,
                "CaptureCadenceFivePercentLowFps": 119,
                "CaptureCadenceOnePercentLowFps": 118,
                "CaptureCadenceSampleDurationMs": 2500,
                "CaptureCadenceJitterStdDevMs": 0.1,
                "CaptureCadenceSevereGapCount": 0,
                "CaptureCadenceEstimatedDroppedFrames": 0,
                "CaptureCadenceEstimatedDropPercent": 0,
                "MjpegDecodeSampleCount": 1,
                "MjpegDecodeAvgMs": 2.1,
                "MjpegDecodeP95Ms": 3.1,
                "MjpegDecodeMaxMs": 4.1,
                "MjpegDecoderCount": 1,
                "MjpegPerDecoder": [
                  { "WorkerIndex": 0, "AvgMs": 2.1, "P95Ms": 3.1, "MaxMs": 4.1, "SampleCount": 5 }
                ],
                "AvSyncCaptureDriftMs": 1.5,
                "AvSyncCaptureDriftRateMsPerSec": 0.1,
                "AvSyncEncoderDriftMs": -0.5,
                "AvSyncEncoderCorrectionSamples": 2,
                "PreviewRendererMode": "Software",
                "PreviewStartupState": "Rendering",
                "PreviewFirstVisualConfirmed": true,
                "PreviewCadenceObservedFps": 120,
                "DetectedSourceFrameRate": 120,
                "SourceWidth": 3840,
                "SourceHeight": 2160,
                "SourceIsHdr": false,
                "SourceTelemetryAvailability": "Available",
                "SourceTelemetryConfidence": "High"
              }
            }
            """);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string formatted;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, false })!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(formatted, "== Sussudio State ==");
        AssertContains(formatted, "Device: Synthetic (dev-1)");
        AssertContains(formatted, "Frame Rate: 59.94 fps (59.940 fps, 60000/1001)");
        AssertContains(formatted, "== Thread Health ==");
        AssertContains(formatted, "WASAPI Playback:");
        AssertContains(formatted, "== Diagnostics ==");
        AssertContains(formatted, "Legacy Score:");
        AssertContains(formatted, "Pipeline Latency: 1ms (app receive -> estimated visible)");
        AssertContains(formatted, "Process CPU: 1.5%");
        AssertContains(formatted, "== MJPEG Pipeline Timing ==");
        AssertContains(formatted, "Decoder[0]: avg=2.1ms");
        AssertContains(formatted, "== AV Sync ==");
        AssertContains(formatted, "Capture Drift: 1.5ms | Rate: 0.1ms/s");
        AssertContains(formatted, "Encoder Drift: -0.5ms | Correction Samples: 2");
        AssertContains(formatted, "== Preview ==");
        AssertContains(formatted, "== Source ==");
        AssertDoesNotContain(formatted, "== Flashback ==");
        AssertDoesNotContain(formatted, "Flashback Failure:");

        AssertOccursBefore(formatted, "== Sussudio State ==", "== Capture Settings ==");
        AssertOccursBefore(formatted, "== Capture Settings ==", "== Audio ==");
        AssertOccursBefore(formatted, "== Audio ==", "== Video Pipeline ==");
        AssertOccursBefore(formatted, "== Video Pipeline ==", "== Thread Health ==");
        AssertOccursBefore(formatted, "== Thread Health ==", "== Recording ==");
        AssertOccursBefore(formatted, "== Recording ==", "== Diagnostics ==");
        AssertOccursBefore(formatted, "== Diagnostics ==", "== Performance ==");
        AssertOccursBefore(formatted, "== Performance ==", "== Memory & GC ==");
        AssertOccursBefore(formatted, "== Memory & GC ==", "== Capture Cadence ==");
        AssertOccursBefore(formatted, "== Capture Cadence ==", "== MJPEG Pipeline Timing ==");
        AssertOccursBefore(formatted, "== MJPEG Pipeline Timing ==", "== AV Sync ==");
        AssertOccursBefore(formatted, "== AV Sync ==", "== Preview ==");
        AssertOccursBefore(formatted, "== Preview ==", "== Source ==");

        return Task.CompletedTask;
    }

    internal static Task AutomationSnapshotFormatter_SourceOwnership_IsSplit()
    {
        var sharedFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadAutomationSnapshotFormatterSource();
        var sharedFormatterRootSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.cs");
        var sharedFormatterCoreSectionsSource = sharedFormatterRootSource;
        var sharedFormatterAudioSource = sharedFormatterRootSource;
        var sharedFormatterRecordingSource = sharedFormatterRootSource;
        var sharedFormatterProcessResourcesSource = sharedFormatterRootSource;
        var sharedFormatterCaptureSettingsSource = sharedFormatterRootSource;
        var sharedFormatterVideoPipelineSource = sharedFormatterRootSource;
        var sharedFormatterDiagnosticsSource = sharedFormatterRootSource;
        var sharedFormatterCaptureCadenceSource = sharedFormatterRootSource;
        var sharedFormatterAvSyncSource = sharedFormatterCaptureCadenceSource;
        var sharedFormatterSourceSource = sharedFormatterCaptureCadenceSource;
        var sharedFormatterValuesSource = sharedFormatterRootSource;
        var sharedFormatterDisplayValuesSource = sharedFormatterValuesSource;
        var sharedFormatterFlashbackSource = sharedFormatterRootSource;
        var sharedFormatterMjpegTimingSource = sharedFormatterRootSource;
        var sharedFormatterPreviewSource = sharedFormatterCaptureCadenceSource;
        var sharedFormatterPreviewD3DSource = sharedFormatterRootSource;
        var sharedFormatterThreadHealthSource = sharedFormatterVideoPipelineSource;
        AssertContains(sharedFormatterRootSource, "AppendStateSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendCaptureSettingsSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendAudioSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendVideoPipelineSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendRecordingSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendFlashbackSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendDiagnosticsSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendPerformanceSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendMemorySection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendCaptureCadenceSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertContains(sharedFormatterRootSource, "var selectedFriendlyFrameRate = Get(snapshot, \"SelectedFriendlyFrameRate\", string.Empty);");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Audio ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Recording ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Performance ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Memory & GC ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Capture Cadence ==\");");
        AssertContains(sharedFormatterRootSource, "RecordingIntegrityStatus");
        AssertContains(sharedFormatterRootSource, "ProcessCpuPercent");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendStateSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertContains(sharedFormatterCoreSectionsSource, "CaptureCommandLastCorrelationId");
        AssertContains(sharedFormatterCaptureSettingsSource, "private static void AppendCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureSettingsSource, "private static string FormatFrameRateSummary(JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureSettingsSource, "SelectedFriendlyFrameRate");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendAudioSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "AudioFramesWrittenToSink");
        AssertContains(sharedFormatterAudioSource, "private static void AppendAudioSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterAudioSource, "builder.AppendLine(\"== Audio ==\");");
        AssertContains(sharedFormatterAudioSource, "AudioFramesWrittenToSink");
        AssertContains(sharedFormatterVideoPipelineSource, "private static void AppendVideoPipelineSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterVideoPipelineSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertContains(sharedFormatterVideoPipelineSource, "RecordingVideoQueueLatencyP99Ms");
        AssertContains(sharedFormatterVideoPipelineSource, "AppendThreadHealthSection(builder, snapshot);");
        AssertContains(sharedFormatterVideoPipelineSource, "private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterVideoPipelineSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendRecordingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "RecordingIntegrityStatus");
        AssertContains(sharedFormatterRecordingSource, "private static void AppendRecordingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterRecordingSource, "builder.AppendLine(\"== Recording ==\");");
        AssertContains(sharedFormatterRecordingSource, "RecordingIntegrityStatus");
        AssertContains(sharedFormatterDiagnosticsSource, "private static void AppendDiagnosticsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterDiagnosticsSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertContains(sharedFormatterDiagnosticsSource, "DiagnosticEvidence");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "ProcessCpuPercent");
        AssertContains(sharedFormatterProcessResourcesSource, "private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterProcessResourcesSource, "Pipeline Latency: {Get(snapshot, \"EstimatedPipelineLatencyMs\")}ms (app receive -> estimated visible)");
        AssertContains(sharedFormatterProcessResourcesSource, "private static void AppendMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterProcessResourcesSource, "ProcessCpuPercent");
        AssertContains(sharedFormatterProcessResourcesSource, "ThreadPoolWorkerAvailable");
        AssertContains(sharedFormatterCaptureCadenceSource, "private static void AppendCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureCadenceSource, "FormatFrameBudgetMs(snapshot, \"ExpectedCaptureFrameRate\")");
        AssertContains(sharedFormatterCaptureCadenceSource, "MjpegPacketHashInputObservedFps");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendMjpegTimingSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendAvSyncSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendPreviewSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendSourceSection(builder, snapshot);");
        AssertContains(sharedFormatterValuesSource, "internal static bool IsSuccess(JsonElement response)");
        AssertContains(sharedFormatterValuesSource, "response.TryGetProperty(\"Success\", out var success)");
        AssertContains(sharedFormatterValuesSource, "internal static string Get(JsonElement element, string propertyName, string fallback = \"N/A\")");
        AssertContains(sharedFormatterValuesSource, "internal static bool GetBool(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterValuesSource, "internal static string? GetString(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterValuesSource, "internal static int GetInt(JsonElement element, string propertyName, int fallback = 0)");
        AssertContains(sharedFormatterValuesSource, "internal static double GetDouble(JsonElement element, string propertyName, double fallback = 0.0)");
        AssertContains(sharedFormatterValuesSource, "internal static long GetLong(JsonElement element, string propertyName, long fallback = 0)");
        AssertContains(sharedFormatterValuesSource, "internal static long? GetNullableLong(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterValuesSource, "CultureInfo.InvariantCulture");
        AssertContains(sharedFormatterValuesSource, "internal static string FormatBytes(long bytes)");
        AssertContains(sharedFormatterValuesSource, "internal static long ComputeTickAgeMs(long tickMs)");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatBytes(long bytes)");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatIntervalMs(JsonElement element, string propertyName, string fallback = \"N/A\")");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatFrameBudgetMs(JsonElement element, string fpsPropertyName, string fallback = \"N/A\")");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatNumber(double value, string format)");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static long ComputeTickAgeMs(long tickMs)");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "var flashbackActive = Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackEncodingSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackPlaybackStatusSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackExportSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackPlaybackMetricsSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackEncodingStatusSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackEncodingHealthSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackEncodingStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Encoder: {codec}");
        AssertContains(sharedFormatterFlashbackSource, "Temp Cache:");
        AssertContains(sharedFormatterFlashbackSource, "Cleanup:");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackEncodingHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Flashback Queue Latency:");
        AssertContains(sharedFormatterFlashbackSource, "Flashback Backpressure:");
        AssertContains(sharedFormatterFlashbackSource, "Flashback Failure:");
        AssertContains(sharedFormatterFlashbackSource, "Flashback GPU Queue:");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackExportSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Export: active=");
        AssertContains(sharedFormatterFlashbackSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sharedFormatterFlashbackSource, "forceRotateFallbacks=");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Playback Commands:");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackPlaybackMetricsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Playback Decode:");
        AssertContains(sharedFormatterFlashbackSource, "A/V Drift:");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterRootSource, "WasapiPlaybackQueueDurationMs");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterThreadHealthSource, "AppendSourceReaderThreadHealthLine(builder, snapshot);");
        AssertContains(sharedFormatterThreadHealthSource, "AppendWasapiCaptureThreadHealthLine(builder, snapshot);");
        AssertContains(sharedFormatterThreadHealthSource, "AppendWasapiPlaybackThreadHealthLine(builder, snapshot);");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "SourceReaderFrameChannelDepth");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "WasapiCaptureCallbackSevereGapCount");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "WasapiPlaybackQueueDurationMs");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegTimingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterMjpegTimingSource, "var mjpegDecodeSamples = Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegDecodeTimingLines(builder, snapshot, mjpegDecodeSamples);");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegPipelineTimingLines(builder, snapshot, mjpegDecoderCount);");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegPreviewJitterSection(builder, snapshot);");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegPerDecoderTimingLines(builder, snapshot);");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegDecodeTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecodeSamples)");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegPerDecoderTimingLines(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterMjpegTimingSource, "Decode: avg=");
        AssertContains(sharedFormatterMjpegTimingSource, "Decoder[{Get(worker, \"WorkerIndex\", \"?\")}]");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegPipelineTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecoderCount)");
        AssertContains(sharedFormatterMjpegTimingSource, "Compressed Queue:");
        AssertContains(sharedFormatterMjpegTimingSource, "MJPEG Drop Reasons:");
        AssertContains(sharedFormatterMjpegTimingSource, "Pipeline: avg=");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterMjpegTimingSource, "Preview Jitter Latency:");
        AssertContains(sharedFormatterMjpegTimingSource, "Preview Jitter Underflow:");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendAvSyncSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendSourceSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "private static void AppendAvSyncSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureCadenceSource, "private static void AppendSourceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterAvSyncSource, "private static void AppendAvSyncSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterAvSyncSource, "var avSyncDrift = Get(snapshot, \"AvSyncCaptureDriftMs\", string.Empty);");
        AssertContains(sharedFormatterPreviewSource, "private static void AppendPreviewSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewSource, "AppendPreviewD3DSection(builder, snapshot);");
        AssertContains(sharedFormatterPreviewSource, "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(sharedFormatterPreviewSource, "D3D CPU timing:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "private static bool IsPreviewD3DRendererMode(string rendererMode)");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DCpuTiming(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DPipelineLatency(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameLatencyWait(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameStats(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameOwnership(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DCpuTiming(builder, snapshot);", "AppendPreviewD3DPipelineLatency(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DPipelineLatency(builder, snapshot);", "AppendPreviewD3DFrameLatencyWait(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameLatencyWait(builder, snapshot);", "AppendPreviewD3DFrameStats(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameStats(builder, snapshot);", "AppendPreviewD3DFrameOwnership(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameOwnership(builder, snapshot);", "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D CPU timing:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D pipeline latency:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D frame-latency wait:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D DXGI stats:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D Ownership:");
        AssertContains(sharedFormatterPreviewD3DSource, "internal static void AppendPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "private static string FormatDiagnosticMs(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D Slow Frames:");
        AssertContains(sharedFormatterSourceSource, "private static void AppendSourceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterSourceSource, "var sourceFrameRate = Get(snapshot, \"DetectedSourceFrameRate\", string.Empty);");
        AssertContains(sharedFormatterSource, "CaptureCommandOldestPendingCommandAgeMs");
        AssertContains(sharedFormatterSource, "CaptureCommandMaxQueueLatencyMs");
        AssertContains(sharedFormatterSource, "CaptureCommandCommandsCoalesced");
        AssertContains(sharedFormatterSource, "CaptureCommandLastOutcome");
        AssertContains(sharedFormatterSource, "CaptureCommandLastCorrelationId");
        AssertContains(sharedFormatterSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(sharedFormatterSource, "PreviewD3DTotalFrameCpuMaxMs");
        AssertContains(sharedFormatterSource, "ProcessCpuPercent");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "AutomationSnapshotFormatter.VideoPipeline.cs")),
            "shared snapshot video-pipeline and thread-health text lives with the root snapshot formatter flow");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "AutomationSnapshotFormatter.Values.cs")),
            "shared snapshot value accessors live with the root snapshot formatter flow");
        foreach (var removedFile in new[]
        {
            "AutomationSnapshotFormatter.Flashback.cs",
            "AutomationSnapshotFormatter.MjpegTiming.cs",
            "AutomationSnapshotFormatter.PreviewD3D.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", removedFile)),
                $"{removedFile} folded into root snapshot formatter");
        }

        return Task.CompletedTask;
    }

    internal static Task PresentMonProbe_SourceOwnership_IsUnified()
    {
        static string ReadPresentMonProbeFile(string fileName)
            => ReadRepoFile($"tools/Common/PresentMon/{fileName}").Replace("\r\n", "\n");

        var rootText = ReadPresentMonProbeFile("PresentMonProbe.cs");
        var formatText = rootText;
        var csvText = rootText;

        AssertContains(rootText, "public static class PresentMonProbe");
        AssertDoesNotContain(rootText, "partial class PresentMonProbe");
        AssertContains(rootText, "public static async Task<PresentMonProbeResult> RunAsync(");
        AssertContains(rootText, "var targetProcess = ResolveTargetProcess(options);");
        AssertContains(rootText, "var presentMonPath = ResolvePresentMonPath(options.PresentMonPath);");
        AssertContains(rootText, "var outputPath = ResolveOutputPath(options.OutputFile);");
        AssertContains(rootText, "var arguments = BuildArguments(");
        AssertContains(rootText, "private static string BuildArguments(");
        AssertContains(rootText, "private static string QuoteArgument(");
        AssertContains(rootText, "private static string BuildResultMessage(");
        AssertContains(rootText, "Captured {summary.RawSampleCount} PresentMon frame rows");
        AssertContains(rootText, "expected swap chain {summary.ExpectedSwapChainAddress} was not present");
        AssertContains(rootText, "PresentMon capture did not produce frame rows.");
        AssertContains(rootText, "var run = await RunProcessAsync(");
        AssertContains(rootText, "summary = ParseCsv(outputPath, options.ExpectedSwapChainAddress, options, captureStartUtcUnixMs);");
        AssertContains(rootText, "TryDelete(outputPath);");

        AssertContains(rootText, "public readonly record struct PresentMonProbeCorrelation(");
        AssertContains(rootText, "public static PresentMonProbeOptions CreateOptions(");
        AssertContains(rootText, "ExpectedSwapChainAddress = string.IsNullOrWhiteSpace(swapChainAddress)");
        AssertContains(rootText, "AppPresentId = appPresentId ?? correlation.PresentId");
        AssertContains(rootText, "public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)");
        AssertContains(rootText, "PreviewD3DSwapChainAddress");
        AssertContains(rootText, "PreviewD3DLastRenderedPreviewPresentId");
        AssertContains(rootText, "PreviewD3DLastRenderedSourceSequenceNumber");
        AssertContains(rootText, "PreviewD3DLastRenderedUtcUnixMs");
        AssertContains(rootText, "private static long? GetPositiveLong(");
        AssertContains(rootText, "private static long? GetNonNegativeLong(");

        AssertContains(rootText, "public sealed class PresentMonProbeOptions");
        AssertContains(rootText, "public sealed class PresentMonProbeResult");
        AssertContains(rootText, "public sealed class PresentMonCaptureSummary");
        AssertContains(rootText, "public sealed class PresentMonAppCorrelation");
        AssertContains(rootText, "public sealed class PresentMonSwapChainSummary");
        AssertContains(rootText, "public sealed class PresentMonMetricSummary");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Models.cs")),
            "PresentMon public DTOs live with PresentMonProbe.RunAsync and result formatting");

        AssertContains(formatText, "public static string Format(PresentMonProbeResult result)");
        AssertContains(formatText, "private static void AppendSummaryContext(");
        AssertContains(formatText, "private static void AppendMetric(");
        AssertContains(formatText, "private static void AppendAppCorrelation(");
        AssertContains(formatText, "private static void AppendSwapChains(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Format.cs")),
            "PresentMon result formatting lives with PresentMonProbe.RunAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.cs")),
            "PresentMon CSV parsing and aggregation live with PresentMonProbe.RunAsync");

        AssertContains(csvText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertContains(csvText, "var csvRows = ReadCsvRows(path);");
        AssertContains(csvText, "var rows = csvRows.Rows;");
        AssertContains(csvText, "var selectedRows = selectedSwapChain == null");
        AssertContains(csvText, "var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);");
        AssertContains(csvText, "var warnings = BuildWarnings(");
        AssertContains(csvText, "var appCorrelation = BuildAppCorrelation(");
        AssertContains(csvText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertContains(csvText, "private static string? NormalizeSwapChainAddress(");
        AssertContains(csvText, "private static string NormalizeHeader(");
        AssertContains(csvText, "private static double? ReadMetric(");
        AssertContains(csvText, "private static List<string> SplitCsvLine(");
        AssertContains(csvText, "private static PresentMonCsvRows ReadCsvRows(string path)");
        AssertContains(csvText, "private sealed record PresentMonCsvRows(");
        AssertContains(csvText, "private sealed record PresentMonRow(");
        AssertContains(csvText, "private static IReadOnlyDictionary<string, int> BuildCsvHeaderIndex(");
        AssertContains(csvText, "private static PresentMonRow ReadRow(");
        AssertContains(csvText, "rows.Add(ReadRow(rowIndex++, fields, index));");
        AssertContains(csvText, "private static bool HasAnyColumn(");
        AssertContains(csvText, "private static PresentMonAppCorrelation BuildAppCorrelation(");
        AssertContains(csvText, "private static string ClassifyPresentOutcome(");
        AssertContains(csvText, "private static IReadOnlyList<string> BuildWarnings(");
        AssertContains(csvText, "private static PresentMonMetricSummary Summarize(");
        AssertContains(csvText, "private static double Percentile(");

        AssertContains(rootText, "private static Process? ResolveTargetProcess(");
        AssertContains(rootText, "private static string? ResolvePresentMonPath(");
        AssertContains(rootText, "private static string ResolveOutputPath(");
        AssertContains(rootText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertContains(rootText, "private static async Task<string> TryReadAsync(");
        AssertContains(rootText, "private static void TryKill(");
        AssertContains(rootText, "private static void TryDelete(");
        AssertContains(rootText, "private sealed class ProcessRun");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Paths.cs")),
            "PresentMon path resolution lives with PresentMonProbe.RunAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Process.cs")),
            "PresentMon process supervision lives with PresentMonProbe.RunAsync");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Rows.cs")),
            "PresentMon CSV row ingestion lives with PresentMonProbe.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Correlation.cs")),
            "PresentMon CSV app correlation lives with PresentMonProbe.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Summary.cs")),
            "PresentMon CSV warnings and percentile summaries live with PresentMonProbe.cs");

        return Task.CompletedTask;
    }

    internal static Task PresentMonParser_SelectsDominantNonArtifactSwapChain()
    {
        var toolAssembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var probeType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbe")
            ?? throw new InvalidOperationException("Sussudio.Tools.PresentMonProbe type not found.");
        var parseCsv = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string) not found.");
        var parseCsvWithExpectedSwapChain = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string,string) not found.");
        var optionsType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbeOptions")
            ?? throw new InvalidOperationException("PresentMonProbeOptions type not found.");
        var parseCsvWithCorrelation = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string), optionsType, typeof(long?) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv correlation overload not found.");

        var csvPath = Path.Combine(Path.GetTempPath(), $"presentmon_parser_{Guid.NewGuid():N}.csv");
        File.WriteAllText(
            csvPath,
            """
            Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,0.0000,8.3333,8.3333,NA,16.0000,0.0700,8.2500,2.0000,7.0000,NA
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,8.3333,8.3334,8.3334,NA,16.1000,0.0710,8.2600,2.1000,7.1000,NA
            Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
            """);

        try
        {
            var summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null.");

            AssertEqual(2, GetIntProperty(summary, "SampleCount"), "selected PresentMon sample count");
            AssertEqual(3, GetIntProperty(summary, "RawSampleCount"), "raw PresentMon sample count");
            AssertEqual(1, GetIntProperty(summary, "ExcludedSampleCount"), "excluded PresentMon sample count");
            AssertEqual("0xABC", GetStringProperty(summary, "SelectedSwapChainAddress"), "selected PresentMon swap chain");

            var betweenPresents = GetPropertyValue(summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("BetweenPresentsMs was null.");
            AssertNearlyEqual(8.33335, GetDoubleProperty(betweenPresents, "Average"), 0.0001, "selected PresentMon average");
            AssertNearlyEqual(8.3334, GetDoubleProperty(betweenPresents, "Max"), 0.0001, "selected PresentMon max");

            var swapChains = GetPropertyValue(summary, "SwapChains")
                ?? throw new InvalidOperationException("SwapChains was null.");
            AssertEqual(2, GetCountProperty(swapChains), "PresentMon swap chain summary count");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0xAAA,DXGI,0,0,0,Composed: Flip,0.0000,99.0000,99.0000,8.3333,16.0000,0.0700,8.2500,2.0000,7.0000,20.0000
                Sussudio.exe,1234,0x0000000000000BBB,DXGI,0,0,0,Composed: Flip,8.3333,8.3333,8.3333,8.3333,16.1000,0.0710,8.2600,2.1000,7.1000,20.1000
                """);

            var expectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xbbb" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for expected swap-chain CSV.");
            AssertEqual("0xBBB", GetStringProperty(expectedSwapChainSummary, "SelectedSwapChainAddress"), "expected PresentMon selected swap chain");
            AssertEqual(true, GetBoolProperty(expectedSwapChainSummary, "ExpectedSwapChainMatched"), "expected PresentMon swap chain matched");
            var expectedBetweenPresents = GetPropertyValue(expectedSwapChainSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("expected BetweenPresentsMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(expectedBetweenPresents, "Average"), 0.0001, "expected swap-chain PresentMon average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,GPUTime,DisplayedTime,MsUntilDisplayed,DisplayLatency
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,90.0000,8.3333,8.2000,6.0000,8.3333,6.0000,12.0000
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,104.0000,8.3333,8.2000,6.0000,NA,20.0000,18.0000
                """);
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("Failed to create PresentMonProbeOptions.");
            SetPropertyOrBackingField(options, "AppPresentId", 42L);
            SetPropertyOrBackingField(options, "AppSourceSequenceNumber", 1001L);
            SetPropertyOrBackingField(options, "AppPresentUtcUnixMs", 1105L);
            SetPropertyOrBackingField(options, "CaptureStartUtcUnixMs", 1000L);
            var correlatedSummary = parseCsvWithCorrelation.Invoke(null, new object?[] { csvPath, "0xBBB", options, 1000L })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for correlated CSV.");
            var appCorrelation = GetPropertyValue(correlatedSummary, "AppCorrelation")
                ?? throw new InvalidOperationException("AppCorrelation was null.");
            AssertEqual(true, GetBoolProperty(appCorrelation, "Available"), "PresentMon app correlation available");
            AssertEqual(42L, GetLongProperty(appCorrelation, "AppPresentId"), "PresentMon app present id");
            AssertEqual(1001L, GetLongProperty(appCorrelation, "AppSourceSequenceNumber"), "PresentMon app source sequence");
            AssertEqual(1, GetIntProperty(appCorrelation, "PresentMonRowIndex"), "PresentMon correlated row index");
            AssertNearlyEqual(1.0, GetDoubleProperty(appCorrelation, "DeltaMs"), 0.0001, "PresentMon app correlation delta");
            AssertEqual("SupersededOrNotDisplayed", GetStringProperty(appCorrelation, "Outcome"), "PresentMon app correlation outcome");

            var missingExpectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xCCC" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for missing expected swap-chain CSV.");
            AssertEqual(0, GetIntProperty(missingExpectedSwapChainSummary, "SampleCount"), "missing expected PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "RawSampleCount"), "missing expected raw PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "ExcludedSampleCount"), "missing expected excluded PresentMon sample count");
            AssertEqual("0xCCC", GetStringProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainAddress"), "missing expected PresentMon swap chain");
            AssertEqual(false, GetBoolProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainMatched"), "missing expected PresentMon swap chain matched");
            AssertEqual(string.Empty, GetStringProperty(missingExpectedSwapChainSummary, "SelectedSwapChainAddress"), "missing expected selected PresentMon swap chain");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,CPUWait,GPULatency,GPUTime,GPUBusy,GPUWait,VideoBusy,DisplayLatency,DisplayedTime
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,0.0000,9.0000,8.9000,0.1000,3.0000,6.0000,2.0000,4.0000,7.0000,22.0000,8.3333
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,9.0000,7.6666,7.5000,0.1666,3.0000,6.5000,2.5000,4.0000,7.0000,22.5000,8.3334
                """);

            var v2Summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for v2 CSV.");
            var v2BetweenPresents = GetPropertyValue(v2Summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("v2 BetweenPresentsMs was null.");
            var v2CpuBusy = GetPropertyValue(v2Summary, "CpuBusyMs")
                ?? throw new InvalidOperationException("v2 CpuBusyMs was null.");
            var v2GpuBusy = GetPropertyValue(v2Summary, "GpuBusyMs")
                ?? throw new InvalidOperationException("v2 GpuBusyMs was null.");
            var v2GpuTime = GetPropertyValue(v2Summary, "GpuTimeMs")
                ?? throw new InvalidOperationException("v2 GpuTimeMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(v2BetweenPresents, "Average"), 0.0001, "v2 PresentMon frame time average");
            AssertNearlyEqual(8.2, GetDoubleProperty(v2CpuBusy, "Average"), 0.0001, "v2 PresentMon CPU busy average");
            AssertNearlyEqual(2.25, GetDoubleProperty(v2GpuBusy, "Average"), 0.0001, "v2 PresentMon GPU busy average");
            AssertNearlyEqual(6.25, GetDoubleProperty(v2GpuTime, "Average"), 0.0001, "v2 PresentMon GPU time average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
                """);

            var artifactOnlySummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for artifact-only CSV.");
            AssertEqual(0, GetIntProperty(artifactOnlySummary, "SampleCount"), "artifact-only selected sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "RawSampleCount"), "artifact-only raw sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "ExcludedSampleCount"), "artifact-only excluded sample count");
            AssertEqual(string.Empty, GetStringProperty(artifactOnlySummary, "SelectedSwapChainAddress"), "artifact-only selected swap chain");

            File.WriteAllText(csvPath, "   \r\n");
            var emptyHeaderSummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for empty-header CSV.");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "SampleCount"), "empty-header selected sample count");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "RawSampleCount"), "empty-header raw sample count");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "ExcludedSampleCount"), "empty-header excluded sample count");
            AssertEqual(false, GetBoolProperty(emptyHeaderSummary, "DisplayedTimeColumnPresent"), "empty-header displayed-time column presence");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,AllowsTearing,PresentMode,MsBetweenPresents,MsBetweenPresents,DisplayedTime,MsBetweenDisplayChange
                Sussudio.exe,1234,0xDAD,DXGI,0,0,Composed: Flip,7.0000,99.0000,7.0000,7.0000
                """);
            var duplicateHeaderSummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for duplicate-header CSV.");
            var duplicateHeaderBetweenPresents = GetPropertyValue(duplicateHeaderSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("duplicate-header BetweenPresentsMs was null.");
            AssertEqual(1, GetIntProperty(duplicateHeaderSummary, "RawSampleCount"), "duplicate-header raw sample count");
            AssertEqual("0xDAD", GetStringProperty(duplicateHeaderSummary, "SelectedSwapChainAddress"), "duplicate-header selected swap chain");
            AssertNearlyEqual(7.0, GetDoubleProperty(duplicateHeaderBetweenPresents, "Average"), 0.0001, "duplicate header uses first metric occurrence");
        }
        finally
        {
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }

        return Task.CompletedTask;
    }

    internal static async Task SsctlPipeTransport_ExposesAdvancedAutomationCommandIds()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);

        // Verify PipeTransport exposes expected command routing.
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var sendCommandAsync = transportType.GetMethod(
                "SendCommandAsync",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(int?) },
                modifiers: null)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport.SendCommandAsync not found.");

        var pipeName = $"ssctl-pipe-transport-{Guid.NewGuid():N}";
        var transport = Activator.CreateInstance(transportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for transport test.");
        var request = await CapturePipeRequestAsync(
            pipeName,
            async () =>
            {
                var task = sendCommandAsync.Invoke(
                    transport,
                    new object?[]
                    {
                        "SetPreviewVolume",
                        new Dictionary<string, object?> { ["previewVolumePercent"] = 55.5 },
                        null
                    }) as Task
                    ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(34, request.GetProperty("command").GetInt32(), "PipeTransport SetPreviewVolume command id");
        AssertEqual(55.5, request.GetProperty("payload").GetProperty("previewVolumePercent").GetDouble(), "PipeTransport preview volume payload");

        JsonElement response = default;
        var responsePipeName = $"ssctl-pipe-response-{Guid.NewGuid():N}";
        var responseTransport = Activator.CreateInstance(transportType, responsePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for response test.");
        var responseRequests = await CapturePipeRequestsAsync(
                responsePipeName,
                expectedCount: 1,
                async () =>
                {
                    response = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            responseTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                _ => """
                     {
                       "Success": true,
                       "Message": "snapshot ready",
                       "Data": {
                         "value": 123
                       }
                     }
                     """)
            .ConfigureAwait(false);
        AssertEqual(1, responseRequests[0].GetProperty("command").GetInt32(), "PipeTransport GetSnapshot command id");
        AssertEqual("snapshot ready", response.GetProperty("Message").GetString(), "PipeTransport parsed response message");
        AssertEqual(123, response.GetProperty("Data").GetProperty("value").GetInt32(), "PipeTransport parsed response data");

        JsonElement retryResponse = default;
        var retryPipeName = $"ssctl-pipe-retry-{Guid.NewGuid():N}";
        var retryTransport = Activator.CreateInstance(transportType, retryPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for retry test.");
        var retryRequests = await CapturePipeRequestsAsync(
                retryPipeName,
                expectedCount: 2,
                async () =>
                {
                    retryResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            retryTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": false,
                        "Status": "not_ready",
                        "RetryAfterMs": 100,
                        "Message": "snapshot not ready"
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "snapshot ready after retry",
                        "Data": {
                          "attempt": 2
                        }
                      }
                      """)
            .ConfigureAwait(false);
        AssertEqual(1, retryRequests[0].GetProperty("command").GetInt32(), "PipeTransport retry first command id");
        AssertEqual(1, retryRequests[1].GetProperty("command").GetInt32(), "PipeTransport retry second command id");
        AssertEqual("snapshot ready after retry", retryResponse.GetProperty("Message").GetString(), "PipeTransport retry final message");
        AssertEqual(2, retryResponse.GetProperty("Data").GetProperty("attempt").GetInt32(), "PipeTransport retry final data");

        JsonElement invalidJsonResponse = default;
        var invalidPipeName = $"ssctl-pipe-invalid-{Guid.NewGuid():N}";
        var invalidTransport = Activator.CreateInstance(transportType, invalidPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for invalid JSON test.");
        var invalidRequest = await CapturePipeRequestWithRawResponseAsync(
                invalidPipeName,
                async () =>
                {
                    invalidJsonResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            invalidTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                "not-json")
            .ConfigureAwait(false);
        AssertEqual(1, invalidRequest.GetProperty("command").GetInt32(), "PipeTransport invalid JSON request command id");
        AssertEqual(false, invalidJsonResponse.GetProperty("Success").GetBoolean(), "PipeTransport invalid JSON response Success=false");
        AssertEqual("pipe-invalid-json", invalidJsonResponse.GetProperty("ErrorCode").GetString(), "PipeTransport invalid JSON response ErrorCode");
        var invalidJsonMessage = invalidJsonResponse.GetProperty("Message").GetString() ?? "";
        AssertEqual(
            true,
            invalidJsonMessage.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase) || invalidJsonMessage.Contains("pipe-invalid-json", StringComparison.OrdinalIgnoreCase),
            $"PipeTransport invalid JSON response Message should mention invalid JSON, got: {invalidJsonMessage}");

        var usageTransport = Activator.CreateInstance(transportType, $"ssctl-pipe-usage-{Guid.NewGuid():N}", (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for usage test.");
        Exception? usageException = null;
        try
        {
            await InvokePipeTransportSendCommandAsync(
                    sendCommandAsync,
                    usageTransport,
                    "DefinitelyNotACommand",
                    null,
                    null)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            usageException = ex;
        }

        AssertEqual("Sussudio.Tools.Ssctl.UsageException", usageException?.GetType().FullName, "PipeTransport unknown command exception type");
    }

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

    private static async Task<JsonElement> InvokePipeTransportSendCommandAsync(
        MethodInfo sendCommandAsync,
        object transport,
        string commandName,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
    {
        var task = sendCommandAsync.Invoke(
                transport,
                new object?[]
                {
                    commandName,
                    payload,
                    responseTimeoutMs
                }) as Task<JsonElement>
            ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return Task<JsonElement>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<JsonElement> CapturePipeRequestWithRawResponseAsync(
        string pipeName,
        Func<Task> clientAction,
        string rawResponseLine)
    {
        var clientTask = Task.Run(clientAction);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        string requestLine;
        {
            using var serverPipe = new System.IO.Pipes.NamedPipeServerStream(
                pipeName,
                System.IO.Pipes.PipeDirection.InOut,
                1,
                System.IO.Pipes.PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);

            var connectTask = serverPipe.WaitForConnectionAsync(cts.Token);
            if (await Task.WhenAny(connectTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe request, but the client completed before connecting.");
            }

            await connectTask.ConfigureAwait(false);
            using var reader = new StreamReader(serverPipe, leaveOpen: true);
            var readTask = reader.ReadLineAsync().WaitAsync(cts.Token);
            if (await Task.WhenAny(readTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe payload, but the client completed before sending one.");
            }

            try
            {
                requestLine = await readTask.ConfigureAwait(false)
                    ?? throw new InvalidOperationException("No request received on raw-response pipe.");
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("Timed out waiting for raw-response pipe payload.", ex);
            }

            using var writer = new StreamWriter(serverPipe, leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(rawResponseLine)
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
        }

        await EnsureNoUnexpectedPipeRequestAsync(pipeName, 1, 1, clientTask, cts.Token)
            .ConfigureAwait(false);

        using var document = JsonDocument.Parse(requestLine);
        return document.RootElement.Clone();
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
        var pipeClientText = sharedClientText;
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "PipeTransport.cs")),
            "ssctl PipeTransport should stay with the command-handler surface instead of returning as a tiny adapter file.");
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "Common", "AutomationPipeClient", "AutomationPipeClient.cs")),
            "AutomationPipeClient transport is folded into Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionCommandChannelText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
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
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "Common", "DiagnosticSessionCommandChannel.cs")),
            "diagnostic-session command channel should stay with the run-context infrastructure owner.");
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
