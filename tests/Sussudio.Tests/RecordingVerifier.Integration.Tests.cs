using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    /// <summary>
    /// A real IProcessSupervisor fake that returns crafted ffprobe output.
    /// This is the test seam that reviewers flagged as missing.
    /// </summary>
    private sealed class FakeProcessSupervisorImpl
    {
        private readonly List<(string FileName, string Arguments, string? PriorityClass)> _calls = new();
        private string _streamInfoOutput = string.Empty;
        private string _cadenceOutput = string.Empty;
        private string _hdrSideDataOutput = string.Empty;
        private bool _ffprobeVersionSucceeds = true;
        private int _exitCode;

        public IReadOnlyList<(string FileName, string Arguments, string? PriorityClass)> Calls => _calls;

        public FakeProcessSupervisorImpl WithStreamInfo(string output)
        {
            _streamInfoOutput = output;
            return this;
        }

        public FakeProcessSupervisorImpl WithCadenceJson(string json)
        {
            _cadenceOutput = json;
            return this;
        }

        public FakeProcessSupervisorImpl WithHdrSideDataJson(string json)
        {
            _hdrSideDataOutput = json;
            return this;
        }

        public FakeProcessSupervisorImpl WithFfprobeUnavailable()
        {
            _ffprobeVersionSucceeds = false;
            return this;
        }

        public FakeProcessSupervisorImpl WithExitCode(int code)
        {
            _exitCode = code;
            return this;
        }

        /// <summary>
        /// Creates an instance that implements IProcessSupervisor via a DispatchProxy.
        /// </summary>
        public object CreateProxy()
        {
            var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
            var specType = RequireType("Sussudio.Services.Runtime.ProcessSpec");

            // Use the generic DispatchProxy.Create<T, TProxy>() method
            var createMethod = typeof(DispatchProxy).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 2)
                .MakeGenericMethod(supervisorType, typeof(FakeSupervisorProxy));

            var proxy = createMethod.Invoke(null, null)!;

            // Set the callback on our proxy
            ((FakeSupervisorProxy)proxy).SetHandler(async (method, args) =>
            {
                var spec = args[0];
                var fileName = (string)specType.GetProperty("FileName")!.GetValue(spec)!;
                var arguments = (string)specType.GetProperty("Arguments")!.GetValue(spec)!;
                var priorityClass = specType.GetProperty("PriorityClass")!.GetValue(spec)?.ToString();
                _calls.Add((fileName, arguments, priorityClass));

                // Determine which probe this is based on arguments
                string stdout;
                if (arguments.Contains("-version"))
                {
                    return CreateProcessRunResult(
                        _ffprobeVersionSucceeds,
                        _ffprobeVersionSucceeds ? 0 : 1,
                        "ffprobe version N/A");
                }
                else if (arguments.Contains("-show_frames"))
                {
                    stdout = _cadenceOutput;
                }
                else if (arguments.Contains("side_data_list"))
                {
                    stdout = _hdrSideDataOutput;
                }
                else
                {
                    stdout = _streamInfoOutput;
                }

                return CreateProcessRunResult(true, _exitCode, stdout);
            });

            return proxy;
        }

        private static object CreateProcessRunResult(bool started, int exitCode, string stdOut)
        {
            var resultType = RequireType("Sussudio.Services.Runtime.ProcessRunResult");
            var result = RuntimeHelpers.GetUninitializedObject(resultType);
            SetPropertyBackingField(result, "Started", started);
            SetPropertyBackingField(result, "TimedOut", false);
            SetPropertyBackingField(result, "ExitConfirmed", true);
            SetPropertyBackingField(result, "ExitCode", (int?)exitCode);
            SetPropertyBackingField(result, "StdOut", stdOut);
            SetPropertyBackingField(result, "StdErr", string.Empty);
            return result;
        }
    }

    /// <summary>
    /// DispatchProxy implementation for IProcessSupervisor.
    /// The key challenge: Invoke must return Task&lt;ProcessRunResult&gt;, not Task&lt;object&gt;.
    /// We use a helper to wrap the result in the correctly-typed Task.
    /// </summary>
    public class FakeSupervisorProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[], Task<object>>? _handler;

        public void SetHandler(Func<MethodInfo, object?[], Task<object>> handler)
        {
            _handler = handler;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (_handler == null)
                throw new InvalidOperationException("Handler not set on FakeSupervisorProxy");

            // RunAsync returns Task<ProcessRunResult>. We must return that exact type,
            // not Task<object>. Use reflection to create a typed Task wrapper.
            var resultType = targetMethod!.ReturnType; // Task<ProcessRunResult>
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var innerType = resultType.GetGenericArguments()[0]; // ProcessRunResult
                return WrapAsTypedTask(_handler(targetMethod, args!), innerType);
            }

            return _handler(targetMethod, args!);
        }

        private static object WrapAsTypedTask(Task<object> objectTask, Type targetType)
        {
            // Create a TaskCompletionSource<ProcessRunResult> and wire it to our Task<object>
            var tcsType = typeof(TaskCompletionSource<>).MakeGenericType(targetType);
            var tcs = Activator.CreateInstance(tcsType)!;
            var setResultMethod = tcsType.GetMethod("SetResult")!;
            var setExceptionMethod = tcsType.GetMethod("SetException", new[] { typeof(Exception) })!;
            var taskProp = tcsType.GetProperty("Task")!;

            objectTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    setExceptionMethod.Invoke(tcs, new object[] { t.Exception!.InnerException! });
                else if (t.IsCanceled)
                    tcsType.GetMethod("SetCanceled", Type.EmptyTypes)!.Invoke(tcs, null);
                else
                    setResultMethod.Invoke(tcs, new[] { t.Result });
            }, TaskScheduler.Default);

            return taskProp.GetValue(tcs)!;
        }
    }

    private static object BuildRuntimeSnapshotForVerificationEx(
        string? requestedFormat = "HevcMp4",
        bool requestedHdrEnabled = false,
        bool hdrOutputActive = false,
        bool requestedHdrMasteringMetadata = false,
        uint? negotiatedWidth = 1920,
        uint? negotiatedHeight = 1080,
        uint? negotiatedFrameRateNumerator = 60,
        uint? negotiatedFrameRateDenominator = 1,
        string? flashbackExportOutputPath = null,
        string? flashbackExportVerificationFormat = null,
        string? lastOutputPath = null,
        string? recordingBackend = null,
        string? recordingIntegrityBackend = null)
    {
        var type = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "HdrOutputActive", hdrOutputActive);
        SetPropertyOrBackingField(snapshot, "RequestedHdrMasteringMetadata", (bool?)requestedHdrMasteringMetadata);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        SetPropertyOrBackingField(snapshot, "FlashbackExportOutputPath", flashbackExportOutputPath);
        SetPropertyOrBackingField(snapshot, "FlashbackExportVerificationFormat", flashbackExportVerificationFormat);
        SetPropertyOrBackingField(snapshot, "LastOutputPath", lastOutputPath);
        SetPropertyOrBackingField(snapshot, "RecordingBackend", recordingBackend);
        SetPropertyOrBackingField(snapshot, "RecordingIntegrityBackend", recordingIntegrityBackend);
        return snapshot;
    }

    private static object CreateVerifierWithFake(object fakeSupervisor)
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
        var ctor = verifierType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] { supervisorType, typeof(string) },
            modifiers: null)
            ?? throw new InvalidOperationException("RecordingVerifier internal constructor not found.");
        return ctor.Invoke(new object[] { fakeSupervisor, "ffprobe.exe" });
    }

    private static async Task<object> RunVerifyAsync(object verifier, string? outputPath, object snapshot)
    {
        var verifierType = verifier.GetType();
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");
        var task = verifyAsync.Invoke(verifier, new object?[] { outputPath, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    // ── Helper: build cadence JSON with uniform frame timestamps ──

    private static object BuildRuntimeSnapshotForVerification(
        string? requestedFormat = "HevcMp4",
        bool requestedHdrEnabled = false,
        uint? negotiatedWidth = 1920,
        uint? negotiatedHeight = 1080,
        uint? negotiatedFrameRateNumerator = 60,
        uint? negotiatedFrameRateDenominator = 1)
    {
        var type = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        return snapshot;
    }

    // RecordingVerifier early-exit paths and source-shape contracts.

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFileDoesNotExist()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("VerifyAsync not found.");

        var snapshot = BuildRuntimeSnapshotForVerification();
        var task = verifyAsync.Invoke(verifier, new object?[] { "/nonexistent/file.mp4", snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result")!;
        var result = resultProp.GetValue(task)!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
        AssertEqual(false, GetBoolProperty(result, "FileExists"), "FileExists");
        AssertContains(GetStringProperty(result, "Message"), "does not exist");
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFileIsEmpty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_test_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, Array.Empty<byte>());
        try
        {
            var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
            var verifier = Activator.CreateInstance(verifierType)!;
            var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)!;

            var snapshot = BuildRuntimeSnapshotForVerification();
            var task = verifyAsync.Invoke(verifier, new object?[] { tempFile, snapshot, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

            await task.ConfigureAwait(false);
            var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "output-empty");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_ReturnsFailure_WhenOutputPathIsNull()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var verifier = Activator.CreateInstance(verifierType)!;
        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance)!;

        var snapshot = BuildRuntimeSnapshotForVerification();
        var task = verifyAsync.Invoke(verifier, new object?[] { null, snapshot, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("VerifyAsync did not return Task.");

        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
    }

    internal static Task RecordingVerifier_ImplementsIRecordingVerifier()
    {
        var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
        var interfaceType = RequireType("Sussudio.Services.Contracts.IRecordingVerifier");

        AssertEqual(true, interfaceType.IsAssignableFrom(verifierType), "RecordingVerifier implements IRecordingVerifier");

        var verifyAsync = verifierType.GetMethod("VerifyAsync", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(verifyAsync, "RecordingVerifier.VerifyAsync");

        var parameters = verifyAsync!.GetParameters();
        AssertEqual(3, parameters.Length, "VerifyAsync parameter count");

        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");
        AssertEqual(true, verifyAsync.ReturnType.IsGenericType, "VerifyAsync returns generic Task");
        AssertEqual(resultType, verifyAsync.ReturnType.GetGenericArguments()[0], "VerifyAsync returns Task<RecordingVerificationResult>");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_CadenceAnalysisLivesWithVerifier()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public sealed class RecordingVerifier : IRecordingVerifier");
        AssertContains(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(rootText, "private static CadenceMetrics ComputeCadenceMetrics(");
        AssertContains(rootText, "private static double? TryGetFrameTimestampSeconds(JsonElement frame)");
        AssertContains(rootText, "private static double? TryGetJsonDouble(JsonElement element, string propertyName)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Cadence.cs")),
            "RecordingVerifier cadence ffprobe pass folded into verifier owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Ffprobe.cs")),
            "RecordingVerifier ffprobe helper partial folded into verifier owner");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerifier_ProbeValidationAndResultShapingOwnership()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/Verification/RecordingVerifier.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public async Task<RecordingVerificationResult> VerifyAsync(");
        AssertContains(rootText, "private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(");
        AssertContains(rootText, "private async Task<CadenceMetrics?> AnalyzeCadenceMetricsAsync(");
        AssertContains(rootText, "private static Dictionary<string, string> ParseKeyValueOutput(string output)");
        AssertContains(rootText, "private static double? TryParseRational(string? value)");
        AssertContains(rootText, "private ProcessSpec CreateFfprobeProcessSpec(");
        AssertContains(rootText, "private static void ValidateContainer(");
        AssertContains(rootText, "private static void ValidateCodec(");
        AssertContains(rootText, "private static void ValidateDimensions(");
        AssertContains(rootText, "private static double? ResolveExpectedFrameRate(");
        AssertContains(rootText, "private static void ValidateCadence(");
        AssertContains(rootText, "private readonly record struct HdrValidationResult(");
        AssertContains(rootText, "private static HdrValidationResult ValidateHdrMetadata(");
        AssertContains(rootText, "private static string ResolveExpectedFormat(");
        AssertContains(rootText, "private static bool IsFlashbackRecording(");
        AssertContains(rootText, "private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(");
        AssertContains(rootText, "private static HdrParityResult BuildHdrParityResult(");
        AssertContains(rootText, "private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(");
        AssertContains(rootText, "private static string? TryGetMismatchPart(");
        AssertContains(rootText, "private static RecordingVerificationResult CreateEarlyFailure(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Results.cs")),
            "RecordingVerifier.Results.cs folded into RecordingVerifier.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Validation.cs")),
            "RecordingVerifier validation policy folded into RecordingVerifier.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "Verification", "RecordingVerifier.Ffprobe.cs")),
            "RecordingVerifier ffprobe probe/process helpers folded into RecordingVerifier.cs");

        return Task.CompletedTask;
    }

    internal static Task RecordingVerificationResult_HasExpectedProperties()
    {
        var resultType = RequireType("Sussudio.Models.RecordingVerificationResult");

        var expectedProps = new[]
        {
            "TimestampUtc", "Succeeded", "Message", "OutputPath", "FileExists", "FileSizeBytes",
            "VerificationMode", "DetectedContainer", "DetectedVideoCodec", "DetectedPixelFormat",
            "DetectedColorPrimaries", "DetectedColorTransfer", "DetectedColorSpace",
            "DetectedHdrSideDataTypes", "HdrMetadataPresent", "HdrColorimetryValid",
            "HdrMasteringMetadataPresent", "HdrVerificationLevel",
            "DetectedWidth", "DetectedHeight", "DetectedFrameRate",
            "CadenceSampleCount", "CadenceObservedFps", "CadenceExpectedIntervalMs",
            "CadenceAverageIntervalMs", "CadenceP95IntervalMs", "CadenceMaxIntervalMs",
            "CadenceJitterStdDevMs", "CadenceSevereGapCount", "CadenceSevereGapPercent",
            "CadenceEstimatedDroppedFrames", "CadenceEstimatedDropPercent",
            "PrimaryMismatchCode", "PrimaryMismatchExpected", "PrimaryMismatchActual",
            "Mismatches", "HdrParity"
        };

        foreach (var prop in expectedProps)
        {
            AssertNotNull(
                resultType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance),
                $"RecordingVerificationResult.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task DedicatedLibAvVerificationScript_UsesFlashbackOffAndStrictVerification()
    {
        var scriptText = ReadRepoFile("tools/verify-dedicated-libav-recording.ps1")
            .Replace("\r\n", "\n");

        AssertContains(scriptText, "SetFlashbackEnabled");
        AssertContains(scriptText, "SetRecordingEnabled");
        AssertContains(scriptText, "VerifyLastRecording");
        AssertContains(scriptText, "[int]$ResponseTimeoutBufferMs = 5000");
        AssertContains(scriptText, "-ResponseTimeoutMs $ResponseTimeoutMs");
        AssertContains(scriptText, "$responseTimeoutMs = [Math]::Max($TimeoutMs + $ResponseTimeoutBufferMs, 60000)");
        AssertContains(scriptText, "-ResponseTimeoutMs $responseTimeoutMs -AllowFailure");
        AssertContains(scriptText, "$rawText = ($raw | Out-String).Trim()");
        AssertContains(scriptText, "if ($AllowFailure -and $null -ne $response)");
        AssertContains(scriptText, "$initialOutputPath = [string](Get-PropertyValue $initialSnapshot \"OutputPath\" \"\")");
        AssertContains(scriptText, "$outputPathChanged = -not [string]::IsNullOrWhiteSpace($OutputDirectory)");
        AssertContains(scriptText, "function Assert-AutomationResponseSucceeded");
        AssertContains(scriptText, "Invoke-Automation -Command \"SetOutputPath\" -Payload @{ outputPath = $initialOutputPath } -AllowFailure");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $stopResponse -Context \"Cleanup recording stop\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $outputPathResponse -Context \"Output path restore\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $flashbackResponse -Context \"Flashback restore\"");
        AssertContains(scriptText, "Assert-AutomationResponseSucceeded -Response $previewResponse -Context \"Preview stop cleanup\"");
        AssertContains(scriptText, "$verificationCompleted = $true");
        AssertContains(scriptText, "Dedicated LibAv verification cleanup failed");
        AssertDoesNotContain(scriptText, "ConvertFrom-Json -Depth");
        AssertContains(scriptText, "[int]$ExpectedWidth = 3840");
        AssertContains(scriptText, "[int]$ExpectedHeight = 2160");
        AssertContains(scriptText, "[double]$ExpectedFrameRate = 120.0");
        AssertContains(scriptText, "[double]$FrameRateTolerance = 1.0");
        AssertContains(scriptText, "[double]$MinExpectedFrameFraction = 0.80");
        AssertContains(scriptText, "[int]$MaxEncoderLastWriteAgeMs = 2000");
        AssertContains(scriptText, "RecordingFileGrowing");
        AssertContains(scriptText, "RecordingStopped");
        AssertContains(scriptText, "VideoFramesFlowing");
        AssertContains(scriptText, "function Assert-ExpectedRuntimeMode");
        AssertContains(scriptText, "NegotiatedWidth");
        AssertContains(scriptText, "ActualWidth");
        AssertContains(scriptText, "NegotiatedFrameRate");
        AssertContains(scriptText, "ActualFrameRate");
        AssertContains(scriptText, "Get-PropertyValue $Snapshot \"RecordingBackend\" \"\") -eq \"LibAv\"");
        AssertContains(scriptText, "FlashbackActive=false");
        AssertContains(scriptText, "RecordingEncodingFailed");
        AssertContains(scriptText, "EncoderLastWriteAgeMs");
        AssertContains(scriptText, "VideoDropsQueueSaturated");
        AssertContains(scriptText, "VideoDropsBacklogEviction");
        AssertContains(scriptText, "RecordingGpuFramesDropped");
        AssertContains(scriptText, "RecordingCudaFramesDropped");
        AssertContains(scriptText, "CaptureCadenceEstimatedDroppedFrames");
        AssertContains(scriptText, "DetectedWidth");
        AssertContains(scriptText, "DetectedHeight");
        AssertContains(scriptText, "DetectedFrameRate");
        AssertContains(scriptText, "RecordingVerifier treats ffprobe cadence metrics as optional");
        AssertContains(scriptText, "Verification CadenceSampleCount must be greater than zero.");
        AssertContains(scriptText, "if ($null -ne $estimatedDrops)");
        AssertContains(scriptText, "if ($null -ne $severeGaps)");
        AssertContains(scriptText, "function Assert-FileLevelIntegrityFallback");
        AssertContains(scriptText, "-show_entries\", \"stream=nb_frames,duration");
        AssertContains(scriptText, "Fallback ffprobe frame count");
        AssertContains(scriptText, "if ($null -eq $cadenceSampleCount)");
        AssertContains(scriptText, "CadenceEstimatedDroppedFrames");
        AssertContains(scriptText, "CadenceSevereGapCount");
        AssertContains(scriptText, "VideoFramesEnqueued regressed");
        AssertContains(scriptText, "EncoderVideoFramesEncoded regressed");
        AssertContains(scriptText, "RecordingTotalBytes regressed");
        AssertContains(scriptText, "$minimumExpectedFrames = [long][Math]::Floor($ExpectedFrameRate * $DurationSeconds * $MinExpectedFrameFraction)");
        AssertContains(scriptText, "NoRestoreFlashback");
        AssertContains(scriptText, "initialFlashbackActive");
        AssertContains(scriptText, "restoreFlashbackAfterDisable");

        AssertOccursBefore(
            scriptText,
            "Invoke-Automation -Command \"SetFlashbackEnabled\" -Payload @{ enabled = $false }",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $true }");
        AssertOccursBefore(
            scriptText,
            "Assert-ExpectedRuntimeMode -Snapshot $preDisable -Context \"Pre-record\"",
            "Invoke-Automation -Command \"SetFlashbackEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Assert-LibAvRuntimeSnapshot -Snapshot $recordingStart",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Assert-Condition ($progressObservedCount -gt 0)",
            "Invoke-Automation -Command \"SetRecordingEnabled\" -Payload @{ enabled = $false }");
        AssertOccursBefore(
            scriptText,
            "Wait-Condition -ConditionName \"RecordingStopped\"",
            "$verifyResponse = Invoke-Automation -Command \"VerifyLastRecording\"");
        AssertOccursBefore(
            scriptText,
            "$verification = Get-PropertyValue",
            "Assert-Verification -Verification $verification");
        AssertOccursBefore(
            scriptText,
            "$verificationCompleted = $true",
            "if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0)");
        AssertOccursBefore(
            scriptText,
            "if ($verificationCompleted -and $script:CleanupFailures.Count -gt 0)",
            "Write-Host \"Dedicated LibAv recording verification: PASS\"");

        return Task.CompletedTask;
    }

    private static string BuildCadenceJson(double fps, int frameCount)
    {
        var interval = 1.0 / fps;
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"frames\":[");
        for (var i = 0; i < frameCount; i++)
        {
            if (i > 0) sb.Append(',');
            var ts = i * interval;
            sb.Append($"{{\"best_effort_timestamp_time\":{ts:F6}}}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    // ── Integration test: ffprobe unavailable ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ffprobe_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 }); // minimal mp4 header
        try
        {
            var fake = new FakeProcessSupervisorImpl().WithFfprobeUnavailable();
            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: ffprobe exit code failure ──

    internal static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_exit_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithExitCode(1)
                .WithStreamInfo("");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx();
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "ffprobe-failed");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: codec match (HEVC) ──

    internal static async Task RecordingVerifier_RunsFfprobeBelowNormalPriority()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_priority_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            _ = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, fake.Calls.Count >= 2, "ffprobe calls recorded");
            foreach (var call in fake.Calls)
            {
                AssertEqual("BelowNormal", call.PriorityClass, $"ffprobe priority for {call.Arguments}");
            }
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hevc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
            AssertEqual((uint)1920, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedWidth")), "DetectedWidth");
            AssertEqual((uint)1080, (uint)Convert.ToInt64(GetPropertyValue(result, "DetectedHeight")), "DetectedHeight");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: codec mismatch ──

    internal static async Task RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_codec_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "codec-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: H264 codec match ──

    internal static async Task RecordingVerifier_PassesVerification_ForH264Format()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_h264_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=h264\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(requestedFormat: "H264Mp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("h264", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: resolution mismatch ──

    internal static async Task RecordingVerifier_UsesFlashbackExportVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportOutputPath: tempFile,
                flashbackExportVerificationFormat: "HevcMp4");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_UsesFlashbackRecordingVerificationFormat()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_flashback_recording_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "Av1Mp4",
                flashbackExportVerificationFormat: "HevcMp4",
                lastOutputPath: tempFile,
                recordingIntegrityBackend: "Flashback");
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("hevc", GetStringProperty(result, "DetectedVideoCodec"), "DetectedVideoCodec");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    internal static async Task RecordingVerifier_DetectsResolutionMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_res_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1280\n" +
                    "height=720\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n")
                ;

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedWidth: 1920, negotiatedHeight: 1080);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "resolution-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: frame rate mismatch ──

    internal static async Task RecordingVerifier_DetectsFrameRateMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_fps_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=30/1\n" +
                    "r_frame_rate=30/1\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertContains(GetStringProperty(result, "PrimaryMismatchCode"), "fps-mismatch");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: HDR validation passes with correct metadata ──

    internal static async Task RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // Use hdrOutputActive=true (not requestedHdrEnabled) to trigger HDR validation
            // without the ProbeHdrSideDataAsync JSON path (avoids System.Text.Json version mismatch)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=p010le\n" +
                    "color_primaries=bt2020\n" +
                    "color_transfer=smpte2084\n" +
                    "color_space=bt2020nc\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual("p010le", GetStringProperty(result, "DetectedPixelFormat"), "DetectedPixelFormat");
            AssertEqual(true, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(true, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Verified", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(true, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");
            AssertEqual("ColorimetryOnly", GetStringProperty(hdrParity, "VerificationLevel"), "HdrParity.VerificationLevel");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: HDR colorimetry mismatch ──

    internal static async Task RecordingVerifier_DetectsHdrColorimetryMismatch()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_hdr_bad_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // SDR colorimetry on an HDR-active recording (use hdrOutputActive, not requestedHdrEnabled
            // to avoid ProbeHdrSideDataAsync JSON path)
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=3840\n" +
                    "height=2160\n" +
                    "avg_frame_rate=60/1\n" +
                    "r_frame_rate=60/1\n" +
                    "pix_fmt=yuv420p\n" +
                    "color_primaries=bt709\n" +
                    "color_transfer=bt709\n" +
                    "color_space=bt709\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                requestedFormat: "HevcMp4",
                requestedHdrEnabled: false,
                hdrOutputActive: true,
                negotiatedWidth: 3840,
                negotiatedHeight: 2160);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            // Should have multiple HDR-related mismatches
            var mismatches = GetPropertyValue(result, "Mismatches") as System.Collections.IEnumerable;
            var mismatchList = new List<string>();
            foreach (var m in mismatches!) mismatchList.Add(m?.ToString() ?? "");
            var hasPixfmtMismatch = mismatchList.Any(m => m.Contains("pixfmt-not-10bit"));
            var hasColorimetryMismatch = mismatchList.Any(m => m.Contains("colorimetry-mismatch"));
            AssertEqual(true, hasPixfmtMismatch, "Has pixfmt-not-10bit mismatch");
            AssertEqual(true, hasColorimetryMismatch, "Has colorimetry-mismatch");

            AssertEqual(false, GetPropertyValue(result, "HdrMetadataPresent"), "HdrMetadataPresent");
            AssertEqual(false, GetPropertyValue(result, "HdrColorimetryValid"), "HdrColorimetryValid");
            AssertEqual("ColorimetryOnly", GetStringProperty(result, "HdrVerificationLevel"), "HdrVerificationLevel");

            var hdrParity = GetPropertyValue(result, "HdrParity")!;
            AssertEqual("Mismatch", GetStringProperty(hdrParity, "Status"), "HdrParity.Status");
            AssertEqual(false, GetBoolProperty(hdrParity, "Verified"), "HdrParity.Verified");

            var taxonomy = GetPropertyValue(hdrParity, "MismatchTaxonomy") as System.Collections.IEnumerable;
            var taxonomyEntries = new List<object>();
            foreach (var entry in taxonomy!) taxonomyEntries.Add(entry!);
            var hasHdrError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "HDR" &&
                GetStringProperty(entry, "Code") == "pixfmt-not-10bit" &&
                GetStringProperty(entry, "Severity") == "Error");
            var hasColorimetryError = taxonomyEntries.Any(entry =>
                GetStringProperty(entry, "Category") == "Colorimetry" &&
                GetStringProperty(entry, "Code") == "colorimetry-mismatch" &&
                GetStringProperty(entry, "Severity") == "Error");
            AssertEqual(true, hasHdrError, "HDR mismatch taxonomy is Error severity");
            AssertEqual(true, hasColorimetryError, "Colorimetry mismatch taxonomy is Error severity");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: NTSC frame rate tolerance ──

    internal static async Task RecordingVerifier_PassesNtscFrameRateWithinTolerance()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"rv_ntsc_{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 });
        try
        {
            // 59.94 fps (60000/1001) vs expected 60 fps — within 0.75 tolerance
            var fake = new FakeProcessSupervisorImpl()
                .WithStreamInfo(
                    "format_name=mov,mp4,m4a,3gp,3g2,mj2\n" +
                    "codec_name=hevc\n" +
                    "width=1920\n" +
                    "height=1080\n" +
                    "avg_frame_rate=60000/1001\n" +
                    "r_frame_rate=60000/1001\n" +
                    "pix_fmt=yuv420p\n");

            var verifier = CreateVerifierWithFake(fake.CreateProxy());
            var snapshot = BuildRuntimeSnapshotForVerificationEx(
                negotiatedFrameRateNumerator: 60, negotiatedFrameRateDenominator: 1);
            var result = await RunVerifyAsync(verifier, tempFile, snapshot);

            // 60 - 59.94 = 0.06 which is within 0.75 tolerance
            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
