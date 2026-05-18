using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationProtocol_SetRecordingUsesRecordingSizedTimeout()
    {
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n");
        var clientText = ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n");

        AssertContains(protocolText, "public const int DefaultResponseTimeoutMs = 15000;");
        AssertContains(protocolText, "public const int ExtendedResponseTimeoutMs = 60000;");
        AssertContains(protocolText, "public const int RecordingResponseTimeoutMs = 150000;");
        AssertContains(protocolText, "public const int FlashbackMutationResponseTimeoutMs = 305000;");
        AssertContains(protocolText, "commandName = ResolveCanonicalCommandName(commandName);");
        AssertContains(protocolText, "AutomationCommandCatalog.TryGet(commandName, out var metadata)");
        AssertContains(protocolText, "? metadata.ResponseTimeoutMs");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        AssertContains(catalogText, "AutomationCommandKind.SetRecordingEnabled");
        AssertContains(catalogText, "AutomationPipeProtocol.RecordingResponseTimeoutMs");
        AssertContains(catalogText, "AutomationCommandKind.FlashbackExport");
        AssertContains(catalogText, "AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs");
        AssertDoesNotContain(protocolText, "AlignResponseTimeoutWithServerRequest");
        AssertContains(clientText, "AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)");
        AssertContains(clientText, "AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName)");
        AssertContains(clientText, "public int? ResponseTimeoutMs { get; set; }");
        var pipeClientText = global::Sussudio.Tests.RuntimeContractSource.ReadAutomationPipeClientSource();
        AssertDoesNotContain(pipeClientText, "AlignResponseTimeoutWithServerRequest");

        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var getDefaultResponseTimeout = protocolType.GetMethod(
            "GetDefaultResponseTimeout",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout(string) not found.");

        foreach (var acceptedName in new[] { "SetRecordingEnabled", "setrecordingenabled", "set-recording-enabled", "17" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(150000, timeoutMs, $"SetRecordingEnabled timeout for '{acceptedName}'");
        }

        var defaultTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "GetSnapshot" })!;
        AssertEqual(15000, defaultTimeoutMs, "GetSnapshot timeout remains bounded");

        var flashbackExportTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "FlashbackExport" })!;
        AssertEqual(305000, flashbackExportTimeoutMs, "FlashbackExport uses flashback mutation timeout");

        foreach (var acceptedName in new[] { "SetFlashbackEnabled", "set-flashback-enabled", "RestartFlashback" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(305000, timeoutMs, $"Flashback mutation timeout for '{acceptedName}' outlives server cancellation");
        }

        return Task.CompletedTask;
    }

    private static Task AutomationPipeProtocol_ResolvesCommandsTimeoutsAuthAndEnvelopes()
    {
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        AssertEqual("SussudioAutomation", GetConstant<string>(protocolType, "DefaultPipeName"), "AutomationPipeProtocol.DefaultPipeName");
        AssertEqual("SUSSUDIO_AUTOMATION_TOKEN", GetConstant<string>(protocolType, "AutomationKeyEnvVar"), "AutomationPipeProtocol.AutomationKeyEnvVar");
        var commandManifestRevision = GetConstant<int>(protocolType, "CommandManifestRevision");
        AssertEqual(1, commandManifestRevision, "AutomationPipeProtocol.CommandManifestRevision");
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

            var defaultTimeout = protocolType.GetMethod(
                "GetDefaultResponseTimeout",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null)
                ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout(string) not found.");
            AssertEqual(15000, (int)defaultTimeout.Invoke(null, new object[] { "GetSnapshot" })!, "GetDefaultResponseTimeout default command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "FlashbackExport" })!, "GetDefaultResponseTimeout flashback export command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "SetFlashbackEnabled" })!, "GetDefaultResponseTimeout flashback command");
            AssertEqual(305000, (int)defaultTimeout.Invoke(null, new object[] { "RestartFlashback" })!, "GetDefaultResponseTimeout flashback restart command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "SetRecordingEnabled" })!, "GetDefaultResponseTimeout recording command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "set-recording-enabled" })!, "GetDefaultResponseTimeout normalized recording command");
            AssertEqual(150000, (int)defaultTimeout.Invoke(null, new object[] { "17" })!, "GetDefaultResponseTimeout numeric recording command");

            var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
            var typedDefaultTimeout = protocolType.GetMethod(
                "GetDefaultResponseTimeout",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { enumType },
                modifiers: null)
                ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind) not found.");
            var waitForCondition = Enum.Parse(enumType, "WaitForCondition");
            AssertEqual(60000, (int)typedDefaultTimeout.Invoke(null, new[] { waitForCondition })!, "GetDefaultResponseTimeout typed wait command");

            var createEnvelope = RequireNonPublicStaticMethod(protocolType, "CreateRequestEnvelope");
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", "env-token");
            var payload = new Dictionary<string, object?> { ["enabled"] = true };
            var envelope = (IDictionary<string, object?>)createEnvelope.Invoke(null, new object?[] { 17, payload, null })!;
            AssertEqual(17, (int)envelope["command"]!, "CreateRequestEnvelope command");
            AssertEqual(32, ((string)envelope["correlationId"]!).Length, "CreateRequestEnvelope correlation id length");
            AssertEqual(commandManifestRevision, (int)envelope["manifestRevision"]!, "CreateRequestEnvelope manifest revision");
            AssertEqual("env-token", (string)envelope["authToken"]!, "CreateRequestEnvelope env auth");
            AssertEqual(true, ReferenceEquals(payload, envelope["payload"]), "CreateRequestEnvelope payload identity");

            var explicitEnvelope = (IDictionary<string, object?>)createEnvelope.Invoke(null, new object?[] { 1, null, "explicit-token" })!;
            AssertEqual("explicit-token", (string)explicitEnvelope["authToken"]!, "CreateRequestEnvelope explicit auth");
            AssertEqual(commandManifestRevision, (int)explicitEnvelope["manifestRevision"]!, "CreateRequestEnvelope explicit manifest revision");
            AssertEqual(true, explicitEnvelope["payload"] is Dictionary<string, object?>, "CreateRequestEnvelope default payload shape");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_TOKEN", previousToken);
        }

        return Task.CompletedTask;
    }

    private static Task SharedProtocol_CommandMap_CoversEveryAutomationCommandKind()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var enumNames = Enum.GetNames(enumType);
        var expectedCommands = ExpectedAutomationCommands();
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");

        if (enumNames.Length == 0)
            throw new InvalidOperationException("AutomationCommandKind enum has no members.");

        var commandMapProperty = protocolType.GetProperty(
            "CommandMap",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.CommandMap not found.");
        var commandMap = commandMapProperty.GetValue(null) as IReadOnlyDictionary<string, int>
            ?? throw new InvalidOperationException("AutomationPipeProtocol.CommandMap has an unexpected shape.");

        AssertEqual(expectedCommands.Length, commandMap.Count,
            "AutomationPipeProtocol CommandMap entry count vs golden command table");
        foreach (var (name, ordinal) in expectedCommands)
        {
            if (!commandMap.TryGetValue(name, out var mappedOrdinal))
            {
                throw new InvalidOperationException($"AutomationPipeProtocol.CommandMap missing '{name}'.");
            }

            AssertEqual(ordinal, mappedOrdinal, $"AutomationPipeProtocol.CommandMap[{name}]");
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
        }

        AssertEqual(enumNames.Length, commandMap.Count,
            "AutomationPipeProtocol CommandMap entry count vs AutomationCommandKind enum count");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandMaps_StayAligned_ForAdvancedMcpControls()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var scriptText = ReadRepoFile("tools/send-automation-command.ps1");
        var resolveCommand = protocolType.GetMethod(
            "ResolveCommand",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.ResolveCommand not found.");

        foreach (var (name, ordinal) in new[]
        {
            ("GetCaptureOptions", 29),
            ("SetPreset", 30),
            ("SetSplitEncodeMode", 31),
            ("SetMjpegDecoderCount", 32),
            ("SetShowAllCaptureOptions", 33),
            ("SetPreviewVolume", 34),
            ("SetStatsVisible", 35)
        })
        {
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
            AssertEqual(ordinal, Convert.ToInt32(resolveCommand.Invoke(null, new object?[] { name })), $"AutomationPipeProtocol.ResolveCommand({name})");
        }

        AssertContains(protocolText, "Enum.GetValues<AutomationCommandKind>()");

        AssertContains(scriptText, "AutomationClient\\AutomationClient.csproj");
        AssertContains(scriptText, "Get-AutomationClientInputWriteTimeUtc");
        AssertContains(scriptText, "Test-AutomationClientBuildFresh");
        AssertContains(scriptText, "AutomationClient build failed with exit code $LASTEXITCODE.");
        AssertContains(scriptText, "AutomationClient build output is stale after rebuild");
        AssertContains(scriptText, "$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"");
        AssertContains(scriptText, "\"--command\", $Command");
        AssertContains(scriptText, "$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))");
        AssertContains(scriptText, "\"--payload-base64\", $payloadBase64");
        AssertContains(scriptText, "[int]$ResponseTimeoutMs = 0");
        AssertContains(scriptText, "\"--response-timeout-ms\", $ResponseTimeoutMs");
        AssertDoesNotContain(scriptText, "function Resolve-AutomationCommand");

        return Task.CompletedTask;
    }

}
