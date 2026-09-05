using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackFailureIdentityTests
{
    [Theory]
    [InlineData("", "cancelled in point timeout output path", false, "Failed")]
    [InlineData("future-failure", "cancelled", false, "Failed")]
    [InlineData("flashback-export-input-read-failed", "permission denied cancelled", false, "InputReadFailed")]
    [InlineData("flashback-export-cancelled", "localized status", false, "Cancelled")]
    [InlineData("flashback-export-cancelled", "cancelled", true, "")]
    public void ClassificationUsesFailureIdentityAndSuccess(string code, string message, bool succeeded, string expected)
    {
        var result = Activator.CreateInstance(RequireType("Sussudio.Services.Contracts.FinalizeResult"))!;
        Set(result, "FailureCode", code);
        Set(result, "StatusMessage", message);
        Set(result, "Succeeded", succeeded);
        Assert.Equal(expected, Classify(result));
    }

    [Fact]
    public void NativeFailureIdentitySurvivesAnExceptionWrapper()
    {
        var nativeException = (Exception)Activator.CreateInstance(
            RequireType("Sussudio.Services.Flashback.FlashbackExportException"),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { "cancelled input path", "flashback-export-output-write-failed" }, null)!;
        var fromException = RequireType("Sussudio.Services.Flashback.FlashbackExportFailureCodes")
            .GetMethod("FromException", BindingFlags.Static | BindingFlags.NonPublic)!;

        Assert.Equal("flashback-export-output-write-failed",
            fromException.Invoke(null, new object[] { new InvalidOperationException("wrapper", nativeException) }));
        Assert.Equal("flashback-export-failed",
            fromException.Invoke(null, new object[] { new IOException("cancelled output path") }));
    }

    [Fact]
    public void PreCancelledExportCarriesCancellationIdentity()
    {
        using var fixture = new ExportFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var result = fixture.Export(Path.Combine(fixture.DirectoryPath, "missing.ts"), cancellation.Token);
        Assert.Equal("Cancelled", Classify(result));
        Assert.False(File.Exists(fixture.OutputPath));
    }

    [Theory]
    [InlineData("cancelled.ts")]
    [InlineData("in point.ts")]
    [InlineData("output path.ts")]
    [InlineData("disposed.ts")]
    [InlineData("timeout.ts")]
    public void MissingInputCategoryDoesNotDependOnTheFilename(string filename)
    {
        using var fixture = new ExportFixture();
        var result = fixture.Export(Path.Combine(fixture.DirectoryPath, filename));

        Assert.Equal("InputUnavailable", Classify(result));
        Assert.Contains(filename, Get<string>(result, "StatusMessage"));
        Assert.False(File.Exists(fixture.OutputPath));
    }

    [Fact]
    public void ExistingDestinationContainingCancelIsAnOutputFailure()
    {
        using var fixture = new ExportFixture();
        var input = Path.Combine(fixture.DirectoryPath, "input.ts");
        File.WriteAllText(input, "input remains untouched");
        File.WriteAllText(fixture.OutputPath, "destination remains untouched");

        var result = fixture.Export(input);

        Assert.Equal("InvalidOutputPath", Classify(result));
        Assert.Equal("input remains untouched", File.ReadAllText(input));
        Assert.Equal("destination remains untouched", File.ReadAllText(fixture.OutputPath));
    }

    [Fact]
    public void PreservingEndArtifactsRetainsFailureIdentityAndEvidence()
    {
        var type = RequireType("Sussudio.Services.Contracts.FinalizeResult");
        var export = Activator.CreateInstance(type)!;
        Set(export, "StatusMessage", "Export failed.");
        Set(export, "FailureCode", "specific-failure");
        Set(export, "CleanupPending", true);
        Set(export, "RecoveryPath", "recovery.ts");
        Set(export, "VerificationCompleted", true);
        Set(export, "FinalizationElapsedMs", 123L);
        Set(export, "PreservedArtifacts", new[] { "export.ts" });
        Set(export, "RequestedTracks", new[] { "video", "audio" });
        Set(export, "ObservedTracks", new[] { "video" });
        var end = Activator.CreateInstance(type)!;
        Set(end, "PreservedArtifacts", new[] { "end.ts" });

        var method = RequireType("Sussudio.Services.Flashback.FlashbackBackendResources")
            .GetMethod("PreserveEndArtifactsOnFailure", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = method.Invoke(null, new[] { export, end })!;

        Assert.Equal("specific-failure", Get<string>(result, "FailureCode"));
        Assert.True(Get<bool>(result, "CleanupPending"));
        Assert.Equal("recovery.ts", Get<string>(result, "RecoveryPath"));
        Assert.True(Get<bool>(result, "VerificationCompleted"));
        Assert.Equal(123L, Get<long>(result, "FinalizationElapsedMs"));
        Assert.Equal(new[] { "export.ts", "end.ts" }, Get<IEnumerable<string>>(result, "PreservedArtifacts"));
        Assert.Equal(new[] { "video", "audio" }, Get<IEnumerable<string>>(result, "RequestedTracks"));
        Assert.Equal(new[] { "video" }, Get<IEnumerable<string>>(result, "ObservedTracks"));
    }

    private static string Classify(object result)
        => (string)RequireType("Sussudio.Services.Capture.CaptureService")
            .GetMethod("ClassifyFlashbackExportFailureKind", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { result })!;

    private static Type RequireType(string name)
        => SussudioAssembly.Load().GetType(name, throwOnError: true)!;

    private static T Get<T>(object value, string property)
        => (T)value.GetType().GetProperty(property)!.GetValue(value)!;

    private static void Set(object value, string property, object propertyValue)
        => value.GetType().GetProperty(property)!.SetValue(value, propertyValue);

    private sealed class ExportFixture : IDisposable
    {
        private readonly object _exporter = Activator.CreateInstance(
            RequireType("Sussudio.Services.Flashback.FlashbackExporter"))!;

        public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "flashback-identity-" + Guid.NewGuid().ToString("N"));
        public string OutputPath => Path.Combine(DirectoryPath, "cancelled-output.mp4");

        public ExportFixture() => Directory.CreateDirectory(DirectoryPath);

        public object Export(string input, CancellationToken cancellationToken = default)
            => _exporter.GetType().GetMethod("ExportCore", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_exporter, new object?[]
                {
                    input, TimeSpan.Zero, TimeSpan.FromSeconds(1), OutputPath,
                    true, false, null, cancellationToken
                })!;

        public void Dispose()
        {
            ((IDisposable)_exporter).Dispose();
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
