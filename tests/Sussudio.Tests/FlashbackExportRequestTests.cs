using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackExportRequestTests
{
    [Fact]
    public async Task PathOnlySegmentObjectsUsePublicSegmentValidationBeforeCreatingOutput()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sussudio-export-request-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var assembly = SussudioAssembly.Load();
            var exporterType = assembly.GetType("Sussudio.Services.Flashback.FlashbackExporter", throwOnError: true)!;
            var requestType = assembly.GetType("Sussudio.Models.FlashbackExportRequest", throwOnError: true)!;
            var segmentType = assembly.GetType("Sussudio.Models.FlashbackExportSegment", throwOnError: true)!;
            using var exporter = (IDisposable)Activator.CreateInstance(exporterType)!;
            var segmentPath = Path.Combine(directory, "segment.ts");
            var sourceBytes = new byte[] { 1, 2, 3 };
            File.WriteAllBytes(segmentPath, sourceBytes);
            var segments = Array.CreateInstance(segmentType, 2);
            var first = Activator.CreateInstance(segmentType)!;
            Set(first, "Path", segmentPath);
            var duplicate = Activator.CreateInstance(segmentType)!;
            Set(duplicate, "Path", Path.Combine(directory, ".", "segment.ts"));
            segments.SetValue(first, 0);
            segments.SetValue(duplicate, 1);

            var outputPath = Path.Combine(directory, "output.mp4");
            var request = Activator.CreateInstance(requestType)!;
            Set(request, "Segments", segments);
            Set(request, "InputPath", Path.Combine(directory, "unused-single-file.ts"));
            Set(request, "InPoint", TimeSpan.Zero);
            Set(request, "OutPoint", TimeSpan.FromSeconds(1));
            Set(request, "OutputPath", outputPath);
            var export = exporterType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance)!;
            var task = (Task)export.Invoke(exporter, new object?[] { request, null, CancellationToken.None })!;
            await task;
            var result = Read<object>(task, "Result");

            Assert.False(Read<bool>(result, "Succeeded"));
            Assert.Equal("flashback-export-segment-unavailable", Read<string>(result, "FailureCode"));
            Assert.Contains("duplicate segment path at index 1", Read<string>(result, "StatusMessage"));
            Assert.False(File.Exists(outputPath));
            Assert.Equal(sourceBytes, File.ReadAllBytes(segmentPath));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static T Read<T>(object instance, string property)
        => (T)instance.GetType().GetProperty(property)!.GetValue(instance)!;

    private static void Set(object instance, string property, object value)
        => instance.GetType().GetProperty(property)!.SetValue(instance, value);
}
