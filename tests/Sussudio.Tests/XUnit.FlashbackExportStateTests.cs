using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackExportStateTests
{
    [Fact]
    public void NewStateHasNoExportOrDerivedActivity()
    {
        using var fixture = new ExportStateFixture();
        var snapshot = fixture.Snapshot(50_000);

        Assert.False(Get<bool>(snapshot, "Active"));
        Assert.Equal("NotStarted", Get<string>(snapshot, "Status"));
        Assert.Equal(0L, Get<long>(snapshot, "Id"));
        Assert.Equal(0L, Get<long>(snapshot, "ElapsedMs"));
        Assert.Equal(0L, Get<long>(snapshot, "LastProgressAgeMs"));
        Assert.Equal(0L, Get<long>(snapshot, "OutputBytes"));
        Assert.Equal(0d, Get<double>(snapshot, "ThroughputBytesPerSec"));
        Assert.Null(Get<object?>(snapshot, "LastResult"));
    }

    [Fact]
    public void BeginningAnotherExportResetsActivityAndKeepsTheLastResult()
    {
        using var fixture = new ExportStateFixture();
        var first = fixture.Begin("first.mp4");
        fixture.Progress(first, 3, 4, 75);
        var previous = fixture.Result(false, "A source packet was unavailable.", "flashback-export-input-unavailable");
        fixture.Call("RecordLastResult", first, previous);
        fixture.Call("CompleteDiagnostics", first, previous);

        var next = fixture.Begin("next.mp4", TimeSpan.FromMilliseconds(1250), TimeSpan.MaxValue);
        var snapshot = fixture.Snapshot();

        Assert.True(next > first);
        Assert.True(Get<bool>(snapshot, "Active"));
        Assert.Equal("Running", Get<string>(snapshot, "Status"));
        Assert.Equal(fixture.PathFor("next.mp4"), Get<string>(snapshot, "OutputPath"));
        Assert.Equal(1250L, Get<long>(snapshot, "InPointMs"));
        Assert.Equal(-1L, Get<long>(snapshot, "OutPointMs"));
        Assert.Equal(0L, Get<long>(snapshot, "CompletedUtcUnixMs"));
        Assert.Equal(0, Get<int>(snapshot, "SegmentsProcessed"));
        Assert.Equal(0, Get<int>(snapshot, "TotalSegments"));
        Assert.Equal(0d, Get<double>(snapshot, "Percent"));
        Assert.Empty(Get<string>(snapshot, "Message"));
        Assert.Empty(Get<string>(snapshot, "FailureKind"));
        Assert.Same(previous, Get<object>(snapshot, "LastResult"));
        Assert.Equal(first, Get<long>(snapshot, "LastResultId"));
    }

    [Fact]
    public void StaleProgressCompletionAndRotationCannotReplaceTheCurrentExport()
    {
        using var fixture = new ExportStateFixture();
        var oldId = fixture.Begin("old.mp4");
        var currentId = fixture.Begin("current.mp4");
        fixture.Progress(currentId, 2, 8, 25);
        var now = Get<long>(fixture.Snapshot(), "StartedUtcUnixMs") + 2000;
        var before = fixture.Snapshot(now);

        fixture.Progress(oldId, 8, 8, 100);
        fixture.Call("CompleteDiagnostics", oldId, fixture.Result(true, "Old export completed."));
        fixture.Call("RecordForceRotateFallback", oldId, 9, TimeSpan.Zero, TimeSpan.MaxValue);

        Assert.Equal(before, fixture.Snapshot(now));
    }

    [Fact]
    public void ARejectedConcurrentRequestDoesNotReplaceActiveDiagnostics()
    {
        using var fixture = new ExportStateFixture();
        var activeId = fixture.Begin("active.mp4", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        fixture.Progress(activeId, 2, 4, 50);
        var started = Get<long>(fixture.Snapshot(), "StartedUtcUnixMs");
        var rejection = fixture.Result(false, "Recording owns the buffer.", "flashback-export-unavailable-during-recording");

        fixture.Call("RecordRejectedDiagnostics", fixture.PathFor("rejected.mp4"), rejection, null, null);

        var snapshot = fixture.Snapshot();
        Assert.Equal(activeId, Get<long>(snapshot, "Id"));
        Assert.True(Get<bool>(snapshot, "Active"));
        Assert.Equal("Running", Get<string>(snapshot, "Status"));
        Assert.Equal(fixture.PathFor("active.mp4"), Get<string>(snapshot, "OutputPath"));
        Assert.Equal(started, Get<long>(snapshot, "StartedUtcUnixMs"));
        Assert.Equal(1000L, Get<long>(snapshot, "InPointMs"));
        Assert.Equal(5000L, Get<long>(snapshot, "OutPointMs"));
        Assert.Equal(50d, Get<double>(snapshot, "Percent"));
        Assert.Empty(Get<string>(snapshot, "Message"));
        Assert.Empty(Get<string>(snapshot, "FailureKind"));
        Assert.Same(rejection, Get<object>(snapshot, "LastResult"));
        Assert.Equal(0L, Get<long>(snapshot, "LastResultId"));

        var completed = fixture.Result(true, "Saved the active export.");
        fixture.Call("RecordLastResult", activeId, completed);
        fixture.Call("CompleteDiagnostics", activeId, completed);
        Assert.Same(completed, Get<object>(fixture.Snapshot(), "LastResult"));
        Assert.Equal(activeId, Get<long>(fixture.Snapshot(), "LastResultId"));
    }

    [Theory]
    [InlineData(-2, -3, double.NaN, 0, 0, 0d)]
    [InlineData(7, 4, 125d, 4, 4, 100d)]
    [InlineData(2, 4, -10d, 2, 4, 0d)]
    [InlineData(3, 0, 50d, 3, 0, 50d)]
    [InlineData(1, 4, double.PositiveInfinity, 1, 4, 0d)]
    [InlineData(1, 4, double.NegativeInfinity, 1, 4, 0d)]
    [InlineData(3, 8, 37.5d, 3, 8, 37.5d)]
    public void ProgressPublishesFiniteBoundedValuesAndAllowsUnknownTotal(
        int processed, int total, double percent, int expectedProcessed, int expectedTotal, double expectedPercent)
    {
        using var fixture = new ExportStateFixture();
        var id = fixture.Begin("export.mp4");

        fixture.Progress(id, processed, total, percent);

        var snapshot = fixture.Snapshot();
        Assert.Equal(expectedProcessed, Get<int>(snapshot, "SegmentsProcessed"));
        Assert.Equal(expectedTotal, Get<int>(snapshot, "TotalSegments"));
        Assert.Equal(expectedPercent, Get<double>(snapshot, "Percent"));
        Assert.Equal(Get<long>(snapshot, "LastProgressUtcUnixMs"), (long)fixture.Call("ReadLastProgressUtcUnixMs")!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProgressIsStoredBeforeForwardingAndObserverFailureIsContained(bool throwFromObserver)
    {
        using var fixture = new ExportStateFixture();
        var id = fixture.Begin("export.mp4");
        var progress = fixture.CreateProgress(9, 4, 150);
        object? received = null;
        object? storedBeforeForward = null;
        var calls = 0;
        var observerType = typeof(ProgressObserver<>).MakeGenericType(progress.GetType());
        Action<object> observe = value =>
        {
            calls++;
            received = value;
            storedBeforeForward = fixture.Snapshot();
            if (throwFromObserver)
                throw new InvalidOperationException("The UI observer is unavailable.");
        };
        var observer = Activator.CreateInstance(observerType, observe)!;
        var sink = fixture.Call("CreateProgressSink", id, observer)!;
        var report = typeof(IProgress<>).MakeGenericType(progress.GetType()).GetMethod("Report")!;

        var error = Record.Exception(() => report.Invoke(sink, new[] { progress }));

        Assert.Null(error);
        Assert.Equal(1, calls);
        Assert.Same(progress, received);
        Assert.NotNull(storedBeforeForward);
        Assert.Equal(4, Get<int>(storedBeforeForward, "SegmentsProcessed"));
        Assert.Equal(100d, Get<double>(storedBeforeForward, "Percent"));
        Assert.Equal(100d, Get<double>(fixture.Snapshot(), "Percent"));
    }

    [Fact]
    public void SuccessfulCompletionFreezesElapsedTimeAndIgnoresLateProgress()
    {
        using var fixture = new ExportStateFixture();
        var id = fixture.Begin("export.mp4");
        fixture.Progress(id, 2, 3, 66.5);
        fixture.Call("CompleteDiagnostics", id, fixture.Result(true, "Export saved.", "flashback-export-cancelled"));
        var completed = fixture.Snapshot();
        var completedAt = Get<long>(completed, "CompletedUtcUnixMs");

        fixture.Progress(id, 0, 3, 0);
        var later = fixture.Snapshot(completedAt + 60_000);

        Assert.False(Get<bool>(later, "Active"));
        Assert.Equal("Succeeded", Get<string>(later, "Status"));
        Assert.Equal("Export saved.", Get<string>(later, "Message"));
        Assert.Empty(Get<string>(later, "FailureKind"));
        Assert.Equal(100d, Get<double>(later, "Percent"));
        Assert.Equal(2, Get<int>(later, "SegmentsProcessed"));
        Assert.True(completedAt > 0);
        Assert.Equal(completedAt, Get<long>(later, "LastProgressUtcUnixMs"));
        Assert.Equal(Get<long>(completed, "ElapsedMs"), Get<long>(later, "ElapsedMs"));
        Assert.Equal(0L, Get<long>(later, "LastProgressAgeMs"));
    }

    [Fact]
    public void ActiveMetricsUseTheRequestedSnapshotTimeAndCurrentOutputBytes()
    {
        using var fixture = new ExportStateFixture();
        var output = fixture.PathFor("export.mp4");
        File.WriteAllBytes(output, new byte[4096]);
        fixture.Begin("export.mp4");
        var started = Get<long>(fixture.Snapshot(), "StartedUtcUnixMs");

        var snapshot = fixture.Snapshot(started + 2000);

        Assert.Equal(2000L, Get<long>(snapshot, "ElapsedMs"));
        Assert.Equal(2000L, Get<long>(snapshot, "LastProgressAgeMs"));
        Assert.Equal(4096L, Get<long>(snapshot, "OutputBytes"));
        Assert.Equal(2048d, Get<double>(snapshot, "ThroughputBytesPerSec"));

        var regressedClock = fixture.Snapshot(started - 1);
        Assert.Equal(0L, Get<long>(regressedClock, "ElapsedMs"));
        Assert.Equal(0L, Get<long>(regressedClock, "LastProgressAgeMs"));
        Assert.Equal(0d, Get<double>(regressedClock, "ThroughputBytesPerSec"));
        File.Delete(output);
        Assert.Equal(0L, Get<long>(fixture.Snapshot(started + 2000), "OutputBytes"));
    }

    [Fact]
    public void OutputMetricsUseLastResultOnlyWhenThereIsNoDiagnosticOutputPath()
    {
        using var fixture = new ExportStateFixture();
        var previousPath = fixture.PathFor("previous.mp4");
        File.WriteAllBytes(previousPath, new byte[25]);
        var previous = fixture.Result(true, "Saved earlier.", outputPath: previousPath);
        fixture.Call("RecordLastResult", 42L, previous);

        Assert.Equal(25L, Get<long>(fixture.Snapshot(), "OutputBytes"));

        fixture.Begin("not-written-yet.mp4");
        Assert.Equal(0L, Get<long>(fixture.Snapshot(), "OutputBytes"));
    }

    [Fact]
    public void ForceRotationReportsCurrentCountAndRangeWithoutLosingEarlierAttempts()
    {
        using var fixture = new ExportStateFixture();
        var id = fixture.Begin("export.mp4");
        fixture.Call("RecordForceRotateFallback", id, 3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        var first = fixture.Snapshot();
        Assert.Equal(1L, Get<long>(first, "ForceRotateFallbacks"));
        Assert.Equal(3, Get<int>(first, "LastForceRotateFallbackSegments"));
        Assert.Equal(1000L, Get<long>(first, "LastForceRotateFallbackInPointMs"));
        Assert.Equal(5000L, Get<long>(first, "LastForceRotateFallbackOutPointMs"));

        fixture.Call("RecordForceRotateFallback", id, -1, TimeSpan.FromMilliseconds(1500), TimeSpan.MaxValue);

        var last = fixture.Snapshot();
        Assert.Equal(2L, Get<long>(last, "ForceRotateFallbacks"));
        Assert.Equal(0, Get<int>(last, "LastForceRotateFallbackSegments"));
        Assert.Equal(1500L, Get<long>(last, "LastForceRotateFallbackInPointMs"));
        Assert.Equal(-1L, Get<long>(last, "LastForceRotateFallbackOutPointMs"));
        Assert.True(Get<long>(last, "LastForceRotateFallbackUtcUnixMs") > 0);
    }

    private static T Get<T>(object instance, string name)
        => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;

    private sealed class ProgressObserver<T>(Action<object> observe) : IProgress<T>
    {
        public void Report(T value) => observe(value!);
    }

    private sealed class ExportStateFixture : IDisposable
    {
        private readonly object _state;
        private readonly string _directory;

        internal ExportStateFixture()
        {
            _state = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackExportState"), nonPublic: true)!;
            _directory = Path.Combine(Path.GetTempPath(), $"sussudio-export-state-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_directory);
        }

        internal string PathFor(string name) => Path.Combine(_directory, name);
        internal object? Call(string name, params object?[] arguments)
            => _state.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!.Invoke(_state, arguments);
        internal object Snapshot(long? now = null)
            => Call("CaptureHealthSnapshotFields", now ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())!;
        internal long Begin(string name, TimeSpan? start = null, TimeSpan? end = null)
            => (long)Call("BeginDiagnostics", start ?? TimeSpan.Zero, end ?? TimeSpan.FromSeconds(1), PathFor(name))!;
        internal object CreateProgress(int processed, int total, double percent)
            => Activator.CreateInstance(TypeOf("Sussudio.Models.ExportProgress"), processed, total, percent)!;
        internal void Progress(long id, int processed, int total, double percent)
            => Call("UpdateProgress", id, CreateProgress(processed, total, percent));
        internal object Result(bool succeeded, string message, string failureCode = "", string? outputPath = null)
        {
            var result = Activator.CreateInstance(TypeOf("Sussudio.Services.Contracts.FinalizeResult"))!;
            var outcome = result.GetType().GetProperty("Outcome")!;
            outcome.SetValue(result, Enum.Parse(outcome.PropertyType, succeeded ? "Saved" : "Failed"));
            result.GetType().GetProperty("StatusMessage")!.SetValue(result, message);
            result.GetType().GetProperty("FailureCode")!.SetValue(result, failureCode);
            result.GetType().GetProperty("OutputPath")!.SetValue(result, outputPath ?? PathFor("export.mp4"));
            return result;
        }

        private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}
