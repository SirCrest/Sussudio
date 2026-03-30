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
        private readonly List<(string FileName, string Arguments)> _calls = new();
        private string _streamInfoOutput = string.Empty;
        private string _cadenceOutput = string.Empty;
        private string _hdrSideDataOutput = string.Empty;
        private bool _ffprobeVersionSucceeds = true;
        private int _exitCode;

        public IReadOnlyList<(string FileName, string Arguments)> Calls => _calls;

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
            var supervisorType = RequireType("ElgatoCapture.Services.IProcessSupervisor");
            var specType = RequireType("ElgatoCapture.Services.ProcessSpec");

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
                _calls.Add((fileName, arguments));

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
            var resultType = RequireType("ElgatoCapture.Services.ProcessRunResult");
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
        uint? negotiatedFrameRateDenominator = 1)
    {
        var type = RequireType("ElgatoCapture.Models.CaptureRuntimeSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(type);
        SetPropertyOrBackingField(snapshot, "RequestedFormat", requestedFormat);
        SetPropertyOrBackingField(snapshot, "RequestedHdrEnabled", (bool?)requestedHdrEnabled);
        SetPropertyOrBackingField(snapshot, "HdrOutputActive", hdrOutputActive);
        SetPropertyOrBackingField(snapshot, "RequestedHdrMasteringMetadata", (bool?)requestedHdrMasteringMetadata);
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", negotiatedWidth);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", negotiatedHeight);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedFrameRateNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedFrameRateDenominator);
        return snapshot;
    }

    private static object CreateVerifierWithFake(object fakeSupervisor)
    {
        var verifierType = RequireType("ElgatoCapture.Services.Recording.RecordingVerifier");
        var supervisorType = RequireType("ElgatoCapture.Services.IProcessSupervisor");
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

    // ── Integration test: ffprobe unavailable ──

    private static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeUnavailable()
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

    // ── Integration test: codec match (HEVC) ──

    private static async Task RecordingVerifier_PassesVerification_WhenAllFieldsMatch_Hevc()
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

    private static async Task RecordingVerifier_DetectsCodecMismatch_WhenH264InsteadOfHevc()
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

    // ── Integration test: resolution mismatch ──

    private static async Task RecordingVerifier_DetectsResolutionMismatch()
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

    private static async Task RecordingVerifier_DetectsFrameRateMismatch()
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

    private static async Task RecordingVerifier_PassesHdrValidation_WhenAllHdrFieldsPresent()
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
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: HDR colorimetry mismatch ──

    private static async Task RecordingVerifier_DetectsHdrColorimetryMismatch()
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
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    // ── Integration test: H264 codec match ──

    private static async Task RecordingVerifier_PassesVerification_ForH264Format()
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

    // ── Integration test: NTSC frame rate tolerance ──

    private static async Task RecordingVerifier_PassesNtscFrameRateWithinTolerance()
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

    // ── Integration test: ffprobe exit code failure ──

    private static async Task RecordingVerifier_ReturnsFailure_WhenFfprobeExitsNonZero()
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
