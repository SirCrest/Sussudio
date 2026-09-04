using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackFailureTests
{
    [Theory]
    [InlineData("cancelled-take.ts")]
    [InlineData("disposed-camera.ts")]
    [InlineData("timeout-no-packets.ts")]
    public async Task MissingInputKeepsItsKindThroughRejectedAndCompletedDiagnostics(string fileName)
    {
        using var fixture = new ExportFixture();
        var result = await fixture.ExportAsync(fileName);

        AssertFailure(result, "flashback-export-input-unavailable", "InputUnavailable");
        Assert.Contains(fileName, Get<string>(result, "StatusMessage"));
        AssertDiagnostics(result, "Failed", "InputUnavailable");
        Assert.False(File.Exists(fixture.OutputPath));
    }

    [Fact]
    public async Task CallerCancellationIsStructuredEvenWhenMessageIsReworded()
    {
        using var fixture = new ExportFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var result = await fixture.ExportAsync("missing.ts", cancellation.Token);

        AssertFailure(result, "flashback-export-cancelled", "Cancelled");
        Set(result, "StatusMessage", "The caller stopped this export.");
        Assert.True(IsCancelled(result));
        AssertDiagnostics(result, "Cancelled", "Cancelled");
        Assert.False(File.Exists(fixture.OutputPath));
    }

    [Fact]
    public async Task ExistingDestinationContainingCancelledIsAnOutputPathFailure()
    {
        using var fixture = new ExportFixture("cancelled-destination.mp4");
        File.WriteAllBytes(Path.Combine(fixture.DirectoryPath, "input.ts"), new byte[] { 1 });
        var existingBytes = new byte[] { 2, 3, 4 };
        File.WriteAllBytes(fixture.OutputPath, existingBytes);

        var result = await fixture.ExportAsync("input.ts");

        AssertFailure(result, "flashback-export-invalid-output-path", "InvalidOutputPath");
        Assert.Equal(existingBytes, File.ReadAllBytes(fixture.OutputPath));
    }

    [Fact]
    public async Task DisposedExporterIsDistinctFromCancellation()
    {
        using var fixture = new ExportFixture("cancelled-output.mp4");
        ((IDisposable)fixture.Exporter).Dispose();

        var result = await fixture.ExportAsync("cancelled-input.ts");

        AssertFailure(result, "flashback-export-disposed", "Disposed");
        Assert.False(IsCancelled(result));
    }

    [Fact]
    public void MetadataCopiesPreserveCodeAndAnIntegrityFailureReplacesCancellation()
    {
        var result = CreateFailure("flashback-export-cancelled", "The caller stopped this export.");
        Set(result, "VerificationCompleted", true);
        Set(result, "CleanupPending", true);
        Set(result, "RecoveryPath", "recovery.ts");
        Set(result, "FinalizationElapsedMs", 1234L);
        result = InvokeInstance(result, "WithTrackEvidence", new[] { "video", "program" }, new[] { "video" });

        var endResult = CreateFailure("recording-finalization-failed", "Retained recording segments.");
        Set(endResult, "PreservedArtifacts", new[] { "segment.ts" });
        var preserved = InvokeStatic(
            TypeOf("Sussudio.Services.Flashback.FlashbackBackendResources"),
            "PreserveEndArtifactsOnFailure",
            result,
            endResult);

        AssertFailure(preserved, "flashback-export-cancelled", "Cancelled");
        Assert.Equal(new[] { "segment.ts" }, Get<IEnumerable<string>>(preserved, "PreservedArtifacts"));
        AssertMetadata(preserved);

        var integrityFailure = InvokeInstance(preserved, "AsFailure", "Requested audio was missing.", "recording-program-audio-missing");
        AssertFailure(integrityFailure, "recording-program-audio-missing", "Failed");
        Assert.False(IsCancelled(integrityFailure));
        AssertMetadata(integrityFailure);
        AssertDiagnostics(integrityFailure, "Failed", "Failed");
    }

    [Fact]
    public void SuccessfulResultDoesNotExposeStaleFailureCode()
    {
        var result = CreateFailure("flashback-export-cancelled", "Export complete.");
        Set(result, "Succeeded", true);

        Assert.Equal(string.Empty, GetKind(result));
        Assert.False(IsCancelled(result));
    }

    [Fact]
    public void NativeFailureCodeIsIndependentOfExceptionText()
    {
        var exceptionType = TypeOf("Sussudio.Services.Flashback.FlashbackExportException");
        var exception = Activator.CreateInstance(
            exceptionType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { "cancelled input, permission denied", "flashback-export-input-read-failed" },
            culture: null)!;

        Assert.Equal("flashback-export-input-read-failed", InvokeStatic(FailureType, "GetExceptionCode", exception));
        Assert.Equal("flashback-export-failed", InvokeStatic(FailureType, "GetExceptionCode", new InvalidOperationException("cancelled, disposed, timeout")));
    }

    [Theory]
    [InlineData(false, "flashback-export-no-media-written")]
    [InlineData(true, "flashback-export-invalid-output-path")]
    public void OutputPublicationReportsTheActualFailure(bool destinationAppears, string expectedCode)
    {
        using var fixture = new ExportFixture("cancelled-final.mp4");
        var transactionType = TypeOf("Sussudio.Services.Flashback.FlashbackExportOutputTransaction");
        var reserveArguments = new object?[] { fixture.OutputPath, null, null, null };
        Assert.True((bool)InvokeStatic(transactionType, "TryReserve", reserveArguments));
        using var transaction = (IDisposable)reserveArguments[1]!;
        var temporaryPath = Get<string>(transaction, "TemporaryPath");

        if (destinationAppears)
        {
            using (var stream = new FileStream(temporaryPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
            {
                stream.WriteByte(1);
            }
            File.WriteAllBytes(fixture.OutputPath, new byte[] { 7, 8 });
        }

        var publishArguments = new object?[] { fixture.OutputPath, 0L, null, null };
        Assert.False((bool)InvokeInstance(transaction, "TryPublish", publishArguments));
        Assert.Equal(expectedCode, publishArguments[3]);
        Assert.False(File.Exists(temporaryPath));
        if (destinationAppears)
        {
            Assert.Equal(new byte[] { 7, 8 }, File.ReadAllBytes(fixture.OutputPath));
        }
        else
        {
            Assert.False(File.Exists(fixture.OutputPath));
        }
    }

    private static void AssertMetadata(object result)
    {
        Assert.True(Get<bool>(result, "VerificationCompleted"));
        Assert.True(Get<bool>(result, "CleanupPending"));
        Assert.Equal("recovery.ts", Get<string>(result, "RecoveryPath"));
        Assert.Equal(1234L, Get<long>(result, "FinalizationElapsedMs"));
        Assert.Equal(new[] { "video", "program" }, Get<IEnumerable<string>>(result, "RequestedTracks"));
        Assert.Equal(new[] { "video" }, Get<IEnumerable<string>>(result, "ObservedTracks"));
    }

    private static void AssertDiagnostics(object result, string expectedStatus, string expectedKind)
    {
        var captureType = TypeOf("Sussudio.Services.Capture.CaptureService");
        var service = RuntimeHelpers.GetUninitializedObject(captureType);
        GC.SuppressFinalize(service);
        captureType.GetField("_flashbackExportDiagnosticsLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(service, new object());

        InvokeInstance(service, "RecordRejectedFlashbackExportDiagnostics", "output.mp4", result, null, null);
        AssertFields();

        var exportId = InvokeInstance(service, "BeginFlashbackExportDiagnostics", TimeSpan.Zero, TimeSpan.FromSeconds(1), "output.mp4");
        InvokeInstance(service, "CompleteFlashbackExportDiagnostics", exportId, result);
        AssertFields();

        void AssertFields()
        {
            Assert.Equal(expectedStatus, ReadField("_flashbackExportStatus"));
            Assert.Equal(expectedKind, ReadField("_flashbackExportFailureKind"));
            Assert.Equal(Get<string>(result, "StatusMessage"), ReadField("_flashbackExportMessage"));
        }

        object? ReadField(string name)
            => captureType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service);
    }

    private static void AssertFailure(object result, string expectedCode, string expectedKind)
    {
        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Equal(expectedCode, Get<string>(result, "FailureCode"));
        Assert.Equal(expectedKind, GetKind(result));
    }

    private static object CreateFailure(string code, string message)
        => InvokeStatic(FailureType, "Create", "output.mp4", message, code, null);

    private static string GetKind(object result) => (string)InvokeStatic(FailureType, "GetKind", result);
    private static bool IsCancelled(object result) => (bool)InvokeStatic(FailureType, "IsCancelled", result);
    private static Type FailureType => TypeOf("Sussudio.Services.Flashback.FlashbackExportFailure");
    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;

    private static object InvokeStatic(Type type, string name, params object?[] arguments)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, arguments)!;

    private static object InvokeInstance(object instance, string name, params object?[] arguments)
        => instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(instance, arguments)!;

    private static T Get<T>(object instance, string name)
        => (T)instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void Set(object instance, string name, object value)
        => instance.GetType().GetProperty(name)!.SetValue(instance, value);

    private sealed class ExportFixture : IDisposable
    {
        internal ExportFixture(string outputName = "output.mp4")
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"sussudio-failure-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            OutputPath = Path.Combine(DirectoryPath, outputName);
            Exporter = Activator.CreateInstance(TypeOf("Sussudio.Services.Flashback.FlashbackExporter"))!;
        }

        internal string DirectoryPath { get; }
        internal string OutputPath { get; }
        internal object Exporter { get; }

        internal async Task<object> ExportAsync(string inputName, CancellationToken cancellationToken = default)
        {
            var request = Activator.CreateInstance(TypeOf("Sussudio.Models.FlashbackExportRequest"))!;
            Set(request, "InputTsPath", Path.Combine(DirectoryPath, inputName));
            Set(request, "OutputPath", OutputPath);
            Set(request, "InPoint", TimeSpan.Zero);
            Set(request, "OutPoint", TimeSpan.FromSeconds(1));
            var task = (Task)InvokeInstance(Exporter, "ExportAsync", request, null, cancellationToken);
            await task;
            return Get<object>(task, "Result");
        }

        public void Dispose()
        {
            ((IDisposable)Exporter).Dispose();
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
