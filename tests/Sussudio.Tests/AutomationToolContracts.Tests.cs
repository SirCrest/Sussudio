using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Shared reflection helpers and contract checks for automation tool contract tests.
static partial class Program
{
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

    private static (string Name, int Value)[] ExpectedAutomationCommands()
        => AutomationCommandGoldenTable.ExpectedCommands();

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
