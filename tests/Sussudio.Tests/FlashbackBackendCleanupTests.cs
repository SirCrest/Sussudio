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

        // The backend has returned, so this callback is the only owner of the
        // detached resources. Hold the export lock until that handoff is proven.
        await fixture.CleanupRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(fixture.BufferDisposed);
        Assert.False(fixture.ExporterDisposed);
        Assert.Equal("retained segment", File.ReadAllText(fixture.SegmentPath));

        fixture.AllowCleanup.TrySetResult(true);
        await fixture.CleanupReleased.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.BufferDisposed);
        Assert.True(fixture.ExporterDisposed);
        Assert.Equal("retained segment", File.ReadAllText(fixture.SegmentPath));
        Assert.True(File.Exists(Path.Combine(fixture.SessionDirectory, ".flashback-retired-session")));
        Assert.Equal(1, fixture.AcquireCount);
        Assert.Equal(1, fixture.ReleaseCount);
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
        Assert.Equal(0, fixture.AcquireCount);
        Assert.Equal(0, fixture.ReleaseCount);
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
        Assert.True(File.Exists(Path.Combine(fixture.SessionDirectory, ".flashback-retired-session")));
        Assert.Equal(0, fixture.AcquireCount);
        Assert.Equal(0, fixture.ReleaseCount);
    }

    private sealed class CleanupFixture : IAsyncDisposable
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sussudio-backend-cleanup-" + Guid.NewGuid().ToString("N"));
        private readonly object _backend;
        private readonly object _buffer;
        private readonly object _exporter;
        private int _acquireCount;
        private int _releaseCount;

        public CleanupFixture()
        {
            var options = Activator.CreateInstance(TypeOf("Sussudio.Models.FlashbackBufferOptions"))!;
            options.GetType().GetProperty("TempDirectory")!.SetValue(options, _root);
            _buffer = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackBufferManager"), options)!;
            Invoke(_buffer, "Initialize", "owned-cleanup");
            SessionDirectory = (string)Read(_buffer, "SessionDirectory")!;
            SegmentPath = (string)_buffer.GetType().GetMethod("AcquireSegmentPath", Type.EmptyTypes)!.Invoke(_buffer, null)!;
            File.WriteAllText(SegmentPath, "retained segment");
            _exporter = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackExporter"))!;
            _backend = Activator.CreateInstance(TypeOf("Sussudio.Services.Capture.FlashbackBackendResources"))!;
            Invoke(_backend, "Install", _buffer, null, _exporter, null, null);
        }

        public TaskCompletionSource<bool> AllowCleanup { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CleanupRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CleanupReleased { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string SessionDirectory { get; }
        public string SegmentPath { get; }
        public bool HasAnyResource => (bool)Read(_backend, "HasAnyResource")!;
        public bool BufferDisposed => ReadDisposed(_buffer);
        public bool ExporterDisposed => ReadDisposed(_exporter);
        public int AcquireCount => Volatile.Read(ref _acquireCount);
        public int ReleaseCount => Volatile.Read(ref _releaseCount);

        public Task DisposeBackendAsync(bool purgeSegments, CancellationToken cancellationToken)
        {
            var request = Activator.CreateInstance(
                TypeOf("Sussudio.Services.Capture.FlashbackPreviewBackendDisposalRequest"),
                new object?[]
                {
                    null, null, null,
                    new EventHandler<long>((_, _) => { }),
                    new Func<Task<bool>>(AcquireCleanupLockAsync),
                    new Action<string>(_ =>
                    {
                        Interlocked.Increment(ref _releaseCount);
                        CleanupReleased.TrySetResult();
                    }),
                    purgeSegments, true, true, cancellationToken
                })!;
            return (Task)Invoke(_backend, "DisposePreviewBackendAsync", request)!;
        }

        private Task<bool> AcquireCleanupLockAsync()
        {
            Interlocked.Increment(ref _acquireCount);
            CleanupRequested.TrySetResult();
            return AllowCleanup.Task;
        }

        public async ValueTask DisposeAsync()
        {
            AllowCleanup.TrySetResult(true);
            if (CleanupRequested.Task.IsCompleted)
            {
                await CleanupReleased.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            ((IDisposable)_exporter).Dispose();
            ((IDisposable)_buffer).Dispose();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
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
