using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class ProcessFailureEvidenceTests
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string NativeRuntimeRoot = @"C:\native runtime";
    private const string NativeRuntimeVersions = "1/2/3/4";

    [Fact]
    public async Task CompletedOutputReadPreservesTextWithoutFailure()
    {
        var result = await ReadOutputAsync(Task.FromResult("complete output"));
        Assert.Equal("complete output", result.Output);
        Assert.Null(result.Failure);
        Assert.Null(ReadFailure(ProcessResult()));
    }

    [Fact]
    public async Task FaultedOutputReadRetainsTheOriginalException()
    {
        var cause = new IOException("redirected pipe failed");
        var result = await ReadOutputAsync(Task.FromException<string>(cause));
        Assert.Equal(string.Empty, result.Output);
        Assert.Same(cause, result.Failure);
    }

    [Fact]
    public async Task UnfinishedOutputReadReturnsABoundedTimeoutFailure()
    {
        var pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var result = await ReadOutputAsync(pending.Task, timeoutMs: 20).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(string.Empty, result.Output);
            Assert.IsType<TimeoutException>(result.Failure);
        }
        finally
        {
            pending.TrySetResult(string.Empty);
        }
    }

    [Fact]
    public void BothStreamFailuresRetainTheirCausesAndStreamNames()
    {
        var stdoutCause = new IOException("stdout pipe closed");
        var stderrCause = new TimeoutException("stderr did not finish");
        var process = ProcessResult();
        Set(process, "StdOutReadException", stdoutCause);
        Set(process, "StdErrReadException", stderrCause);

        var failure = Assert.IsType<AggregateException>(ReadFailure(process));
        Assert.Same(stdoutCause, Get<Exception>(process, "StdOutReadException"));
        Assert.Same(stderrCause, Get<Exception>(process, "StdErrReadException"));
        Assert.Same(stdoutCause, failure.InnerExceptions[0].InnerException);
        Assert.Same(stderrCause, failure.InnerExceptions[1].InnerException);
        Assert.Contains("stdout", failure.Message);
        Assert.Contains(stdoutCause.Message, failure.Message);
        Assert.Contains("stderr", failure.Message);
        Assert.Contains(stderrCause.Message, failure.Message);
    }

    [Theory]
    [InlineData("availability", "StdOutReadException", "stdout")]
    [InlineData("availability", "StdErrReadException", "stderr")]
    [InlineData("metadata", "StdOutReadException", "stdout")]
    [InlineData("metadata", "StdErrReadException", "stderr")]
    [InlineData("HDR side-data", "StdOutReadException", "stdout")]
    [InlineData("HDR side-data", "StdErrReadException", "stderr")]
    [InlineData("cadence", "StdOutReadException", "stdout")]
    [InlineData("cadence", "StdErrReadException", "stderr")]
    public async Task EveryFfprobePassReportsReadFailureBeforeMetadataMismatch(string phase, string property, string stream)
    {
        using var fixture = new VerifierFixture();
        Exception cause = stream == "stdout"
            ? new IOException($"{phase} pipe failed")
            : new TimeoutException($"{phase} pipe did not finish");
        fixture.ConfigureResult = (currentPhase, process) =>
        {
            if (currentPhase == phase) Set(process, property, cause);
        };

        var result = await fixture.VerifyAsync();
        AssertProbeFailure(result);
        var message = Get<string>(result, "Message");
        Assert.Contains(phase, message);
        Assert.Contains(stream, message);
        Assert.Contains(cause.GetType().Name, message);
        Assert.Contains(cause.Message, message);
        Assert.Contains(phase, fixture.Phases);
    }

    [Fact]
    public async Task BothSecondaryProbeFailuresKeepBothCauses()
    {
        using var fixture = new VerifierFixture();
        fixture.ConfigureResult = (phase, process) =>
        {
            if (phase is "HDR side-data" or "cadence")
            {
                Set(process, "StdOutReadException", new IOException($"{phase} read cause"));
            }
        };

        var result = await fixture.VerifyAsync();
        AssertProbeFailure(result);
        Assert.Contains("HDR side-data read cause", Get<string>(result, "Message"));
        Assert.Contains("cadence read cause", Get<string>(result, "Message"));
    }

    [Theory]
    [InlineData("HDR side-data", "empty")]
    [InlineData("HDR side-data", "malformed")]
    [InlineData("HDR side-data", "start")]
    [InlineData("HDR side-data", "timeout")]
    [InlineData("HDR side-data", "exit")]
    [InlineData("cadence", "empty")]
    [InlineData("cadence", "malformed")]
    [InlineData("cadence", "start")]
    [InlineData("cadence", "timeout")]
    [InlineData("cadence", "exit")]
    public async Task OptionalProbeAbsenceKeepsItsExistingAcceptancePolicy(string phase, string outcome)
    {
        using var fixture = new VerifierFixture();
        fixture.ConfigureResult = (currentPhase, process) =>
        {
            if (currentPhase != phase) return;
            switch (outcome)
            {
                case "empty": Set(process, "StdOut", string.Empty); break;
                case "malformed": Set(process, "StdOut", "invalid json"); break;
                case "start": Set(process, "Started", false); break;
                case "timeout": Set(process, "TimedOut", true); break;
                case "exit": Set(process, "ExitCode", 1); break;
            }
        };

        var result = await fixture.VerifyAsync();
        Assert.True(Get<bool>(result, "Succeeded"), Get<string>(result, "Message"));
        Assert.Empty(Get<IEnumerable<string>>(result, "Mismatches"));
        if (phase == "cadence") Assert.Null(Get<object?>(result, "CadenceSampleCount"));
    }

    [Theory]
    [InlineData("availability")]
    [InlineData("metadata")]
    [InlineData("HDR side-data")]
    [InlineData("cadence")]
    public async Task FfprobeCancellationStillPropagates(string phase)
    {
        using var fixture = new VerifierFixture { CanceledPhase = phase };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.VerifyAsync(cancellation.Token));
        Assert.Contains(phase, fixture.Phases);
    }

    [Fact]
    public async Task ProductionSupervisorCancellationStaysBoundedAndPropagates()
    {
        var spec = Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessSpec"))!;
        Set(spec, "FileName", Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet");
        // Reuse the existing test harness child, which performs no app or native initialization.
        Set(spec, "Arguments", $"exec \"{typeof(NativeFfmpegCapabilitiesTests).Assembly.Location}\" --native-capability-test-child hang");
        Set(spec, "TimeoutMs", 2_000);
        var supervisor = Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessSupervisor"))!;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        var task = (Task)supervisor.GetType().GetMethod("RunAsync")!
            .Invoke(supervisor, new object[] { spec, cancellation.Token })!;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await task.WaitAsync(TimeSpan.FromSeconds(15)));
    }

    [Theory]
    [InlineData("StdOutReadException", 1)]
    [InlineData("StdOutReadException", 2)]
    [InlineData("StdErrReadException", 1)]
    [InlineData("StdErrReadException", 2)]
    public void NativeProbeDoesNotAcceptOrRejectCapabilityFromIncompleteOutput(string property, int outcome)
    {
        var cause = new IOException("native child pipe failed");
        var process = ProcessResult(JsonSerializer.Serialize(new
        {
            ProtocolVersion = 1, Mode = 2, RuntimeRoot = NativeRuntimeRoot, RuntimeVersions = NativeRuntimeVersions,
            Codec = "hevc_nvenc", Width = 3840, Height = 2160, Outcome = outcome,
            Operation = "completed", PacketCount = outcome == 1 ? 1 : 0
        }));
        Set(process, property, cause);
        var method = RequireType("Sussudio.Services.Runtime.NativeFfmpegCapabilityProbe").GetMethod("ReadAcceptedResult", Static)!;

        var invocation = Assert.Throws<TargetInvocationException>(() => method.Invoke(null,
            new[] { process, NativeRuntimeRoot, NativeRuntimeVersions, (object)2 }));
        var failure = Assert.IsType<InvalidOperationException>(invocation.InnerException);
        Assert.Contains(cause.Message, failure.Message);
        Assert.Same(cause, failure.InnerException!.InnerException);
    }

    [Theory]
    [InlineData("StdOutReadException")]
    [InlineData("StdErrReadException")]
    public void HdrValidatorDoesNotPassWhenAnOutputReadFailed(string property)
    {
        var cause = new IOException("validator pipe failed");
        var process = ProcessResult("validator succeeded");
        Set(process, property, cause);
        var method = RequireType("Sussudio.Services.Recording.HdrValidationRunner").GetMethod("ReadValidationResult", Static)!;
        var result = ((bool Success, string Detail))method.Invoke(null, new[] { process })!;
        Assert.False(result.Success);
        Assert.Contains("validator-output-read-failed", result.Detail);
        Assert.Contains(cause.GetType().Name, result.Detail);
        Assert.Contains(cause.Message, result.Detail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutputStatIoFailuresReturnStructuredEvidenceWithoutProbing(bool accessDenied)
    {
        using var fixture = new VerifierFixture();
        Exception cause = accessDenied
            ? new UnauthorizedAccessException("output permissions changed")
            : new IOException("output disappeared after existence check");
        fixture.ReadFileLength = path =>
        {
            Assert.Equal(fixture.OutputPath, path);
            throw cause;
        };

        var result = await fixture.VerifyAsync();
        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Equal("output-stat-failed", Get<string>(result, "PrimaryMismatchCode"));
        Assert.Equal(new[] { "output-stat-failed" }, Get<IEnumerable<string>>(result, "Mismatches"));
        Assert.Contains(cause.GetType().Name, Get<string>(result, "Message"));
        Assert.Contains(cause.Message, Get<string>(result, "Message"));
        Assert.Equal(fixture.OutputPath, Get<string>(result, "OutputPath"));
        Assert.True(Get<bool>(result, "FileExists"));
        Assert.Equal(0L, Get<long>(result, "FileSizeBytes"));
        Assert.Equal("none", Get<string>(result, "VerificationMode"));
        Assert.Empty(fixture.Phases);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutputStatDoesNotSwallowCancellationOrUnexpectedFailures(bool canceled)
    {
        using var fixture = new VerifierFixture();
        Exception cause = canceled
            ? new OperationCanceledException("stat canceled")
            : new InvalidOperationException("unexpected stat failure");
        fixture.ReadFileLength = _ => throw cause;

        var failure = await Record.ExceptionAsync(() => fixture.VerifyAsync());
        Assert.Same(cause, failure);
        Assert.Empty(fixture.Phases);
    }

    private static void AssertProbeFailure(object result)
    {
        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Equal("ffprobe-failed", Get<string>(result, "PrimaryMismatchCode"));
        Assert.Equal(new[] { "ffprobe-failed" }, Get<IEnumerable<string>>(result, "Mismatches"));
        Assert.Equal("ffprobe", Get<string>(result, "VerificationMode"));
    }

    private static async Task<(string Output, Exception? Failure)> ReadOutputAsync(Task<string> source, int timeoutMs = 500)
    {
        var method = RequireType("Sussudio.Services.Runtime.ProcessSupervisor").GetMethod("TryReadWithTimeoutAsync", Static)!;
        var task = (Task)method.Invoke(null, new object[] { source, timeoutMs })!;
        await task;
        return ((string Output, Exception? Failure))task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static object ProcessResult(string output = "")
    {
        var result = Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessRunResult"))!;
        Set(result, "Started", true);
        Set(result, "ExitConfirmed", true);
        Set(result, "ExitCode", 0);
        Set(result, "StdOut", output);
        return result;
    }

    private static Exception? ReadFailure(object result)
        => (Exception?)result.GetType().GetMethod("GetOutputReadFailure", Instance)!.Invoke(result, null);

    private static Type RequireType(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static void Set(object instance, string property, object? value) => instance.GetType().GetProperty(property, Instance)!.SetValue(instance, value);
    private static T Get<T>(object instance, string property) => (T)instance.GetType().GetProperty(property, Instance)!.GetValue(instance)!;

    private sealed class VerifierFixture : IDisposable
    {
        private readonly object _supervisor;
        public string OutputPath { get; } = Path.Combine(Path.GetTempPath(), $"Sussudio-verification-{Guid.NewGuid():N}.mp4");
        public List<string> Phases { get; } = new();
        public Action<string, object>? ConfigureResult { get; set; }
        public string? CanceledPhase { get; set; }
        public Func<string, long>? ReadFileLength { get; set; }

        public VerifierFixture()
        {
            File.WriteAllBytes(OutputPath, new byte[] { 1 });
            _supervisor = DispatchProxy.Create(RequireType("Sussudio.Services.Runtime.IProcessSupervisor"), typeof(SupervisorProxy));
            ((SupervisorProxy)_supervisor).Run = (spec, cancellation) =>
            {
                var arguments = Get<string>(spec, "Arguments");
                var phase = arguments.Contains("-version", StringComparison.Ordinal) ? "availability"
                    : arguments.Contains("-show_frames", StringComparison.Ordinal) ? "cadence"
                    : arguments.Contains("side_data_list", StringComparison.Ordinal) ? "HDR side-data"
                    : "metadata";
                Phases.Add(phase);
                if (CanceledPhase == phase) return Task.FromCanceled<object>(cancellation);
                var result = ProcessResult(phase switch
                {
                    "availability" => "ffprobe version fixture",
                    "cadence" => "{\"frames\":[{\"best_effort_timestamp_time\":\"0\"},{\"best_effort_timestamp_time\":\"0.0166666667\"}]}",
                    "HDR side-data" => "{\"streams\":[{\"side_data_list\":[{\"side_data_type\":\"Mastering display metadata\"}]}]}",
                    _ => "format_name=mov,mp4\ncodec_name=hevc\nwidth=1920\nheight=1080\navg_frame_rate=60/1\npix_fmt=yuv420p10le\ncolor_primaries=bt2020\ncolor_transfer=smpte2084\ncolor_space=bt2020nc\n"
                });
                ConfigureResult?.Invoke(phase, result);
                return Task.FromResult(result);
            };
        }

        public async Task<object> VerifyAsync(CancellationToken cancellationToken = default)
        {
            var snapshot = RuntimeHelpers.GetUninitializedObject(RequireType("Sussudio.Models.CaptureRuntimeSnapshot"));
            Set(snapshot, "RequestedFormat", "HevcMp4");
            Set(snapshot, "RequestedHdrEnabled", true);
            Set(snapshot, "HdrOutputActive", true);
            Set(snapshot, "RequestedHdrMasteringMetadata", false);
            Set(snapshot, "NegotiatedWidth", (uint?)1920);
            Set(snapshot, "NegotiatedHeight", (uint?)1080);
            Set(snapshot, "NegotiatedFrameRateNumerator", (uint?)60);
            Set(snapshot, "NegotiatedFrameRateDenominator", (uint?)1);
            var verifierType = RequireType("Sussudio.Services.Recording.RecordingVerifier");
            var supervisorType = RequireType("Sussudio.Services.Runtime.IProcessSupervisor");
            var readFileLength = ReadFileLength;
            var parameterTypes = readFileLength == null
                ? new[] { supervisorType, typeof(string) }
                : new[] { supervisorType, typeof(string), typeof(Func<string, long>) };
            var arguments = readFileLength == null
                ? new[] { _supervisor, "ffprobe.exe" }
                : new object[] { _supervisor, "ffprobe.exe", readFileLength };
            var verifier = verifierType.GetConstructor(Instance, null, parameterTypes, null)!.Invoke(arguments);
            var task = (Task)verifierType.GetMethod("VerifyAsync")!.Invoke(verifier,
                new[] { OutputPath, snapshot, (object)cancellationToken })!;
            await task;
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }

        public void Dispose() => File.Delete(OutputPath);
    }

    public class SupervisorProxy : DispatchProxy
    {
        public Func<object, CancellationToken, Task<object>> Run { get; set; } = null!;

        protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var resultType = targetMethod!.ReturnType.GetGenericArguments()[0];
            var result = Run(args![0]!, (CancellationToken)args[1]!);
            return typeof(SupervisorProxy).GetMethod(nameof(ConvertResultAsync), Static)!
                .MakeGenericMethod(resultType).Invoke(null, new object[] { result })!;
        }

        private static async Task<T> ConvertResultAsync<T>(Task<object> result) => (T)await result;
    }
}
