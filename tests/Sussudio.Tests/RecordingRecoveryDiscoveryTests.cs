using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class RecordingRecoveryDiscoveryTests : IDisposable
{
    private static readonly DateTime MarkerTime = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _directory;
    private readonly string _outputDirectory;
    private readonly string _stableDirectory;
    private readonly List<string> _diagnostics = new();

    public RecordingRecoveryDiscoveryTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        _directory = Path.Combine(Path.GetTempPath(), "SussudioRecoveryDiscovery_" + Guid.NewGuid().ToString("N"));
        _outputDirectory = Directory.CreateDirectory(Path.Combine(_directory, "output")).FullName;
        _stableDirectory = Directory.CreateDirectory(Path.Combine(_directory, "stable")).FullName;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void RootFailure_DoesNotHideRecoveryFromTheOtherRoot(bool failOutputRoot, bool accessDenied)
    {
        var failedDirectory = failOutputRoot ? _outputDirectory : _stableDirectory;
        var healthyDirectory = failOutputRoot ? _stableDirectory : _outputDirectory;
        var expectedMarker = WriteMarker(healthyDirectory, "recoverable", MarkerTime);
        var failure = CreateIoFailure(accessDenied);
        var attemptedRoots = new List<string>();

        var recovered = Load(directory =>
        {
            attemptedRoots.Add(directory);
            return directory == failedDirectory
                ? throw failure
                : EnumerateMarkers(directory);
        });

        AssertMarker(expectedMarker, recovered);
        Assert.Equal(new[] { _outputDirectory, _stableDirectory }, attemptedRoots);
        Assert.Equal(
            $"Failed to enumerate recording recovery markers in '{failedDirectory}': {failure.Message}",
            Assert.Single(_diagnostics));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LazyEnumerationFailure_RetainsCandidatesAlreadyYielded(bool failOutputRoot)
    {
        var failedDirectory = failOutputRoot ? _outputDirectory : _stableDirectory;
        var healthyDirectory = failOutputRoot ? _stableDirectory : _outputDirectory;
        WriteMarker(healthyDirectory, "older", MarkerTime);
        var expectedMarker = WriteMarker(failedDirectory, "newer", MarkerTime.AddMinutes(1));
        var attemptedRoots = new List<string>();

        var recovered = Load(directory =>
        {
            attemptedRoots.Add(directory);
            return directory == failedDirectory
                ? YieldMarkerThenFail(expectedMarker)
                : EnumerateMarkers(directory);
        });

        AssertMarker(expectedMarker, recovered);
        Assert.Equal(new[] { _outputDirectory, _stableDirectory }, attemptedRoots);
        Assert.Equal(
            $"Failed to enumerate recording recovery markers in '{failedDirectory}': enumeration interrupted",
            Assert.Single(_diagnostics));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TimestampFailure_SkipsOnlyTheFailedMarker(bool accessDenied)
    {
        var failedMarker = WriteMarker(_outputDirectory, "unreadable-timestamp", MarkerTime.AddMinutes(2));
        var expectedMarker = WriteMarker(_outputDirectory, "next-marker", MarkerTime.AddMinutes(1));
        var stableMarker = WriteMarker(_stableDirectory, "stable-older", MarkerTime);
        var failure = CreateIoFailure(accessDenied);
        var attemptedMarkers = new List<string>();

        var recovered = Load(
            directory => directory == _outputDirectory
                ? new[] { failedMarker, expectedMarker }
                : new[] { stableMarker },
            marker =>
            {
                attemptedMarkers.Add(marker);
                return marker == failedMarker ? throw failure : File.GetLastWriteTimeUtc(marker);
            });

        AssertMarker(expectedMarker, recovered);
        Assert.Equal(new[] { failedMarker, expectedMarker, stableMarker }, attemptedMarkers);
        Assert.Equal(
            $"Failed to read recording recovery marker timestamp '{failedMarker}': {failure.Message}",
            Assert.Single(_diagnostics));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Selection_PrefersNewestMediaThenNewestMetadataOnly(bool mediaAvailable)
    {
        var oldest = WriteMarker(_outputDirectory, "oldest", MarkerTime, mediaAvailable);
        var newestMedia = WriteMarker(_stableDirectory, "newest-media", MarkerTime.AddMinutes(1), mediaAvailable);
        var newestMetadata = WriteMarker(_outputDirectory, "newest-metadata", MarkerTime.AddMinutes(2), writeMedia: false);

        var recovered = Load(directory => directory == _outputDirectory
            ? new[] { oldest, newestMetadata }
            : new[] { newestMedia });

        AssertMarker(mediaAvailable ? newestMedia : newestMetadata, recovered);
        Assert.Empty(_diagnostics);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnexpectedDiscoveryFailure_IsNotHandledAsAnIoFailure(bool failTimestamp)
    {
        WriteMarker(_outputDirectory, "recording", MarkerTime);
        var failure = new InvalidOperationException("unexpected discovery failure");

        var thrown = Assert.Throws<TargetInvocationException>(() => Load(
            directory => failTimestamp ? EnumerateMarkers(directory) : throw failure,
            marker => failTimestamp ? throw failure : File.GetLastWriteTimeUtc(marker)));

        Assert.Same(failure, thrown.InnerException);
        Assert.Empty(_diagnostics);
    }

    private object? Load(
        Func<string, IEnumerable<string>> enumerateMarkerPaths,
        Func<string, DateTime>? getLastWriteTimeUtc = null)
    {
        var recoveryType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Recording.RecordingFinalizationRecoveryArtifacts",
            throwOnError: true)!;
        var restore = recoveryType.GetMethod(
            "TryLoadLatestFromDirectories",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        Action<string> log = _diagnostics.Add;
        return restore.Invoke(null, new object?[]
        {
            _outputDirectory,
            _stableDirectory,
            enumerateMarkerPaths,
            getLastWriteTimeUtc ?? File.GetLastWriteTimeUtc,
            log
        });
    }

    private static IEnumerable<string> EnumerateMarkers(string directory)
        => Directory.EnumerateFiles(directory, "*.recording-*.txt", SearchOption.TopDirectoryOnly);

    private static IEnumerable<string> YieldMarkerThenFail(string marker)
    {
        yield return marker;
        throw new IOException("enumeration interrupted");
    }

    private static Exception CreateIoFailure(bool accessDenied)
        => accessDenied
            ? new UnauthorizedAccessException("access denied")
            : new IOException("storage unavailable");

    private static string WriteMarker(string directory, string name, DateTime writeUtc, bool writeMedia = true)
    {
        var outputPath = Path.Combine(directory, name + ".mp4");
        if (writeMedia)
        {
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });
        }

        var markerPath = outputPath + ".recording-finalization-unresolved.txt";
        File.WriteAllLines(markerPath, new[]
        {
            "status=unresolved",
            "utc=" + writeUtc.ToString("O"),
            "reason=Interrupted recording.",
            "final_output=" + outputPath
        });
        File.SetLastWriteTimeUtc(markerPath, writeUtc);
        return markerPath;
    }

    private static void AssertMarker(string expectedMarker, object? recovered)
    {
        Assert.NotNull(recovered);
        Assert.Equal(expectedMarker, recovered.GetType().GetProperty("MarkerPath")!.GetValue(recovered));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
