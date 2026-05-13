using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Contract tests for automation, ssctl, and MCP command surfaces.
static partial class Program
{
    private static Task AutomationPipeProtocol_ResolvesCommandsTimeoutsAuthAndEnvelopes()
    {
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        AssertEqual("SussudioAutomation", GetConstant<string>(protocolType, "DefaultPipeName"), "AutomationPipeProtocol.DefaultPipeName");
        AssertEqual("SUSSUDIO_AUTOMATION_TOKEN", GetConstant<string>(protocolType, "AutomationKeyEnvVar"), "AutomationPipeProtocol.AutomationKeyEnvVar");
        AssertEqual(5000, GetConstant<int>(protocolType, "DefaultConnectTimeoutMs"), "AutomationPipeProtocol.DefaultConnectTimeoutMs");
        AssertEqual(15000, GetConstant<int>(protocolType, "DefaultResponseTimeoutMs"), "AutomationPipeProtocol.DefaultResponseTimeoutMs");
        AssertEqual(60000, GetConstant<int>(protocolType, "ExtendedResponseTimeoutMs"), "AutomationPipeProtocol.ExtendedResponseTimeoutMs");
        AssertEqual(150000, GetConstant<int>(protocolType, "RecordingResponseTimeoutMs"), "AutomationPipeProtocol.RecordingResponseTimeoutMs");
        AssertEqual(305000, GetConstant<int>(protocolType, "FlashbackMutationResponseTimeoutMs"), "AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs");

        var resolveCommand = RequireNonPublicStaticMethod(protocolType, "ResolveCommand");
        AssertEqual(1, (int)resolveCommand.Invoke(null, new object[] { "GetSnapshot" })!, "ResolveCommand exact");
        AssertEqual(1, (int)resolveCommand.Invoke(null, new object[] { "get-snapshot" })!, "ResolveCommand normalized");
        AssertEqual(17, (int)resolveCommand.Invoke(null, new object[] { "17" })!, "ResolveCommand numeric");
        AssertThrows<ArgumentException>(
            () => resolveCommand.Invoke(null, new object[] { "not-a-command" }),
            "ResolveCommand unknown command");

        var tryGetCommandValue = RequireNonPublicStaticMethod(protocolType, "TryGetCommandValue");
        var valueArgs = new object?[] { "setrecordingenabled", null };
        AssertEqual(true, (bool)tryGetCommandValue.Invoke(null, valueArgs)!, "TryGetCommandValue case-insensitive");
        AssertEqual(17, (int)valueArgs[1]!, "TryGetCommandValue SetRecordingEnabled");

        var tryGetCommandName = RequireNonPublicStaticMethod(protocolType, "TryGetCommandName");
        var nameArgs = new object?[] { 17, null };
        AssertEqual(true, (bool)tryGetCommandName.Invoke(null, nameArgs)!, "TryGetCommandName known");
        AssertEqual("SetRecordingEnabled", (string)nameArgs[1]!, "TryGetCommandName SetRecordingEnabled");
        var unknownNameArgs = new object?[] { -1, null };
        AssertEqual(false, (bool)tryGetCommandName.Invoke(null, unknownNameArgs)!, "TryGetCommandName unknown");
        AssertEqual(string.Empty, (string)unknownNameArgs[1]!, "TryGetCommandName unknown output");

        var previousToken = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN");
        try
        {
            var getAuth = RequireNonPublicStaticMethod(protocolType, "GetConfiguredAuthToken");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "env-token");
            AssertEqual("explicit-token", (string)getAuth.Invoke(null, new object?[] { "explicit-token" })!, "GetConfiguredAuthToken explicit");
            AssertEqual("env-token", (string)getAuth.Invoke(null, new object?[] { null })!, "GetConfiguredAuthToken env");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "   ");
            AssertEqual(null, getAuth.Invoke(null, new object?[] { null }), "GetConfiguredAuthToken whitespace env");

