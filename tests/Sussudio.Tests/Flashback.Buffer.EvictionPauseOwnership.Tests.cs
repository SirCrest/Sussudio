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
        var evictionPauseText = ReadRepoFile(repoRoot, "Sussudio/Services/Flashback/FlashbackBufferManager.EvictionPause.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("public bool IsDiskWarningActive", evictionPauseText);
        Assert.Contains("public TimeSpan RecordingStartPts", evictionPauseText);
        Assert.Contains("public TimeSpan RecordingEndPts", evictionPauseText);
        Assert.Contains("public void PauseEviction()", evictionPauseText);
        Assert.Contains("public (TimeSpan StartPts, TimeSpan EndPts) ResumeEviction()", evictionPauseText);
        Assert.Contains("FLASHBACK_BUFFER_EVICTION_RESUME_UNBALANCED", evictionPauseText);

        Assert.DoesNotContain("public bool IsDiskWarningActive", retentionText);
        Assert.DoesNotContain("public void PauseEviction()", retentionText);
        Assert.DoesNotContain("public (TimeSpan StartPts, TimeSpan EndPts) ResumeEviction()", retentionText);
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
