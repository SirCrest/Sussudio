using System.Threading.Channels;
using Sussudio.Services.Runtime;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of one pass of the logger's writer task (LogWriterBatch.Drain), written before the dropped-lines
// warning reached the log tap:
//
//  1. Lines were dropped because the queue was full. The warning reached the log file but not the log tap, so log tail
//     and the MCP log-tail tool showed neither the lost lines nor any sign of them. The tap must get the same warning
//     the file gets, after the pass's lines.
//  2. The warning repeats: the dropped count is taken once, so a later pass with no new drops warns about nothing.
//  3. A pass takes every queued line: it stops at its limit and leaves the rest queued, in order, for the next pass.
//  4. Nothing queued and nothing dropped: the pass returns no text, so the writer appends nothing.
public sealed class LogWriterBatchTests
{
    private static Channel<string> Queue(params string[] lines)
    {
        var queue = Channel.CreateUnbounded<string>();
        foreach (var line in lines)
        {
            queue.Writer.TryWrite(line);
        }

        return queue;
    }

    [Fact]
    public void DroppedLinesAreReportedToTheTapAsWellAsTheFile()
    {
        var queue = Queue("[10:00:00.000] [A] one\n", "[10:00:00.001] [B] two\n");
        var tap = new List<string>();
        long unreported = 3;
        long total = 7;

        var text = LogWriterBatch.Drain(queue.Reader, 256, ref unreported, ref total, tap.Add);

        Assert.Equal(3, tap.Count);
        Assert.Equal("[10:00:00.000] [A] one\n", tap[0]);
        Assert.Equal("[10:00:00.001] [B] two\n", tap[1]);
        Assert.Contains("] [Logger] Warning: log channel saturated, dropped_in_batch=3 dropped_total=7\n", tap[2], StringComparison.Ordinal);
        Assert.Equal(string.Concat(tap), text);
        Assert.Equal(0, unreported);
    }

    [Fact]
    public void TheDroppedCountIsReportedOnce()
    {
        var queue = Queue("[10:00:00.000] [A] one\n");
        var tap = new List<string>();
        long unreported = 2;
        long total = 2;
        LogWriterBatch.Drain(queue.Reader, 256, ref unreported, ref total, tap.Add);
        tap.Clear();
        queue.Writer.TryWrite("[10:00:01.000] [A] two\n");

        var text = LogWriterBatch.Drain(queue.Reader, 256, ref unreported, ref total, tap.Add);

        Assert.Equal(new[] { "[10:00:01.000] [A] two\n" }, tap);
        Assert.Equal("[10:00:01.000] [A] two\n", text);
    }

    [Fact]
    public void APassStopsAtItsLimitAndLeavesTheRestQueuedInOrder()
    {
        var queue = Queue("1\n", "2\n", "3\n");
        var tap = new List<string>();
        long unreported = 0;
        long total = 0;

        Assert.Equal("1\n2\n", LogWriterBatch.Drain(queue.Reader, 2, ref unreported, ref total, tap.Add));
        Assert.Equal("3\n", LogWriterBatch.Drain(queue.Reader, 2, ref unreported, ref total, tap.Add));
        Assert.Equal(new[] { "1\n", "2\n", "3\n" }, tap);
    }

    [Fact]
    public void NothingQueuedOrDroppedWritesNothing()
    {
        var queue = Queue();
        var tap = new List<string>();
        long unreported = 0;
        long total = 5;

        Assert.Equal(string.Empty, LogWriterBatch.Drain(queue.Reader, 256, ref unreported, ref total, tap.Add));
        Assert.Empty(tap);
    }
}
