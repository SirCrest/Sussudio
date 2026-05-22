using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public class RecordingArtifactManagerTests
{
    [Fact]
    public void ArtifactManager_OwnsContextCreationAndFinalization()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/RecordingArtifactManager.cs");

        AssertContains(rootText, "public sealed class RecordingArtifactManager");
        AssertDoesNotContain(rootText, "partial class RecordingArtifactManager");
        AssertContains(rootText, "public async Task<RecordingContext> CreateContextAsync(");
        AssertContains(rootText, "private static RecordingContext BuildContext(");
        AssertContains(rootText, "public FinalizeResult FinalizeContext(");
        AssertContains(rootText, "public Task RollbackAsync(");
        AssertContains(rootText, "private static bool TryValidateFinalOutput(");
        AssertContains(rootText, "private static IReadOnlyList<string> GetExistingTempArtifacts(");
    }

    [Fact]
    public void ArtifactManager_FinalizeContext_ReturnsSuccess_WhenPostMuxDisabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var finalPath = Path.Combine(tempDir, "video.mp4");
            File.WriteAllText(finalPath, "video-data");

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(usePostMuxAudio: false, finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, true, null })!;

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "Succeeded");
            AssertEqual(finalPath, GetStringProperty(result, "OutputPath"), "OutputPath");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void ArtifactManager_FinalizeContext_PreservesTempArtifacts_WhenMuxFails()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(finalPath, Array.Empty<byte>());

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");
            var result = finalizeMethod.Invoke(manager, new object?[] { context, false, "encoder error" })!;

            AssertEqual(false, GetBoolProperty(result, "Succeeded"), "Succeeded");
            var preserved = GetPropertyValue(result, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "PreservedArtifacts.Count");

            if (File.Exists(finalPath))
            {
                throw new InvalidOperationException("Expected empty final file to be deleted");
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void ArtifactManager_FinalizeContext_RejectsInvalidFinalOutput()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var emptyFinalPath = Path.Combine(tempDir, "empty-final.mp4");
            var missingFinalPath = Path.Combine(tempDir, "missing-final.mp4");
            File.WriteAllText(videoPath, "video-data");
            File.WriteAllText(audioPath, "audio-data");
            File.WriteAllBytes(emptyFinalPath, Array.Empty<byte>());

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var finalizeMethod = manager.GetType().GetMethod("FinalizeContext")
                ?? throw new InvalidOperationException("FinalizeContext not found");

            var directContext = BuildRecordingContext(usePostMuxAudio: false, finalPath: emptyFinalPath);
            var directResult = finalizeMethod.Invoke(manager, new object?[] { directContext, true, null })!;
            AssertEqual(false, GetBoolProperty(directResult, "Succeeded"), "Direct empty output finalize fails");
            AssertContains(GetStringProperty(directResult, "StatusMessage"), "final output invalid");
            AssertContains(GetStringProperty(directResult, "StatusMessage"), "output file is empty");

            var muxContext = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: missingFinalPath);
            var muxResult = finalizeMethod.Invoke(manager, new object?[] { muxContext, true, null })!;
            AssertEqual(false, GetBoolProperty(muxResult, "Succeeded"), "Mux success with missing final output fails");
            AssertContains(GetStringProperty(muxResult, "StatusMessage"), "output file is missing");
            var preserved = GetPropertyValue(muxResult, "PreservedArtifacts");
            AssertEqual(2, GetCountProperty(preserved), "Invalid mux final preserves temp artifacts");
            AssertEqual(true, File.Exists(videoPath), "Invalid mux final preserves video temp");
            AssertEqual(true, File.Exists(audioPath), "Invalid mux final preserves audio temp");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task ArtifactManager_RollbackAsync_DeletesAllArtifacts_WhenPostMuxEnabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"elgtest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var videoPath = Path.Combine(tempDir, "vid.mp4");
            var audioPath = Path.Combine(tempDir, "aud.m4a");
            var finalPath = Path.Combine(tempDir, "final.mp4");
            File.WriteAllText(videoPath, "v");
            File.WriteAllText(audioPath, "a");
            File.WriteAllText(finalPath, "f");

            var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
            var context = BuildRecordingContext(
                usePostMuxAudio: true,
                videoPath: videoPath,
                audioTempPath: audioPath,
                finalPath: finalPath);

            var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
                ?? throw new InvalidOperationException("RollbackAsync not found");
            var task = rollbackMethod.Invoke(manager, new object?[] { context, CancellationToken.None }) as Task
                ?? throw new InvalidOperationException("RollbackAsync did not return Task");
            await task;

            if (File.Exists(videoPath))
            {
                throw new InvalidOperationException("Expected video temp to be deleted");
            }

            if (File.Exists(audioPath))
            {
                throw new InvalidOperationException("Expected audio temp to be deleted");
            }

            if (File.Exists(finalPath))
            {
                throw new InvalidOperationException("Expected final output to be deleted");
            }
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task ArtifactManager_RollbackAsync_SafeWithNullContext()
    {
        var manager = CreateInstance("Sussudio.Services.Recording.RecordingArtifactManager");
        var rollbackMethod = manager.GetType().GetMethod("RollbackAsync")
            ?? throw new InvalidOperationException("RollbackAsync not found");

        _ = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var task = rollbackMethod.Invoke(manager, new object?[] { null, CancellationToken.None }) as Task
            ?? throw new InvalidOperationException("RollbackAsync did not return Task");
        await task;
    }

    private static object BuildRecordingContext(
        bool usePostMuxAudio,
        string? videoPath = null,
        string? audioTempPath = null,
        string? finalPath = null)
    {
        var settings = BuildSettings();
        var contextType = RequireType("Sussudio.Services.Contracts.RecordingContext");
        var context = RuntimeHelpers.GetUninitializedObject(contextType);
        SetPropertyBackingField(context, "Settings", settings);
        SetPropertyBackingField(context, "UsePostMuxAudio", usePostMuxAudio);
        SetPropertyBackingField(context, "EffectiveFrameRate", 60.0);
        SetPropertyBackingField(context, "FrameRateArg", "60");
        SetPropertyBackingField(context, "EffectiveWidth", 1920u);
        SetPropertyBackingField(context, "EffectiveHeight", 1080u);
        SetPropertyBackingField(context, "VideoInputPixelFormat", "nv12");
        SetPropertyBackingField(context, "VideoOutputPath", videoPath ?? "/tmp/video.mp4");
        SetPropertyBackingField(context, "FinalOutputPath", finalPath ?? "/tmp/final.mp4");
        SetPropertyBackingField(context, "AudioTempPath", audioTempPath);
        SetPropertyBackingField(context, "HdrPipelineActive", false);
        return context;
    }

    private static object BuildSettings()
    {
        var settings = CreateInstance("Sussudio.Models.CaptureSettings");
        SetPropertyOrBackingField(settings, "Width", 1920u);
        SetPropertyOrBackingField(settings, "Height", 1080u);
        SetPropertyOrBackingField(settings, "FrameRate", 60d);
        SetPropertyOrBackingField(settings, "RequestedFrameRateArg", "60/1");
        SetPropertyOrBackingField(settings, "RequestedFrameRateNumerator", 60u);
        SetPropertyOrBackingField(settings, "RequestedFrameRateDenominator", 1u);
        SetPropertyOrBackingField(settings, "RequestedPixelFormat", "NV12");
        SetPropertyOrBackingField(settings, "Format", ParseEnum("Sussudio.Models.RecordingFormat", "HevcMp4"));
        SetPropertyOrBackingField(settings, "Quality", ParseEnum("Sussudio.Models.VideoQuality", "High"));
        SetPropertyOrBackingField(settings, "HdrEnabled", false);
        SetPropertyOrBackingField(settings, "HdrOutputMode", ParseEnum("Sussudio.Models.HdrOutputMode", "Hdr10Pq"));
        SetPropertyOrBackingField(settings, "AudioEnabled", true);
        SetPropertyOrBackingField(settings, "OutputPath", Path.GetTempPath());
        return settings;
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateInstance(string typeName)
        => Activator.CreateInstance(RequireType(typeName))
           ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string name, object? value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        SetPropertyBackingField(instance, name, value);
    }

    private static void SetPropertyBackingField(object instance, string name, object? value)
    {
        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static bool GetBoolProperty(object instance, string name)
        => (bool)GetPropertyValue(instance, name)!;

    private static string GetStringProperty(object instance, string name)
        => (string)GetPropertyValue(instance, name)!;

    private static int GetCountProperty(object? value)
        => value is ICollection collection
            ? collection.Count
            : throw new InvalidOperationException("Expected collection value.");

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertEqual<T>(T expected, T actual, string _)
        => Assert.Equal(expected, actual);
}
