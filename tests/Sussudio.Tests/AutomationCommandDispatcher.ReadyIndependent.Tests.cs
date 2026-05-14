using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AutomationCommandDispatcher_CatalogReadyIndependentCommands_BypassDeviceReadiness()
    {
        var catalogType = RequireType("Sussudio.Tools.AutomationCommandCatalog");
        var readyIndependentCommands = GetCatalogEntries(catalogType)
            .Where(entry => !(bool)GetMetadataProperty(entry, "RequiresReadyDevices")!)
            .OrderBy(entry => Convert.ToInt32(GetMetadataProperty(entry, "Kind")!))
            .ToArray();
        AssertEqual(true, readyIndependentCommands.Length > 0, "ready-independent catalog commands exist");

        var tempRoot = Path.Combine(Path.GetTempPath(), "sussudio_ready_independent_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var dispatcher = CreateNoHardwareAutomationCommandDispatcher();
            var failures = new List<string>();

            foreach (var entry in readyIndependentCommands)
            {
                var name = (string)GetMetadataProperty(entry, "Name")!;
                var response = await ExecuteAutomationCommandAsync(
                        dispatcher,
                        CreateAutomationCommandRequest(
                            name,
                            authToken: null,
                            payloadJson: CreateReadyIndependentCommandPayload(name, tempRoot)))
                    .ConfigureAwait(false);

                var errorCode = (string?)GetPublicProperty(response, "ErrorCode");
                var status = GetPublicProperty(response, "Status")!.ToString();
                if (string.Equals(errorCode, "not-ready", StringComparison.Ordinal) ||
                    string.Equals(status, "NotReady", StringComparison.Ordinal))
                {
                    failures.Add($"{name}: rejected as device-not-ready");
                    continue;
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Ready-independent catalog commands must bypass AutomationCommandDispatcher device readiness: " +
                    string.Join("; ", failures));
            }
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static object CreateNoHardwareAutomationCommandDispatcher()
    {
        var viewModelType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("Sussudio.Services.Contracts.IAutomationWindowControl");
        var snapshot = CreateInstance("Sussudio.Models.AutomationSnapshot");

        object? Handler(MethodInfo? method, object?[]? args)
        {
            if (method?.Name == "get_IsInitialized")
            {
                return false;
            }

            if (method?.Name == "get_Devices")
            {
                return Activator.CreateInstance(
                    typeof(ObservableCollection<>).MakeGenericType(RequireType("Sussudio.Models.CaptureDevice")));
            }

            if (method?.Name == "GetLatestSnapshot")
            {
                return snapshot;
            }

            if (method?.Name == "RefreshSnapshotNowAsync")
            {
                return CreateTaskFromResult(method.ReturnType.GetGenericArguments()[0], snapshot);
            }

            return CreateNoHardwareReturnValue(method?.ReturnType ?? typeof(void));
        }

        return CreateAutomationCommandDispatcher(
            CreateConfiguredProxy(viewModelType, Handler),
            CreateConfiguredProxy(diagnosticsType, Handler),
            CreateConfiguredProxy(windowControlType, Handler),
            authToken: null);
    }

    private static string CreateReadyIndependentCommandPayload(string commandName, string tempRoot)
    {
        static string JsonString(string value) => JsonSerializer.Serialize(value);

        var outputPath = Path.Combine(tempRoot, $"{commandName}.tmp");
        return commandName switch
        {
            "SetOutputPath" => $"{{\"outputPath\":{JsonString(tempRoot)}}}",
            "WindowAction" => "{\"action\":\"restore\"}",
            "WaitForCondition" => "{\"condition\":\"PreviewFramesActive\",\"timeoutMs\":250,\"pollMs\":50}",
            "AssertSnapshot" => "{\"assertions\":[{\"field\":\"PreviewFramesDisplayed\",\"op\":\"eq\",\"value\":\"0\"}]}",
            "SetTrueHdrPreviewEnabled" => "{\"enabled\":false}",
            "CapturePreviewFrame" => $"{{\"outputPath\":{JsonString(outputPath)}}}",
            "CaptureWindowScreenshot" => $"{{\"outputPath\":{JsonString(outputPath)}}}",
            "SetShowAllCaptureOptions" => "{\"enabled\":false}",
            "SetPreviewVolume" => "{\"previewVolumePercent\":0}",
            "SetStatsVisible" => "{\"visible\":false}",
            "SetDeviceAudioMode" => "{\"mode\":\"hdmi\"}",
            "SetStatsSectionVisible" => "{\"section\":\"preview\",\"visible\":false}",
            "SetSettingsVisible" => "{\"visible\":false}",
            "FlashbackAction" => "{\"action\":\"pause\"}",
            "FlashbackExport" => $"{{\"seconds\":1,\"outputPath\":{JsonString(outputPath)},\"force\":true}}",
            "VerifyFile" => CreateVerifyFilePayload(tempRoot),
            "SetMicrophoneEnabled" => "{\"enabled\":false}",
            "SetFlashbackEnabled" => "{\"enabled\":false}",
            "SetFrameTimeOverlayVisible" => "{\"visible\":false}",
            "SetFlashbackTimelineVisible" => "{\"visible\":false}",
            "SetFullScreenEnabled" => "{\"enabled\":false}",
            "Authenticate" or
            "GetSnapshot" or
            "GetDiagnostics" or
            "RefreshDevices" or
            "ArmClose" or
            "VerifyLastRecording" or
            "ProbeVideoSource" or
            "ProbePreviewColor" or
            "GetCaptureOptions" or
            "GetPerformanceTimeline" or
            "FlashbackGetSegments" or
            "RestartFlashback" or
            "GetAudioRampTrace" or
            "GetAutomationManifest" or
            "OpenRecordingsFolder" => "{}",
            _ => throw new InvalidOperationException(
                $"Ready-independent automation command '{commandName}' needs a null-safe harness payload.")
        };
    }

    private static string CreateVerifyFilePayload(string tempRoot)
    {
        var filePath = Path.Combine(tempRoot, "verify-input.bin");
        File.WriteAllBytes(filePath, Array.Empty<byte>());
        return $"{{\"filePath\":{JsonSerializer.Serialize(filePath)}}}";
    }

    private static object? CreateNoHardwareReturnValue(Type returnType)
    {
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType == typeof(ValueTask))
        {
            return ValueTask.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            return CreateTaskFromResult(resultType, CreateNoHardwareValue(resultType));
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            return Activator.CreateInstance(returnType, CreateNoHardwareValue(resultType));
        }

        return CreateNoHardwareValue(returnType);
    }

    private static object? CreateNoHardwareValue(Type valueType)
    {
        if (valueType == typeof(string))
        {
            return string.Empty;
        }

        if (valueType == typeof(bool))
        {
            return false;
        }

        if (valueType.IsValueType)
        {
            return Activator.CreateInstance(valueType);
        }

        if (valueType.IsArray)
        {
            return Array.CreateInstance(valueType.GetElementType()!, 0);
        }

        if (TryCreateEmptyGenericArray(valueType, out var emptyArray))
        {
            return emptyArray;
        }

        var parameterlessConstructor = valueType.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor != null)
        {
            return Activator.CreateInstance(valueType);
        }

        return null;
    }

    private static bool TryCreateEmptyGenericArray(Type valueType, out object? emptyArray)
    {
        emptyArray = null;
        var genericEnumerable = valueType.IsGenericType &&
            valueType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                ? valueType
                : valueType.GetInterfaces()
                    .FirstOrDefault(candidate =>
                        candidate.IsGenericType &&
                        candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (genericEnumerable == null)
        {
            return false;
        }

        emptyArray = Array.CreateInstance(genericEnumerable.GetGenericArguments()[0], 0);
        return true;
    }
}
