using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackBackendCleanupTests
{
    [Fact]
    public async Task CanceledPurgeHandsOffCompletedBackendArtifactsWithoutDeletingSegments()
    {
        await using var fixture = new CleanupFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var disposal = fixture.DisposeBackendAsync(purgeSegments: true, cancellation.Token);
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => disposal.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(disposal.IsCanceled);
        Assert.False(fixture.HasAnyResource);

        await fixture.AssertCleanupRemainsBlockedAsync();
        Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);

        fixture.ReleaseExportLock();
        await fixture.WaitForCleanupAsync();
        Assert.Equal("retained segment", File.ReadAllText(fixture.SegmentPath));
        Assert.True(File.Exists(fixture.RetiredMarkerPath));
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Fact]
    public async Task CanceledPurgeWithPendingSinkCompletionKeepsTheScheduledPurge()
    {
        await using var fixture = new CleanupFixture();
        var sinkCompletion = fixture.CreateSinkCompletion();
        fixture.InstallDisposedSinkWithPendingCompletion(sinkCompletion.Task);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var disposal = fixture.DisposeBackendAsync(purgeSegments: true, cancellation.Token);
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => disposal.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(disposal.IsCanceled);
        Assert.False(fixture.HasAnyResource);
        fixture.AssertArtifactsUntouched();
        Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);

        fixture.ReleaseExportLock();
        await fixture.AssertCleanupRemainsBlockedAsync();
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
        sinkCompletion.SetResult();
        await fixture.WaitForCleanupAsync();
        Assert.False(File.Exists(fixture.SegmentPath));
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Fact]
    public async Task UncanceledPurgeCleansArtifactsUnderTheAlreadyHeldExportLock()
    {
        await using var fixture = new CleanupFixture();

        await fixture.DisposeBackendAsync(purgeSegments: true, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(fixture.HasAnyResource);
        Assert.True(fixture.BufferDisposed);
        Assert.True(fixture.ExporterDisposed);
        Assert.False(File.Exists(fixture.SegmentPath));
        Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);
        fixture.ReleaseExportLock();
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Fact]
    public async Task CanceledNonPurgingTeardownStillRetiresArtifactsUnderTheHeldLock()
    {
        await using var fixture = new CleanupFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await fixture.DisposeBackendAsync(purgeSegments: false, cancellation.Token)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(fixture.HasAnyResource);
        Assert.True(fixture.BufferDisposed);
        Assert.True(fixture.ExporterDisposed);
        Assert.Equal("retained segment", File.ReadAllText(fixture.SegmentPath));
        Assert.True(File.Exists(fixture.RetiredMarkerPath));
        Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);
        fixture.ReleaseExportLock();
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Fact]
    public async Task DeferredArtifactCleanupWaitsForAndReleasesTheExportLock()
    {
        await using var fixture = new CleanupFixture();

        var cleanup = fixture.CleanupArtifactsAfterExportAsync(purgeSegments: true);
        Assert.False(cleanup.IsCompleted);
        fixture.AssertArtifactsUntouched();
        Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);

        fixture.ReleaseExportLock();
        Assert.True(await cleanup.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(fixture.BufferDisposed);
        Assert.True(fixture.ExporterDisposed);
        Assert.False(File.Exists(fixture.SegmentPath));
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeferredCleanupWaitsForBothSinkCompletionAndExportLock(bool sinkCompletesFirst)
    {
        await using var fixture = new CleanupFixture();
        var sinkCompletion = fixture.CreateSinkCompletion();
        fixture.ScheduleDeferredCleanup(sinkCompletion.Task, purgeSegments: true);

        if (sinkCompletesFirst)
        {
            sinkCompletion.SetResult();
            await fixture.AssertCleanupRemainsBlockedAsync();
            Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);
            fixture.ReleaseExportLock();
        }
        else
        {
            fixture.ReleaseExportLock();
            await fixture.AssertCleanupRemainsBlockedAsync();
            Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
            sinkCompletion.SetResult();
        }

        await fixture.WaitForCleanupAsync();
        Assert.False(File.Exists(fixture.SegmentPath));
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Fact]
    public async Task FaultedSinkStillCleansArtifactsAfterExportLockBecomesAvailable()
    {
        await using var fixture = new CleanupFixture();
        var sinkCompletion = fixture.CreateSinkCompletion();
        fixture.ScheduleDeferredCleanup(sinkCompletion.Task, purgeSegments: true);
        sinkCompletion.SetException(new InvalidOperationException("sink failed while draining"));

        await fixture.AssertCleanupRemainsBlockedAsync();
        Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);
        fixture.ReleaseExportLock();
        await fixture.WaitForCleanupAsync();
        Assert.False(File.Exists(fixture.SegmentPath));
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RecoverySegmentsSurviveImmediateAndDeferredCleanup(bool deferred, bool purgeSegments)
    {
        await using var fixture = new CleanupFixture();
        fixture.PreserveRecoverySegments();

        if (deferred)
        {
            fixture.ScheduleDeferredCleanup(Task.CompletedTask, purgeSegments);
            fixture.ReleaseExportLock();
            await fixture.WaitForCleanupAsync();
        }
        else
        {
            await fixture.DisposeBackendAsync(purgeSegments, CancellationToken.None)
                .WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, fixture.ExportOperationLock.CurrentCount);
            fixture.ReleaseExportLock();
        }

        Assert.False(fixture.HasAnyResource);
        Assert.True(fixture.BufferDisposed);
        Assert.True(fixture.ExporterDisposed);
        Assert.Equal("retained segment", File.ReadAllText(fixture.SegmentPath));
        Assert.True(File.Exists(fixture.RecoveryMarkerPath));
        Assert.False(File.Exists(fixture.RetiredMarkerPath));
        Assert.Equal(1, fixture.ExportOperationLock.CurrentCount);
    }

    [Fact]
    public async Task FailedExportLockAcquisitionLeavesArtifactsUntouched()
    {
        await using var fixture = new CleanupFixture();
        fixture.DisposeExportLock();

        var cleanup = fixture.CleanupArtifactsAfterExportAsync(purgeSegments: true);

        Assert.False(await cleanup.WaitAsync(TimeSpan.FromSeconds(5)));
        fixture.AssertArtifactsUntouched();
    }

    private sealed class CleanupFixture : IAsyncDisposable
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sussudio-backend-cleanup-" + Guid.NewGuid().ToString("N"));
        private readonly object _backend;
        private readonly object _buffer;
        private readonly object _exporter;
        private readonly List<TaskCompletionSource> _sinkCompletions = [];
        private bool _exportLockHeld = true;
        private bool _exportLockDisposed;
        private bool _cleanupScheduled;
        private Task? _cleanupTask;

        public CleanupFixture()
        {
            ExportOperationLock.Wait();
            var options = Activator.CreateInstance(TypeOf("Sussudio.Models.FlashbackBufferOptions"))!;
            options.GetType().GetProperty("TempDirectory")!.SetValue(options, _root);
            _buffer = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackBufferManager"), options)!;
            Invoke(_buffer, "Initialize", "owned-cleanup");
            SessionDirectory = (string)Read(_buffer, "SessionDirectory")!;
            SegmentPath = (string)_buffer.GetType().GetMethod("AcquireSegmentPath", Type.EmptyTypes)!.Invoke(_buffer, null)!;
            File.WriteAllText(SegmentPath, "retained segment");
            _exporter = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackExporter"))!;
            _backend = Activator.CreateInstance(
                TypeOf("Sussudio.Services.Capture.FlashbackBackendResources"), ExportOperationLock)!;
            Invoke(_backend, "Install", _buffer, null, _exporter, null, null);
        }

        public SemaphoreSlim ExportOperationLock { get; } = new(1, 1);
        public string SessionDirectory { get; }
        public string SegmentPath { get; }
        public string RetiredMarkerPath => Path.Combine(SessionDirectory, ".flashback-retired-session");
        public string RecoveryMarkerPath => Path.Combine(SessionDirectory, ".flashback-recovery-preserve");
        public bool HasAnyResource => (bool)Read(_backend, "HasAnyResource")!;
        public bool BufferDisposed => ReadDisposed(_buffer);
        public bool ExporterDisposed => ReadDisposed(_exporter);

        public Task DisposeBackendAsync(bool purgeSegments, CancellationToken cancellationToken)
        {
            var request = Activator.CreateInstance(
                TypeOf("Sussudio.Services.Capture.FlashbackPreviewBackendDisposalRequest"),
                new object?[]
                {
                    null, null, null,
                    new EventHandler<long>((_, _) => { }),
                    purgeSegments, true, cancellationToken
                })!;
            _cleanupScheduled = purgeSegments && cancellationToken.IsCancellationRequested;
            return (Task)Invoke(_backend, "DisposePreviewBackendUnderExportLockAsync", request)!;
        }

        public TaskCompletionSource CreateSinkCompletion()
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _sinkCompletions.Add(completion);
            return completion;
        }

        public void InstallDisposedSinkWithPendingCompletion(Task sinkCompletion)
        {
            // Seed the completion boundary after disposal has returned; this
            // fixture starts no encoder and does not simulate a native drain.
            var sink = Activator.CreateInstance(
                TypeOf("Sussudio.Services.Flashback.FlashbackEncoderSink"), _buffer)!;
            ((IDisposable)sink).Dispose();
            sink.GetType().GetField("_encodingTask", InstanceFlags)!.SetValue(sink, sinkCompletion);
            Invoke(_backend, "Install", _buffer, sink, _exporter, null, null);
        }

        public void ScheduleDeferredCleanup(Task sinkCompletion, bool purgeSegments)
        {
            var request = DetachCleanupRequest(purgeSegments);
            _cleanupScheduled = true;
            Invoke(_backend, "ScheduleDeferredArtifactCleanup", sinkCompletion, request, 0);
        }

        public Task<bool> CleanupArtifactsAfterExportAsync(bool purgeSegments)
        {
            var request = DetachCleanupRequest(purgeSegments);
            var cleanup = (Task<bool>)Invoke(_backend, "CleanupArtifactsAfterExportAsync", request, "deferred")!;
            _cleanupTask = cleanup;
            return cleanup;
        }

        private object DetachCleanupRequest(bool purgeSegments)
        {
            var request = Activator.CreateInstance(
                TypeOf("Sussudio.Services.Capture.FlashbackBackendArtifactCleanupRequest"),
                _buffer, _exporter, "owned-cleanup", purgeSegments)!;
            Invoke(_backend, "Clear");
            return request;
        }

        public void PreserveRecoverySegments() => Invoke(_backend, "PreserveRecoverySegments", "test_failed_finalize");

        public void ReleaseExportLock()
        {
            ExportOperationLock.Release();
            _exportLockHeld = false;
        }

        public void DisposeExportLock()
        {
            ExportOperationLock.Dispose();
            _exportLockDisposed = true;
            _exportLockHeld = false;
        }

        public void AssertArtifactsUntouched()
        {
            Assert.False(BufferDisposed);
            Assert.False(ExporterDisposed);
            Assert.Equal("retained segment", File.ReadAllText(SegmentPath));
            Assert.False(File.Exists(RetiredMarkerPath));
        }

        public async Task AssertCleanupRemainsBlockedAsync()
        {
            // The scheduler intentionally returns no task; give its worker a
            // bounded opportunity to run before observing the blocked artifacts.
            await Task.Delay(100);
            AssertArtifactsUntouched();
        }

        public async Task WaitForCleanupAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (!BufferDisposed || !ExporterDisposed || ExportOperationLock.CurrentCount != 1)
            {
                await Task.Delay(10, timeout.Token);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                foreach (var completion in _sinkCompletions)
                {
                    completion.TrySetResult();
                }
                if (_exportLockHeld && !_exportLockDisposed)
                {
                    if (ExportOperationLock.CurrentCount == 0)
                    {
                        ReleaseExportLock();
                    }
                    _exportLockHeld = false;
                }
                if (_cleanupTask != null)
                {
                    await _cleanupTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                if (_cleanupScheduled)
                {
                    await WaitForCleanupAsync();
                }
            }
            finally
            {
                ((IDisposable)_exporter).Dispose();
                ((IDisposable)_buffer).Dispose();
                ExportOperationLock.Dispose();
                if (Directory.Exists(_root))
                {
                    Directory.Delete(_root, recursive: true);
                }
            }
        }

        private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
        private static object? Invoke(object target, string name, params object?[] arguments)
            => target.GetType().GetMethod(name, InstanceFlags)!.Invoke(target, arguments);
        private static object? Read(object target, string name)
            => target.GetType().GetProperty(name, InstanceFlags)!.GetValue(target);
        private static bool ReadDisposed(object target)
            => (bool)target.GetType().GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    }
}
