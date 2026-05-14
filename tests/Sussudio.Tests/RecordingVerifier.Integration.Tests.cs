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
}
