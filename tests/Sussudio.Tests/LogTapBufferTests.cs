using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the log tap (docs/architecture/performance-telemetry.md section 2.8): a bounded ring of recent
// log lines plus per-caller and per-tag counts for spam detection. Lines arrive in the Logger's format
// "[HH:mm:ss.fff] [caller] message\n"; malformed lines, unbounded distinct keys and concurrent readers must never
// throw or grow without bound.
public sealed class LogTapBufferTests
{
    private static string Line(string caller, string message, int n = 0) =>
        $"[10:00:{n % 60:00}.{n % 1000:000}] [{caller}] {message}\n";

    [Fact]
    public void TheRingKeepsTheNewestCapacityLinesOldestFirst()
    {
        var tap = new LogTapBuffer();
        for (var i = 0; i < LogTapBuffer.DefaultCapacity + 904; i++)
        {
            tap.Add(Line("Caller", $"line {i}"));
        }

        var tail = tap.Tail(100_000, null);
        Assert.Equal(LogTapBuffer.DefaultCapacity, tail.Count);
        Assert.EndsWith("line 904", tail[0]);
        Assert.EndsWith($"line {LogTapBuffer.DefaultCapacity + 903}", tail[^1]);
        Assert.Equal(LogTapBuffer.DefaultCapacity + 904, tap.Counts().TotalLines);
    }

    [Fact]
    public void TailReturnsTheNewestMatchingLinesInChronologicalOrder()
    {
        var tap = new LogTapBuffer();
        for (var i = 0; i < 20; i++)
        {
            tap.Add(Line("A", $"alpha {i}"));
            tap.Add(Line("B", $"BETA {i}"));
        }

        Assert.Equal(5, tap.Tail(5, null).Count);
        Assert.EndsWith("BETA 19", tap.Tail(5, null)[^1]);

        var beta = tap.Tail(3, "beta");
        Assert.Equal(3, beta.Count);
        Assert.EndsWith("BETA 17", beta[0]);
        Assert.EndsWith("BETA 19", beta[2]);

        Assert.Empty(tap.Tail(10, "no such text"));
        Assert.Single(tap.Tail(0, null));
        Assert.Single(tap.Tail(-5, null));
    }

    [Fact]
    public void StoredLinesDropTheTrailingNewline()
    {
        var tap = new LogTapBuffer();
        tap.Add("[10:00:00.000] [A] hello\n");
        tap.Add("[10:00:00.001] [A] multi\nline\n");
        var tail = tap.Tail(10, null);
        Assert.Equal("[10:00:00.000] [A] hello", tail[0]);
        Assert.Equal("[10:00:00.001] [A] multi\nline", tail[1]);
    }

    [Theory]
    [InlineData("[10:00:00.123] [StartPreview] PREVIEW_RENDERER_ATTACHED mode=D3D11 attempt=3\n", "StartPreview", "PREVIEW_RENDERER_ATTACHED")]
    [InlineData("[10:00:00.123] [Foo] D3D11_PREVIEW_WAITABLE_SWAPCHAIN_CONFIGURED handle=0x1\n", "Foo", "D3D11_PREVIEW_WAITABLE_SWAPCHAIN_CONFIGURED")]
    [InlineData("[10:00:00.123] [Foo] [PREVIEW-FOO] something happened\n", "Foo", "PREVIEW-FOO")]
    [InlineData("[10:00:00.123] [Foo] TUNING_KNOB_SET name=X\n", "Foo", "TUNING_KNOB_SET")]
    [InlineData("[10:00:00.123] [Foo] PREVIEW_DROPPED: reason\n", "Foo", "PREVIEW_DROPPED")]
    [InlineData("[10:00:00.123] [Foo] Preview started\n", "Foo", null)]
    [InlineData("[10:00:00.123] [Foo] HDR output requested but disabled\n", "Foo", null)]
    [InlineData("[10:00:00.123] [Foo] EXCEPTION: InvalidOperationException\n", "Foo", null)]
    [InlineData("[10:00:00.123] [Foo] lower_snake_case is not a tag\n", "Foo", null)]
    [InlineData("[10:00:00.123] [Foo] 1_NUMERIC_START is not a tag\n", "Foo", null)]
    [InlineData("[10:00:00.123] [Foo] \n", "Foo", null)]
    [InlineData("[10:00:00.123] [] SOME_TAG x\n", "<unknown>", "SOME_TAG")]
    [InlineData("not a log line\n", "<unknown>", null)]
    [InlineData("[", "<unknown>", null)]
    [InlineData("[10:00:00.123", "<unknown>", null)]
    [InlineData("[10:00:00.123] [unterminated", "<unknown>", null)]
    [InlineData("", "<unknown>", null)]
    public void CallerAndTagAreParsedFromTheLoggerFormat(string entry, string expectedCaller, string? expectedTag)
    {
        var tap = new LogTapBuffer();
        tap.Add(entry);

        var counts = tap.Counts();
        Assert.Equal(1, counts.TotalLines);
        Assert.Equal(new[] { (expectedCaller, 1L) }, counts.ByCaller.Select(c => (c.Key, c.Count)).ToArray());
        if (expectedTag is null)
        {
            Assert.Empty(counts.ByTag);
        }
        else
        {
            Assert.Equal(new[] { (expectedTag, 1L) }, counts.ByTag.Select(c => (c.Key, c.Count)).ToArray());
        }
    }