            var defaultTimeout = RequireNonPublicStaticMethod(protocolType, "GetDefaultResponseTimeout");
            AssertEqual(15000, (int)defaultTimeout.Invoke(null, new object[] { "GetSnapshot" })!, "GetDefaultResponseTimeout default command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "FlashbackExport" })!, "GetDefaultResponseTimeout flashback export command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "SetFlashbackEnabled" })!, "GetDefaultResponseTimeout flashback command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "RestartFlashback" })!, "GetDefaultResponseTimeout flashback restart command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "SetRecordingEnabled" })!, "GetDefaultResponseTimeout recording command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "set-recording-enabled" })!, "GetDefaultResponseTimeout normalized recording command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "17" })!, "GetDefaultResponseTimeout numeric recording command");

            var createEnvelope = RequireNonPublicStaticMethod(protocolType, "CreateRequestEnvelope");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "env-token");
            var payload = new Dictionary<string, object?> { ["enabled"] = true };
            var envelope = (IDictionary<string, object?>)createEnvelope.Invoke(null, new object?[] { 17, payload, null })!;
            AssertEqual(17, (int)envelope["command"]!, "CreateRequestEnvelope command");
            AssertEqual(32, ((string)envelope["correlationId"]!).Length, "CreateRequestEnvelope correlation id length");
            AssertEqual("env-token", (string)envelope["authToken"]!, "CreateRequestEnvelope env auth");
            AssertEqual(true, ReferenceEquals(payload, envelope["payload"]), "CreateRequestEnvelope payload identity");

            var explicitEnvelope = (IDictionary<string, object?>)createEnvelope.Invoke(null, new object?[] { 1, null, "explicit-token" })!;
            AssertEqual("explicit-token", (string)explicitEnvelope["authToken"]!, "CreateRequestEnvelope explicit auth");
            AssertEqual(true, explicitEnvelope["payload"] is Dictionary<string, object?>, "CreateRequestEnvelope default payload shape");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", previousToken);
        }

        return Task.CompletedTask;
    }

    private static Task AutomationCommandCatalog_CoversCommandsAndPolicyMetadata()
    {
        var catalogType = RequireType("Sussudio.Tools.AutomationCommandCatalog");
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var pathPolicyType = RequireType("Sussudio.Tools.AutomationCommandPathPolicy");
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

        return Task.CompletedTask;
    }

    private static Task AutomationPipeConnectFailures_AreClassifiedForCliAndMcp()
    {
        var sharedClientText = ReadRepoFile("tools/Common/AutomationPipeClient.cs")
            .Replace("\r\n", "\n");
        var ssctlPipeText = ReadRepoFile("tools/ssctl/PipeTransport.cs")
            .Replace("\r\n", "\n");
        var mcpPipeText = ReadRepoFile("tools/McpServer/PipeClient.cs")
            .Replace("\r\n", "\n");
        var diagnosticSessionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var diagnosticSessionPipeRetryText = ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(sharedClientText, "catch (UnauthorizedAccessException ex)");
        AssertContains(sharedClientText, "\"pipe-access-denied\"");
        AssertContains(sharedClientText, "AutomationPipeProtocol.AutomationKeyEnvVar");
        AssertContains(sharedClientText, "public string ErrorCode { get; }");

        AssertContains(ssctlPipeText, "catch (AutomationPipeConnectException ex)");
        AssertContains(ssctlPipeText, "CreateSyntheticError(ex.Message, ex.ErrorCode)");
        AssertDoesNotContain(ssctlPipeText, "Sussudio is not running or not responding. Start the app and try again.");

        AssertContains(mcpPipeText, "catch (AutomationPipeConnectException ex)");
        AssertContains(mcpPipeText, "CreateSyntheticError(ex.Message, ex.ErrorCode)");
        AssertDoesNotContain(mcpPipeText, "Sussudio is not running or not responding. Start the app and try again.");

        AssertContains(diagnosticSessionText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(diagnosticSessionPipeRetryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(diagnosticSessionPipeRetryText, "internal static async Task<JsonElement?> SendCommandWithConnectRetryAsync(");
        AssertContains(diagnosticSessionPipeRetryText, "\"pipe-connect-failed\"");
        AssertContains(diagnosticSessionPipeRetryText, "\"pipe-connect-timeout\"");
        AssertContains(diagnosticSessionPipeRetryText, "IsPermanentPipeConnectFailure(ex.ErrorCode)");
        AssertContains(diagnosticSessionPipeRetryText, "\"pipe-access-denied\"");
        AssertDoesNotContain(diagnosticSessionText, "private static async Task<JsonElement?> SendCommandWithConnectRetryAsync(");

        return Task.CompletedTask;
    }

    private static Task ReliabilityGates_RunToolsAndOfflineHarness()
    {
        var scriptText = ReadRepoFile("tools/reliability-gates.ps1")
            .Replace("\r\n", "\n");
        var diagnosticSessionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var diagnosticSessionCleanupActionsText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.cs")
            .Replace("\r\n", "\n");

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

        AssertContains(diagnosticSessionCleanupActionsText, "var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(\"SetFlashbackEnabled\");");
        AssertContains(diagnosticSessionCleanupActionsText, "CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs))");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = true }");
        return Task.CompletedTask;
    }

    private static Task AutomationManifest_CoversCatalogMetadata()
    {
        var catalogType = RequireType("Sussudio.Tools.AutomationCommandCatalog");
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

    private static Task AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage()
    {
        var catalogType = RequireType("Sussudio.Tools.AutomationCommandCatalog");
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
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

        var dispatcherText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.cs")
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
            AssertContains(
                dispatcherText,
                $"ValidatePathPayload(\n                        AutomationCommandKind.{commandName},\n                        \"{expected.FieldName}\"");

            var enumValue = Enum.Parse(enumType, commandName);
            AssertThrows<InvalidOperationException>(
                () => validatePath.Invoke(null, new[] { enumValue, expected.FieldName, string.Empty }),
                $"{commandName} empty path validation");
        }

        return Task.CompletedTask;
    }

    private static Task AutomationManifest_SerializationIsStable()
    {
        const string ExpectedManifestSha256 = "B6A413C3173E67562BF2030574EC34E2F92942AB13B94B9681F64F373E55BA48";
        var catalogType = RequireType("Sussudio.Tools.AutomationCommandCatalog");
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

    private static Task AutomationResponseState_ParsesStatusAndRetryContracts()
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

        return Task.CompletedTask;
    }

    private static Task AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors()
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
                "SelectedResolution": "3840x2160",
                "SelectedFriendlyFrameRate": "59.94",
                "SelectedExactFrameRate": "59.940",
                "SelectedExactFrameRateArg": "60000/1001",
                "SelectedRecordingFormat": "HevcMp4",
                "SelectedQuality": "High",
                "SelectedPreset": "P5",
                "FlashbackActive": true,
                "EncoderCodecName": "hevc_nvenc",
                "EncoderFrameRate": 120,
                "EncoderFrameRateNumerator": 120,
                "EncoderFrameRateDenominator": 1,
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
                "LastExportId": 7,
                "MjpegDecodeSampleCount": 1,
                "MjpegDecoderCount": 1,
                "MjpegPerDecoder": [
                  { "WorkerIndex": 0, "AvgMs": 2.1, "P95Ms": 3.1, "MaxMs": 4.1, "SampleCount": 5 }
                ],
                "PreviewRendererMode": "D3D11VideoProcessor",
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
        var formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, true })!;
        AssertContains(formatted, "== Sussudio State ==");
        AssertContains(formatted, "Device: Synthetic (dev-1)");
        AssertContains(formatted, "Frame Rate: 59.94 fps (59.940 fps, 60000/1001)");
        AssertContains(formatted, "== Flashback ==");
        AssertContains(formatted, "Encoder: hevc_nvenc 0x0 @ 120 fps (120/1)");
        AssertContains(formatted, "Written: 2 MB");
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
        AssertContains(formatted, "== MJPEG Pipeline Timing ==");
        AssertContains(formatted, "Decoder[0]: avg=2.1ms");
        AssertContains(formatted, "== Preview ==");
        AssertContains(formatted, "D3D CPU timing: input/upload avg=0.1ms P95=0.2ms P99=0.3ms max=0.4ms | render-submit avg=0.5ms P95=0.6ms P99=0.7ms max=0.8ms | present-call avg=0.9ms P95=1.0ms P99=1.1ms max=1.2ms | total-frame avg=1.3ms P95=1.4ms P99=1.5ms max=1.6ms samples=120");
        AssertContains(formatted, "D3D pipeline latency: avg=7.8ms P95=8.9ms P99=9.9ms max=12.3ms last=8.4ms samples=120");
        AssertContains(formatted, "D3D frame-latency wait: enabled=true handle=true calls=118 signaled=110 timeouts=8 unexpected=0 lastResult=0 last=0.05ms avg=0.2ms P95=0.8ms max=2.0ms samples=118");
        AssertContains(formatted, "D3D DXGI stats: ok=119/120 failures=1 recentFailures=1 missedRefresh=4 recentMissed=2 lastError=DXGI_ERROR_WAS_STILL_DRAWING");
        AssertContains(formatted, "D3D Slow Frames: present=42 srcSeq=9001 reason=present_interval target=8.33ms over=0.87ms interval=9.20ms");
        AssertContains(formatted, "presentCall=3.30ms sched=7.70ms pipeline=8.80ms");
        AssertContains(formatted, "== Source ==");

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
        AssertEqual(expectedRead, (bool)tryRead.Invoke(null, args)!, $"{fieldName} read result");
        AssertEqual(expectedSuccess, (bool)args[1]!, $"{fieldName} success");
        AssertEqual(expectedStatus, (string?)args[2], $"{fieldName} status");
        var actualRetryAfterMs = args[3] is null ? (int?)null : Convert.ToInt32(args[3]);
        AssertEqual(expectedRetryAfterMs, actualRetryAfterMs, $"{fieldName} retryAfterMs");
    }

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
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
}
