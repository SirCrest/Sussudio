using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_WindowAction_DefaultsMissingActionToRestore()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("ParseWindowAction",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseWindowAction not found.");

        using var missingDoc = JsonDocument.Parse("{}");
        var missingResult = method.Invoke(null, new object[] { missingDoc.RootElement });
        AssertEqual("Restore", missingResult?.ToString(), "WindowAction missing action defaults to Restore");

        using var blankDoc = JsonDocument.Parse("{\"action\":\"  \"}");
        var blankResult = method.Invoke(null, new object[] { blankDoc.RootElement });
        AssertEqual("Restore", blankResult?.ToString(), "WindowAction blank action defaults to Restore");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WaitForCondition_DefaultsMissingConditionToPreviewFrames()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("ParseWaitCondition",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseWaitCondition not found.");

        using var missingDoc = JsonDocument.Parse("{}");
        var missingResult = method.Invoke(null, new object[] { missingDoc.RootElement });
        AssertEqual("PreviewFramesActive", missingResult?.ToString(), "WaitForCondition missing condition defaults to PreviewFramesActive");

        using var blankDoc = JsonDocument.Parse("{\"condition\":\"  \"}");
        var blankResult = method.Invoke(null, new object[] { blankDoc.RootElement });
        AssertEqual("PreviewFramesActive", blankResult?.ToString(), "WaitForCondition blank condition defaults to PreviewFramesActive");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_OneFieldHandlers_MatchCatalogPayloadFields()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var handlers = GetHandlerEntries(dispatcherType, "TrivialDeviceSelectionHandlers")
            .Concat(GetHandlerEntries(dispatcherType, "TrivialCaptureSettingsHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "TrivialAudioHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "TrivialPreviewRecordingHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "UiPreviewRecordingHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "UiStateHandlers"))
            .ToArray();

        AssertEqual(true, handlers.Length > 0, "dispatcher one-field handler tables are not empty");

        foreach (var entry in handlers)
        {
            var kind = GetPublicProperty(entry, "Key")
                       ?? throw new InvalidOperationException("Trivial handler entry key was null.");
            var commandName = kind.ToString()!;
            var handler = GetPublicProperty(entry, "Value")
                          ?? throw new InvalidOperationException($"Trivial handler for {commandName} was null.");
            var handlerPayloadFieldName = (string)GetPublicProperty(handler, "PayloadFieldName")!;
            var handlerPayloadFieldType = GetPublicProperty(handler, "PayloadFieldType")!.ToString();
            var catalogMetadata = GetAutomationCommandCatalogMetadata(kind);
            var catalogPayloadFields = GetMetadataCollection(catalogMetadata, "PayloadFields");

            AssertEqual(1, catalogPayloadFields.Length, $"{commandName} one-field catalog payload field count");
            var catalogPayloadField = catalogPayloadFields[0];
            AssertEqual(handlerPayloadFieldName, (string)GetMetadataProperty(catalogPayloadField, "Name")!, $"{commandName} one-field payload field name");
            AssertEqual(handlerPayloadFieldType, GetMetadataProperty(catalogPayloadField, "Type")!.ToString(), $"{commandName} one-field payload field type");
            AssertEqual(true, (bool)GetMetadataProperty(catalogPayloadField, "Required")!, $"{commandName} one-field payload field required");
        }

        return Task.CompletedTask;

        static object[] GetHandlerEntries(Type dispatcherType, string fieldName)
        {
            var handlersField = dispatcherType.GetField(
                fieldName,
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{fieldName} not found.");
            return ((IEnumerable)handlersField.GetValue(null)!)
                .Cast<object>()
                .ToArray();
        }

        static object GetAutomationCommandCatalogMetadata(object kind)
        {
            var catalogType = kind.GetType().Assembly.GetType("Sussudio.Tools.AutomationCommandCatalog")
                              ?? throw new InvalidOperationException("AutomationCommandCatalog not found.");
            var getMethod = catalogType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("AutomationCommandCatalog.Get not found.");
            return getMethod.Invoke(null, new[] { kind })
                   ?? throw new InvalidOperationException($"AutomationCommandCatalog.Get({kind}) returned null.");
        }
    }

    internal static Task AutomationCommandDispatcher_GetAudioRampTrace_MetadataMatchesDispatcherPayload()
    {
        var dispatcherText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var diagnosticCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.DiagnosticCommands.cs")
            .Replace("\r\n", "\n");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetAudioRampTrace:");
        AssertContains(dispatcherText, "ExecuteGetDiagnosticsCommand(payload, correlationId)");
        AssertContains(dispatcherText, "ExecuteGetPerformanceTimelineCommand(payload, correlationId)");
        AssertContains(dispatcherText, "ExecuteGetAudioRampTraceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(diagnosticCommandsText, "private AutomationCommandResponse ExecuteGetDiagnosticsCommand(");
        AssertContains(diagnosticCommandsText, "var maxEvents = GetInt(payload, \"maxEvents\") ?? 100;");
        AssertContains(diagnosticCommandsText, "private AutomationCommandResponse ExecuteGetPerformanceTimelineCommand(");
        AssertContains(diagnosticCommandsText, "var maxEntries = GetInt(payload, \"maxEntries\") ?? 240;");
        AssertContains(diagnosticCommandsText, "var maxEntries = GetInt(payload, \"maxEntries\") ?? 512;");
        AssertContains(diagnosticCommandsText, "GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken)");

        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var kind = Enum.Parse(enumType, "GetAudioRampTrace");
        var catalogMetadata = GetAutomationCommandCatalogMetadata(kind);
        var catalogPayloadFields = GetMetadataCollection(catalogMetadata, "PayloadFields");

        AssertEqual("{ maxEntries?: int }", (string)GetMetadataProperty(catalogMetadata, "PayloadShape")!, "GetAudioRampTrace payload shape");
        AssertEqual(1, catalogPayloadFields.Length, "GetAudioRampTrace catalog payload field count");
        var maxEntriesField = catalogPayloadFields[0];
        AssertEqual("maxEntries", (string)GetMetadataProperty(maxEntriesField, "Name")!, "GetAudioRampTrace payload field name");
        AssertEqual("Integer", GetMetadataProperty(maxEntriesField, "Type")!.ToString(), "GetAudioRampTrace payload field type");
        AssertEqual(false, (bool)GetMetadataProperty(maxEntriesField, "Required")!, "GetAudioRampTrace payload field required flag");

        return Task.CompletedTask;

        static object GetAutomationCommandCatalogMetadata(object kind)
        {
            var catalogType = kind.GetType().Assembly.GetType("Sussudio.Tools.AutomationCommandCatalog")
                              ?? throw new InvalidOperationException("AutomationCommandCatalog not found.");
            var getMethod = catalogType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("AutomationCommandCatalog.Get not found.");
            return getMethod.Invoke(null, new[] { kind })
                   ?? throw new InvalidOperationException($"AutomationCommandCatalog.Get({kind}) returned null.");
        }
    }
}