    [Fact]
    public void CountsAreExactAndSortedByCountDescending()
    {
        var tap = new LogTapBuffer();
        for (var i = 0; i < 5; i++)
        {
            tap.Add(Line("Noisy", "SPAMMY_TAG value=1"));
        }

        for (var i = 0; i < 2; i++)
        {
            tap.Add(Line("Quiet", "OTHER_TAG value=1"));
        }

        tap.Add(Line("Quiet", "plain text"));

        var counts = tap.Counts();
        Assert.Equal(8, counts.TotalLines);
        Assert.Equal(new[] { ("Noisy", 5L), ("Quiet", 3L) }, counts.ByCaller.Select(c => (c.Key, c.Count)).ToArray());
        Assert.Equal(new[] { ("SPAMMY_TAG", 5L), ("OTHER_TAG", 2L) }, counts.ByTag.Select(c => (c.Key, c.Count)).ToArray());
    }

    [Fact]
    public void DistinctKeysAreBoundedAndOverflowIsCountedNotLost()
    {
        var tap = new LogTapBuffer();
        const int lines = 10_000;
        for (var i = 0; i < lines; i++)
        {
            tap.Add(Line($"Caller{i}", $"TAG_{i} detail"));
        }

        var counts = tap.Counts();
        Assert.Equal(lines, counts.TotalLines);
        Assert.True(counts.ByCaller.Count <= LogTapBuffer.MaxDistinctKeys + 1);
        Assert.True(counts.ByTag.Count <= LogTapBuffer.MaxDistinctKeys + 1);
        Assert.Equal(lines, counts.ByCaller.Sum(c => c.Count));
        Assert.Equal(lines, counts.ByTag.Sum(c => c.Count));
        Assert.Contains(counts.ByCaller, c => c.Key == LogTapBuffer.OverflowKey);
    }

    [Fact]
    public void AVeryLongLineIsStoredWithoutThrowing()
    {
        var tap = new LogTapBuffer();
        tap.Add(Line("Big", "BIG_TAG " + new string('x', 1_000_000)));
        Assert.Single(tap.Tail(10, null));
        Assert.Equal(1, tap.Counts().ByTag.Single().Count);
    }

    [Fact]
    public async Task ReadersDuringWritesNeverThrowAndCountsOnlyGrow()
    {
        var tap = new LogTapBuffer();
        using var stop = new CancellationTokenSource();
        var writer = Task.Run(() =>
        {
            var i = 0;
            while (!stop.IsCancellationRequested)
            {
                tap.Add(Line($"C{i % 7}", $"TAG_{i % 11} n={i}"));
                i++;
            }
        });

        long previous = 0;
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 300)
        {
            var counts = tap.Counts();
            Assert.True(counts.TotalLines >= previous);
            previous = counts.TotalLines;
            _ = tap.Tail(50, "tag_3");
        }

        stop.Cancel();
        await writer;
        Assert.True(previous > 0);
    }
}
