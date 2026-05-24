using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandCatalog_CoversCommandsAndPolicyMetadata()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var catalogEntriesText = ReadAutomationCommandCatalogEntriesSource();
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

        AssertContains(catalogText, "public static partial class AutomationCommandCatalog");
        AssertContains(catalogEntriesText, "private static IReadOnlyList<AutomationCommandMetadata> BuildEntries()");
        AssertContains(catalogEntriesText, "RegisterCaptureEntries(entries);");
        AssertContains(catalogEntriesText, "private static void RegisterCaptureEntries(");
        AssertContains(catalogEntriesText, "private static void RegisterFlashbackEntries(");
        AssertContains(catalogEntriesText, "Set(entries, AutomationCommandKind.SetRecordingEnabled");
        AssertContains(catalogEntriesText, "Set(entries, AutomationCommandKind.FlashbackExport");
        AssertDoesNotContain(catalogText, "private static IReadOnlyList<AutomationCommandMetadata> BuildEntries()");
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

    private static string ReadAutomationCommandCatalogEntriesSource()
        => string.Join(
            "\n",
            new[]
            {
                "AutomationCommandCatalog.Entries.cs"
            }.Select(file => ReadRepoFile($"Sussudio.Automation.Contracts/{file}").Replace("\r\n", "\n")));

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

}
