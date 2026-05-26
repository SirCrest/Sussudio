using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static string ReadLibAvRecordingSinkSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadUnifiedVideoCaptureSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ExtractSourceBlock(string source, string startToken, string endToken)
    {
        var normalizedSource = NormalizeLineEndings(source);
        var normalizedStartToken = NormalizeLineEndings(startToken);
        var normalizedEndToken = NormalizeLineEndings(endToken);
        var start = normalizedSource.IndexOf(normalizedStartToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{startToken}'.");
        }

        var end = normalizedSource.IndexOf(normalizedEndToken, start + normalizedStartToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source after '{startToken}' to contain '{endToken}'.");
        }

        return normalizedSource[start..end];
    }

}
