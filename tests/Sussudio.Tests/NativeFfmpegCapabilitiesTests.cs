using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class NativeFfmpegCapabilitiesTests
{
    private const string TestChildFlag = "--native-capability-test-child";
    private const string RuntimeRoot = @"C:\native runtime";
    private const string RuntimeVersions = "1/2/3/4";
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Theory]
    [InlineData(1, 1, 0, true)]
    [InlineData(2, 0, 0, false)]
    public void CompletedProbeDistinguishesAcceptedAndUnavailable(int outcome, int packets, int exitCode, bool expected)
    {
        var process = ProcessResult(outcome, packets, exitCode);
        Assert.Equal(expected, ReadResult(process));
    }

    [Theory]
    [InlineData("TimedOut", true)]
    [InlineData("Started", false)]
    [InlineData("ExitConfirmed", false)]
    [InlineData("ExitCode", 1)]
    [InlineData("StdOut", "not json")]
    [InlineData("StdOut", "{}")]
    public void IncompleteOrInvalidProcessIsNeverReportedAsUnsupported(string property, object value)
    {
        var process = ProcessResult();
        Set(process, property, value);
        var error = Assert.Throws<TargetInvocationException>(() => ReadResult(process));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Theory]
    [InlineData("Mode", 3)]
    [InlineData("ProtocolVersion", 0)]
    [InlineData("RuntimeRoot", @"C:\another runtime")]
    [InlineData("RuntimeVersions", "different build")]
    [InlineData("Codec", "av1_nvenc")]
    [InlineData("Width", 16)]
    [InlineData("Height", 16)]
    [InlineData("PacketCount", 0)]
    [InlineData("Outcome", 0)]
    public void ProbeResultMustMatchTheRequestedNativeRuntimeAndFixture(string property, object value)
    {
        var payload = Payload();
        payload[property] = value;
        var process = ProcessResult();
        Set(process, "StdOut", JsonSerializer.Serialize(payload));
        var error = Assert.Throws<TargetInvocationException>(() => ReadResult(process));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Fact]
    public void NormalArgumentsDoNotEnterTheProbeOrChangeLogging()
    {
        var previousLogRoot = Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT");
        foreach (var args in new[] { Array.Empty<string>(), new[] { "--normal-launch" } })
        {
            var arguments = new object?[] { args, null };
            Assert.False((bool)ProbeType.GetMethod("TryRunChildProcess", Static)!.Invoke(null, arguments)!);
            Assert.Equal(0, arguments[1]);
            Assert.Equal(previousLogRoot, Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT"));
        }
    }

    [Fact]
    public void MalformedPrivateArgumentsAreConsumedWithoutNativeInitialization()
    {
        var previousLogRoot = Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT");
        var arguments = new object?[] { new[] { "--native-ffmpeg-split-probe" }, null };
        Assert.True((bool)ProbeType.GetMethod("TryRunChildProcess", Static)!.Invoke(null, arguments)!);
        Assert.Equal(2, arguments[1]);
        Assert.Equal(previousLogRoot, Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT"));
    }

    [Fact]
    public void InaccessibleProbeLogRootIsRejectedBeforeSharedLoggingCanInitialize()
    {
        using var directory = new TestDirectory();
        var blocker = Path.Combine(directory.Path, "file-blocks-directory");
        File.WriteAllText(blocker, "sentinel");
        var previousLogRoot = Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT");
        var arguments = new object?[]
        {
            new[] { "--native-ffmpeg-split-probe", "--protocol-version", "1", "--runtime-root", RuntimeRoot,
                "--mode", "2", "--log-root", Path.Combine(blocker, "child-logs") }, null
        };
        Assert.True((bool)ProbeType.GetMethod("TryRunChildProcess", Static)!.Invoke(null, arguments)!);
        Assert.Equal(2, arguments[1]);
        Assert.Equal(previousLogRoot, Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT"));
        Assert.Equal("sentinel", File.ReadAllText(blocker));
    }

    [Fact]
    public async Task UnconfirmedChildExitStopsFurtherHardwareTrials()
    {
        var proxy = CreateSupervisor(out var control);
        control.Response = ProcessResult();
        Set(control.Response, "ExitConfirmed", false);
        Set(control.Response, "TimedOut", true);
        var task = (Task)LocatorType.GetMethod("ProbeSplitEncodeSupportAsync", Static, null,
            new[] { typeof(string), typeof(string), typeof(string), typeof(string), proxy.GetType().GetInterfaces().Single(t => t.Name == "IProcessSupervisor") }, null)!
            .Invoke(null, new object[] { RuntimeRoot, RuntimeVersions, @"C:\app\Sussudio.exe", @"C:\probe logs", proxy })!;
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task);
        Assert.Single(control.Requests);
    }

    [Fact]
    public async Task ProductionSupervisorTerminatesAChildThatStopsReturning()
    {
        var spec = Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessSpec"))!;
        Set(spec, "FileName", Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet");
        Set(spec, "Arguments", $"exec \"{typeof(NativeFfmpegCapabilitiesTests).Assembly.Location}\" {TestChildFlag} hang");
        Set(spec, "TimeoutMs", 2_000);
        var supervisor = Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessSupervisor"))!;
        var task = (Task)supervisor.GetType().GetMethod("RunAsync")!
            .Invoke(supervisor, new[] { spec, (object)CancellationToken.None })!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        Assert.True((bool)result.GetType().GetProperty("Started")!.GetValue(result)!);
        Assert.True((bool)result.GetType().GetProperty("TimedOut")!.GetValue(result)!);
        Assert.True((bool)result.GetType().GetProperty("ExitConfirmed")!.GetValue(result)!);
        Assert.Contains("NATIVE_TEST_CHILD_WAITING", (string)result.GetType().GetProperty("StdOut")!.GetValue(result)!);
    }

    [Fact]
    public async Task SplitTrialsUseTheAppHostAndExactNativeRoot()
    {
        var proxy = CreateSupervisor(out var control);
        control.ResponseForRequest = request =>
        {
            var arguments = (string)request.GetType().GetProperty("Arguments")!.GetValue(request)!;
            var mode = arguments.Contains("--mode 2 ", StringComparison.Ordinal) ? 2 : 3;
            var result = ProcessResult();
            var payload = Payload();
            payload["Mode"] = mode;
            Set(result, "StdOut", JsonSerializer.Serialize(payload));
            return result;
        };
        var method = LocatorType.GetMethods(Static).Single(m => m.Name == "ProbeSplitEncodeSupportAsync" && m.GetParameters().Length == 5);
        var task = (Task)method.Invoke(null, new object[] { RuntimeRoot, RuntimeVersions, @"C:\app\Sussudio.exe", @"C:\probe logs", proxy })!;
        await task;
        Assert.Equal(2, control.Requests.Count);
        foreach (var request in control.Requests)
        {
            Assert.Equal(@"C:\app\Sussudio.exe", request.GetType().GetProperty("FileName")!.GetValue(request));
            Assert.Equal(10_000, request.GetType().GetProperty("TimeoutMs")!.GetValue(request));
            var arguments = (string)request.GetType().GetProperty("Arguments")!.GetValue(request)!;
            Assert.StartsWith("--native-ffmpeg-split-probe --protocol-version 1 ", arguments);
            Assert.Contains("--runtime-root \"C:\\native runtime\"", arguments);
            Assert.DoesNotContain("ffmpeg.exe", arguments, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SplitRefreshPreservesTheLatestSelectedModeWhenTheBuildLacksIt()
    {
        var fixture = new CapabilityFixture();
        var refresh = fixture.StartRefresh();
        fixture.SelectedMode = "3-way";
        fixture.Complete(twoWay: false, threeWay: false);
        await refresh;
        Assert.Equal("3-way", fixture.SelectedMode);
        Assert.Contains("3-way", fixture.Modes);
        Assert.DoesNotContain("2-way", fixture.Modes);
        Assert.Equal(0, fixture.SelectionWrites);
        Assert.Contains("3-way", fixture.Status);
    }

    [Fact]
    public async Task FailedSplitRefreshLeavesChoicesAndSelectionUntouched()
    {
        var fixture = new CapabilityFixture();
        var initialModes = fixture.Modes.ToArray();
        var refresh = fixture.StartRefresh();
        fixture.Fail(new TimeoutException("driver did not return"));
        await refresh;
        Assert.Equal(initialModes, fixture.Modes);
        Assert.Equal("2-way", fixture.SelectedMode);
        Assert.Equal(0, fixture.SelectionWrites);
        Assert.Contains("could not be checked", fixture.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeCatalogRefreshPreservesTheLatestRequestedCodec(bool hdrEnabled)
    {
        var fixture = new CapabilityFixture { HdrEnabled = hdrEnabled };
        var refresh = fixture.StartFormatRefresh();
        fixture.SelectedFormat = "AV1";
        fixture.CompleteFormats(h264: true, hevc: true, av1: false);
        await refresh;
        Assert.Equal("AV1", fixture.SelectedFormat);
        Assert.Contains("AV1", fixture.Formats);
        Assert.Equal(0, fixture.FormatSelectionWrites);
        Assert.Contains("AV1", fixture.Status);
        Assert.Contains("unavailable", fixture.Status);
    }

    [Fact]
    public void BundledCatalogDoesNotRequireAnFfmpegExecutableOrPath()
    {
        using var directory = new TestDirectory();
        var assemblyPath = SussudioAssembly.Load().Location;
        var runtimeRoot = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "ffmpeg");
        Assert.False(File.Exists(Path.Combine(runtimeRoot, "ffmpeg.exe")));
        var child = RunChild("catalog", assemblyPath, runtimeRoot, Path.Combine(directory.Path, "child-logs"));
        Assert.True(child.ExitCode == 0, child.Output + child.Errors);
        using var payload = JsonDocument.Parse(child.Output);
        Assert.Equal(runtimeRoot, payload.RootElement.GetProperty("RuntimeRoot").GetString(), ignoreCase: true);
        Assert.True(payload.RootElement.GetProperty("Support").GetProperty("HasH264Nvenc").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("Support").GetProperty("HasHevcNvenc").GetBoolean());
        Assert.True(payload.RootElement.GetProperty("Support").GetProperty("HasAv1Nvenc").GetBoolean());
    }

    [Fact]
    public void InitializedNativeBindingCannotBeRetargeted()
    {
        using var directory = new TestDirectory();
        var assemblyPath = SussudioAssembly.Load().Location;
        var runtimeRoot = Path.Combine(Path.GetDirectoryName(assemblyPath)!, "ffmpeg");
        var child = RunChild("root-conflict", assemblyPath, runtimeRoot, Path.Combine(directory.Path, "child-logs"));
        Assert.True(child.ExitCode == 0, child.Output + child.Errors);
        Assert.Contains("NATIVE_ROOT_RETARGET_REJECTED", child.Output);
    }

    [Fact]
    public void PrivateProbeRejectsAnIncompleteRuntimeWithoutFallbackOrParentLogMutation()
    {
        using var directory = new TestDirectory();
        var parentLog = Path.Combine(directory.Path, "Sussudio_Debug.log");
        File.WriteAllText(parentLog, "active-session-sentinel");
        var originalLog = File.ReadAllBytes(parentLog);
        var missingRoot = Path.Combine(directory.Path, "old-abi");
        Directory.CreateDirectory(missingRoot);
        File.WriteAllText(Path.Combine(missingRoot, "avcodec-61.dll"), "wrong ABI fixture");
        var childLogRoot = Path.Combine(directory.Path, "child-logs");
        var child = RunChild("probe", SussudioAssembly.Load().Location, missingRoot, childLogRoot,
            "--native-ffmpeg-split-probe", "--protocol-version", "1", "--runtime-root", missingRoot,
            "--mode", "2", "--log-root", childLogRoot);
        Assert.Equal(1, child.ExitCode);
        using var payload = JsonDocument.Parse(child.Output);
        Assert.Equal(0, payload.RootElement.GetProperty("Outcome").GetInt32());
        Assert.Equal("initialize runtime", payload.RootElement.GetProperty("Operation").GetString());
        Assert.Equal(missingRoot, payload.RootElement.GetProperty("RuntimeRoot").GetString());
        Assert.Contains("wrong ABI", payload.RootElement.GetProperty("Error").GetString());
        Assert.Equal(originalLog, File.ReadAllBytes(parentLog));
        Assert.Empty(Directory.GetFiles(directory.Path, "Sussudio_Debug_*.log"));
    }

    // HarnessCore dispatches this before its normal assembly-load smoke mode.
    // Native bindings are process-wide, so each real-runtime assertion owns a child.
    internal static bool TryRunChildProcess(string[] args, out int exitCode)
    {
        exitCode = 2;
        if (args.Length == 0 || args[0] != TestChildFlag) return false;
        if (args.Length == 2 && args[1] == "hang")
        {
            Console.WriteLine("NATIVE_TEST_CHILD_WAITING");
            Console.Out.Flush();
            Thread.Sleep(Timeout.Infinite);
            return true;
        }
        if (args.Length < 5) return true;
        Environment.SetEnvironmentVariable("SUSSUDIO_LOG_ROOT", args[4]);
        Environment.SetEnvironmentVariable("PATH", string.Empty);
        var assembly = Assembly.LoadFrom(args[2]);
        var init = assembly.GetType("Sussudio.Services.Runtime.FfmpegRuntimeInit", throwOnError: true)!;
        if (args[1] == "probe")
        {
            var probe = assembly.GetType("Sussudio.Services.Runtime.NativeFfmpegCapabilityProbe", throwOnError: true)!;
            var forwarded = new object?[] { args.Skip(5).ToArray(), null };
            var handled = (bool)probe.GetMethod("TryRunChildProcess", Static)!.Invoke(null, forwarded)!;
            exitCode = handled ? (int)forwarded[1]! : 3;
            return true;
        }

        init.GetMethod("EnsureInitializedAtRoot", Static)!.Invoke(null, new object[] { args[3] });
        if (args[1] == "catalog")
        {
            var locator = assembly.GetType("Sussudio.Services.Runtime.FfmpegRuntimeLocator", throwOnError: true)!;
            var support = locator.GetMethod("ReadNativeEncoderSupport", Static)!.Invoke(null, null);
            var root = init.GetMethod("GetInitializedRuntimeRoot", Static)!.Invoke(null, null);
            Console.WriteLine(JsonSerializer.Serialize(new { RuntimeRoot = root, Support = support }));
            exitCode = 0;
        }
        else if (args[1] == "root-conflict")
        {
            try
            {
                init.GetMethod("EnsureInitializedAtRoot", Static)!.Invoke(null, new object[] { Path.Combine(args[3], "different-root") });
                exitCode = 4;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                Console.WriteLine("NATIVE_ROOT_RETARGET_REJECTED");
                exitCode = 0;
            }
        }

        var logger = assembly.GetType("Sussudio.Logger", throwOnError: true)!;
        var shutdown = (Task)logger.GetMethod("ShutdownAsync", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, new object[] { TimeSpan.FromSeconds(5) })!;
        shutdown.GetAwaiter().GetResult();
        return true;
    }

    private static ChildResult RunChild(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(typeof(NativeFfmpegCapabilitiesTests).Assembly.Location);
        startInfo.ArgumentList.Add(TestChildFlag);
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);
        startInfo.Environment["SUSSUDIO_LOG_ROOT"] = arguments[3];
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Native test child did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15_000))
        {
            process.Kill(entireProcessTree: true);
            Assert.True(process.WaitForExit(5_000), "Native test child termination was not confirmed.");
            throw new TimeoutException("Native test child exceeded its deadline.");
        }
        return new ChildResult(process.ExitCode, output.GetAwaiter().GetResult(), errors.GetAwaiter().GetResult());
    }

    private sealed record ChildResult(int ExitCode, string Output, string Errors);

    private sealed class TestDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Sussudio-native-tests", Guid.NewGuid().ToString("N"));
        public TestDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private static Type ProbeType => RequireType("Sussudio.Services.Runtime.NativeFfmpegCapabilityProbe");
    private static Type LocatorType => RequireType("Sussudio.Services.Runtime.FfmpegRuntimeLocator");
    private static Type RequireType(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static void Set(object instance, string property, object? value) => instance.GetType().GetProperty(property, Instance)!.SetValue(instance, value);
    private static bool ReadResult(object process) => (bool)ProbeType.GetMethod("ReadAcceptedResult", Static)!.Invoke(null, new[] { process, RuntimeRoot, RuntimeVersions, 2 })!;

    private static Dictionary<string, object> Payload(int outcome = 1, int packets = 1) => new()
    {
        ["ProtocolVersion"] = 1, ["Mode"] = 2, ["RuntimeRoot"] = RuntimeRoot, ["RuntimeVersions"] = RuntimeVersions,
        ["Codec"] = "hevc_nvenc", ["Width"] = 3840, ["Height"] = 2160, ["Outcome"] = outcome,
        ["Operation"] = "completed", ["PacketCount"] = packets, ["ElapsedMilliseconds"] = 1
    };

    private static object ProcessResult(int outcome = 1, int packets = 1, int exitCode = 0)
    {
        var result = Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessRunResult"))!;
        Set(result, "Started", true);
        Set(result, "ExitConfirmed", true);
        Set(result, "ExitCode", exitCode);
        Set(result, "StdOut", JsonSerializer.Serialize(Payload(outcome, packets)));
        return result;
    }

    private static object CreateSupervisor(out SupervisorProxy control)
    {
        var proxy = DispatchProxy.Create(RequireType("Sussudio.Services.Runtime.IProcessSupervisor"), typeof(SupervisorProxy));
        control = (SupervisorProxy)proxy;
        return proxy;
    }

    public class SupervisorProxy : DispatchProxy
    {
        public object? Response { get; set; }
        public Func<object, object>? ResponseForRequest { get; set; }
        public List<object> Requests { get; } = new();
        protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var request = args![0]!;
            Requests.Add(request);
            var response = ResponseForRequest?.Invoke(request) ?? Response!;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(response.GetType()).Invoke(null, new[] { response })!;
        }
    }

    private sealed class CapabilityFixture
    {
        private readonly object _completion;
        private readonly object _formatCompletion;
        private readonly object _controller;
        public string SelectedMode = "2-way";
        public string SelectedFormat = "HEVC";
        public bool HdrEnabled;
        public bool FfmpegMissing;
        public string Status = string.Empty;
        public int SelectionWrites;
        public int FormatSelectionWrites;
        public List<string> Modes = new() { "Auto", "Disabled", "2-way", "3-way" };
        public List<string> Formats = new() { "H.264", "HEVC", "AV1" };

        public CapabilityFixture()
        {
            var supportType = RequireType("Sussudio.Models.SplitEncodeSupport");
            _completion = Activator.CreateInstance(typeof(TaskCompletionSource<>).MakeGenericType(supportType))!;
            _formatCompletion = Activator.CreateInstance(typeof(TaskCompletionSource<>).MakeGenericType(RequireType("Sussudio.Models.EncoderSupport")))!;
            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelRecordingCapabilityControllerContext"))!;
            var probeProperty = context.GetType().GetProperty("GetSplitEncodeSupportAsync")!;
            var task = _completion.GetType().GetProperty("Task")!.GetValue(_completion)!;
            Set(context, probeProperty.Name, Expression.Lambda(probeProperty.PropertyType, Expression.Constant(task)).Compile());
            var formatProbeProperty = context.GetType().GetProperty("GetEncoderSupportAsync")!;
            var formatTask = _formatCompletion.GetType().GetProperty("Task")!.GetValue(_formatCompletion)!;
            Set(context, formatProbeProperty.Name, Expression.Lambda(formatProbeProperty.PropertyType, Expression.Constant(formatTask)).Compile());
            Set(context, "HasUiThreadAccess", (Func<bool>)(() => true));
            Set(context, "GetSelectedSplitEncodeMode", (Func<string>)(() => SelectedMode));
            Set(context, "SetSelectedSplitEncodeMode", (Action<string>)(value => { SelectionWrites++; SelectedMode = value; }));
            Set(context, "SetStatusText", (Action<string>)(value => Status = value));
            Set(context, "GetAvailableSplitEncodeModes", (Func<IReadOnlyCollection<string>>)(() => Modes));
            Set(context, "ReplaceAvailableSplitEncodeModes", (Action<IReadOnlyList<string>>)(modes => Modes = modes.ToList()));
            Set(context, "DefaultRecordingFormat", "H.264");
            Set(context, "HevcRecordingFormat", "HEVC");
            Set(context, "Av1RecordingFormat", "AV1");
            Set(context, "GetSelectedRecordingFormat", (Func<string>)(() => SelectedFormat));
            Set(context, "SetSelectedRecordingFormat", (Action<string>)(value => { FormatSelectionWrites++; SelectedFormat = value; }));
            Set(context, "NotifySelectedRecordingFormatChanged", (Action)(() => { }));
            Set(context, "GetAvailableRecordingFormats", (Func<IReadOnlyCollection<string>>)(() => Formats));
            Set(context, "ReplaceAvailableRecordingFormats", (Action<IReadOnlyList<string>>)(formats => Formats = formats.ToList()));
            Set(context, "IsHdrEnabled", (Func<bool>)(() => HdrEnabled));
            Set(context, "IsFfmpegMissing", (Func<bool>)(() => FfmpegMissing));
            Set(context, "SetIsFfmpegMissing", (Action<bool>)(value => FfmpegMissing = value));
            _controller = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelRecordingCapabilityController"), context)!;
        }

        public Task StartRefresh() => (Task)_controller.GetType().GetMethod("RefreshSplitEncodeCapabilitiesAsync", Instance)!.Invoke(_controller, null)!;
        public Task StartFormatRefresh() => (Task)_controller.GetType().GetMethod("RefreshRecordingFormatCapabilitiesAsync", Instance)!.Invoke(_controller, null)!;
        public void CompleteFormats(bool h264, bool hevc, bool av1)
        {
            var support = Activator.CreateInstance(RequireType("Sussudio.Models.EncoderSupport"))!;
            Set(support, "HasH264Nvenc", h264);
            Set(support, "HasHevcNvenc", hevc);
            Set(support, "HasAv1Nvenc", av1);
            _formatCompletion.GetType().GetMethod("SetResult")!.Invoke(_formatCompletion, new[] { support });
        }
        public void Complete(bool twoWay, bool threeWay)
        {
            var support = Activator.CreateInstance(RequireType("Sussudio.Models.SplitEncodeSupport"), twoWay, threeWay)!;
            _completion.GetType().GetMethod("SetResult")!.Invoke(_completion, new[] { support });
        }
        public void Fail(Exception error) => _completion.GetType().GetMethod("SetException", new[] { typeof(Exception) })!.Invoke(_completion, new[] { error });
    }
}
