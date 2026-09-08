using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

[Collection(RecoveryEnvironmentCollection.Name)]
public sealed class LegacyRecordingRecoveryTests
{
    [Theory]
    [InlineData("active", ".recording-active.txt")]
    [InlineData("unresolved", ".recording-finalization-unresolved.txt")]
    public void LegacyJournalRestoresSeparateAudioWithoutALiveRecordingContext(string status, string suffix)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"sussudio-legacy-recovery-{Guid.NewGuid():N}");
        var previousRecoveryDirectory = Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY");
        Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", Path.Combine(directory, "stable"));
        Directory.CreateDirectory(directory);
        try
        {
            var outputPath = Path.Combine(directory, "unfinished.mp4");
            var videoPath = Path.Combine(directory, "legacy-video.mp4");
            var audioPath = Path.Combine(directory, "legacy=audio.m4a");
            var videoBytes = new byte[] { 1, 2, 3 };
            var audioBytes = new byte[] { 4, 5, 6 };
            File.WriteAllBytes(videoPath, videoBytes);
            File.WriteAllBytes(audioPath, audioBytes);
            var markerPath = outputPath + suffix;
            File.WriteAllLines(markerPath, new[]
            {
                "status=" + status,
                "utc=2026-09-03T12:00:00.0000000+00:00",
                "reason=Interrupted legacy recording.",
                "final_output=" + outputPath,
                "video_output=" + videoPath,
                "audio_temp=" + audioPath
            });

            var recoveryType = SussudioAssembly.Load().GetType(
                "Sussudio.Services.Recording.RecordingFinalizationRecoveryArtifacts", throwOnError: true)!;
            var restore = recoveryType.GetMethod("TryLoadLatest", BindingFlags.NonPublic | BindingFlags.Static)!;
            var recovered = restore.Invoke(null, new object?[] { directory });

            Assert.NotNull(recovered);
            Assert.Equal(outputPath, Read<string>(recovered!, "OutputPath"));
            Assert.Equal("Interrupted legacy recording.", Read<string>(recovered!, "Reason"));
            var artifacts = Read<IReadOnlyList<string>>(recovered!, "PreservedArtifacts");
            Assert.Contains(videoPath, artifacts);
            Assert.Contains(audioPath, artifacts);
            Assert.Contains(markerPath, artifacts);
            Assert.DoesNotContain(outputPath, artifacts);
            Assert.Equal(videoBytes, File.ReadAllBytes(videoPath));
            Assert.Equal(audioBytes, File.ReadAllBytes(audioPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY", previousRecoveryDirectory);
            Directory.Delete(directory, recursive: true);
        }
    }

    private static T Read<T>(object instance, string property)
        => (T)instance.GetType().GetProperty(property)!.GetValue(instance)!;
}
