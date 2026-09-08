using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace Sussudio.Tests;

[Collection(RecoveryEnvironmentCollection.Name)]
public sealed class LoggerTests
{
    [Fact]
    public async Task ShutdownDrainsQueuedMessagesAndRespectsVerboseSetting()
    {
        await using var logger = new IsolatedLogger();
        logger.Type.GetProperty("VerboseEnabled")!.SetValue(null, false);
        logger.Call("LogVerbose", "hidden-entry", "test");
        logger.Call("Log", "queued-first", "test");
        logger.Type.GetProperty("VerboseEnabled")!.SetValue(null, true);
        logger.Call("LogVerbose", "visible-entry", "test");
        logger.Call("Log", "queued-last", "test");

        await logger.Shutdown();

        var content = File.ReadAllText(logger.FilePath);
        Assert.DoesNotContain("hidden-entry", content);
        Assert.Contains("visible-entry", content);
        Assert.True(content.IndexOf("queued-first", StringComparison.Ordinal) < content.IndexOf("queued-last", StringComparison.Ordinal));
        Assert.Contains("queued-first", content);
        Assert.Contains("queued-last", content);
    }

    [Fact]
    public async Task SaturatedQueueDropsWithoutBlocking()
    {
        await using var logger = new IsolatedLogger();
        var writeGate = logger.Type.GetField("LockObject", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        using var releaseGate = new ManualResetEventSlim();
        var gateHeld = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // A dedicated thread holds the file gate, never the xUnit synchronization context.
        var gateHolder = Task.Factory.StartNew(() =>
        {
            lock (writeGate)
            {
                gateHeld.SetResult();
                releaseGate.Wait();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await gateHeld.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Run(() =>
            {
                for (var i = 0; i < 9000; i++)
                    logger.Call("Log", $"queued-{i}", "test");
            }).WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            releaseGate.Set();
            await gateHolder.WaitAsync(TimeSpan.FromSeconds(10));
        }

        await logger.Shutdown();

        var dropped = (long)logger.Type.GetProperty("DroppedMessageCount", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        Assert.True(dropped > 0);
        var content = File.ReadAllText(logger.FilePath);
        Assert.Contains("log channel saturated", content);
        var written = content.Split('\n').Count(line => line.Contains("[test] queued-", StringComparison.Ordinal));
        Assert.Equal(9000L, written + dropped);
    }

    [Fact]
    public async Task FatalBreadcrumbRemainsAvailableAfterQueueShutdown()
    {
        await using var logger = new IsolatedLogger();
        await logger.Shutdown();

        logger.Call("LogFatalBreadcrumb", "fatal-after-shutdown", new IOException("fatal detail"));

        var content = File.ReadAllText(logger.FilePath);
        Assert.Contains("fatal-after-shutdown", content);
        Assert.Contains("fatal detail", content);
    }

    [Fact]
    public async Task InitializationRotatesExistingLogBeforeStartingNewLog()
    {
        await using var logger = new IsolatedLogger(path => File.WriteAllText(path, "prior-session"));
        logger.Call("Log", "new-session", "test");
        await logger.Shutdown();

        var rotated = Assert.Single(Directory.GetFiles(logger.DirectoryPath, "Sussudio_Debug_*.log"));
        Assert.Equal("prior-session", File.ReadAllText(rotated));
        var current = File.ReadAllText(logger.FilePath);
        Assert.DoesNotContain("prior-session", current);
        Assert.Contains("new-session", current);
        Assert.Equal("Healthy", logger.Type.GetProperty("InitState")!.GetValue(null)!.ToString());
    }

    [Fact]
    public async Task FileFailureIsReportedWithoutFailingProducersOrShutdown()
    {
        await using var logger = new IsolatedLogger(path => Directory.CreateDirectory(path));

        Assert.Equal("FileIoFailed", logger.Type.GetProperty("InitState")!.GetValue(null)!.ToString());
        logger.Call("Log", "unwritable-log", "test");
        logger.Call("LogFatalBreadcrumb", "unwritable-fatal", null);
        await logger.Shutdown();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectoryResolutionFailureReturnsUnavailablePath(bool accessDenied)
    {
        await using var logger = new IsolatedLogger();
        var resolve = logger.Type.GetMethod("TryResolveLogFilePath", BindingFlags.Static | BindingFlags.NonPublic)!;
        Exception failure = accessDenied
            ? new UnauthorizedAccessException("log directory unavailable")
            : new IOException("log directory unavailable");
        Func<string> failedResolution = () => throw failure;

        var path = Assert.IsType<string>(resolve.Invoke(null, new object[] { failedResolution }));

        Assert.Empty(path);
        logger.Call("Log", "producer-remains-usable", "test");
        logger.Call("LogFatalBreadcrumb", "fatal-remains-usable", null);
        await logger.Shutdown();
    }

    [Fact]
    public void DirectoryResolutionRunsInsideLoggerInitialization()
    {
        var source = RuntimeContractSource.ReadRepoFile("Sussudio/AppRuntime.cs");
        Assert.Contains("private static readonly string LogFilePath;", source, StringComparison.Ordinal);
        Assert.Contains("LogFilePath = TryResolveLogFilePath(() => RuntimePaths.GetRepoLogFile(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private static readonly string LogFilePath = RuntimePaths.", source, StringComparison.Ordinal);
        Assert.Contains("var fileIoOk = !string.IsNullOrEmpty(LogFilePath);", source, StringComparison.Ordinal);
    }


    private sealed class IsolatedLogger : IAsyncDisposable
    {
        private readonly LoggerLoadContext _context;
        public Type Type { get; }
        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "Sussudio-logger-tests", Guid.NewGuid().ToString("N"));
        public string FilePath => Path.Combine(DirectoryPath, "Sussudio_Debug.log");

        public IsolatedLogger(Action<string>? prepare = null)
        {
            Directory.CreateDirectory(DirectoryPath);
            prepare?.Invoke(FilePath);
            var appPath = SussudioAssembly.Load().Location;
            _context = new LoggerLoadContext(appPath);
            var previous = Environment.GetEnvironmentVariable("SUSSUDIO_LOG_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("SUSSUDIO_LOG_ROOT", DirectoryPath);
                Type = _context.LoadFromAssemblyPath(appPath).GetType("Sussudio.Logger", throwOnError: true)!;
                Assert.Equal(FilePath, (string)Call("GetLogFilePath")!);
            }
            finally
            {
                Environment.SetEnvironmentVariable("SUSSUDIO_LOG_ROOT", previous);
            }
        }

        public object? Call(string method, params object?[] args)
            => Type.GetMethod(method, BindingFlags.Public | BindingFlags.Static)!.Invoke(null, args);

        public async Task Shutdown()
        {
            await ((Task)Call("ShutdownAsync", TimeSpan.FromSeconds(10))!).WaitAsync(TimeSpan.FromSeconds(15));
            var writer = (Task)Type.GetField("LogWriterTask", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            Assert.True(writer.IsCompletedSuccessfully, "Shutdown did not finish the isolated writer.");
        }

        public async ValueTask DisposeAsync()
        {
            await Shutdown();
            _context.Unload();
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    private sealed class LoggerLoadContext(string appPath) : AssemblyLoadContext(isCollectible: true)
    {
        private readonly AssemblyDependencyResolver _resolver = new(appPath);
        protected override Assembly? Load(AssemblyName name)
        {
            var path = _resolver.ResolveAssemblyToPath(name);
            return path == null ? null : LoadFromAssemblyPath(path);
        }
    }
}
