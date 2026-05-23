using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public class FlashbackBufferEvictionPauseOwnershipTests
{
    [Fact]
    public void EvictionPauseMembers_LiveInFocusedPartial()
    {
        var repoRoot = FindRepoRoot();
        var retentionText = ReadRepoFile(repoRoot, "Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("public bool IsDiskWarningActive", retentionText);
        Assert.Contains("public TimeSpan RecordingStartPts", retentionText);
        Assert.Contains("public TimeSpan RecordingEndPts", retentionText);
        Assert.Contains("public void PauseEviction()", retentionText);
        Assert.Contains("public (TimeSpan StartPts, TimeSpan EndPts) ResumeEviction()", retentionText);
        Assert.Contains("FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED", retentionText);
        Assert.Contains("private void EvictOldestSegments()", retentionText);
    }

    private static string ReadRepoFile(string repoRoot, string relativePath)
    {
        var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Sussudio.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Sussudio repo root.");
    }
}
