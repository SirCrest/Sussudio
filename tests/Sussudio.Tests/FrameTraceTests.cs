using System.Diagnostics;
using System.Text.Json;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the frame-trace flight recorder (docs/architecture/performance-telemetry.md sections 2.4
// and 5): disabled recording, wraparound order, torn slots, capacity clamping, marker escaping and a dump taken
// while writers are running.
public sealed class FrameTraceTests
{
    private static FrameTraceRecorder NewRecorder(PerfMarkerLog? markers = null, PerfThreadNames? threads = null) =>
        new(markers ?? new PerfMarkerLog(), threads ?? new PerfThreadNames());

    private static JsonDocument Dump(FrameTraceRecorder recorder)
    {
        using var stream = new MemoryStream();
        Assert.True(recorder.TryWriteChromeTrace(stream, out var error), error);
        stream.Position = 0;
        return JsonDocument.Parse(stream);
    }

    [Fact]
    public void ADisabledRecorderWritesNothingAndTheStaticFacadeIsOffByDefault()
    {
        Assert.False(FrameTrace.IsEnabled);
        FrameTrace.Record(FrameTraceEventKind.PreviewPresent, Stopwatch.GetTimestamp(), 10, 1);
        FrameTrace.RecordInstant(FrameTraceEventKind.PreviewDrop, Stopwatch.GetTimestamp(), 1);

        var recorder = NewRecorder();
        Assert.False(recorder.IsEnabled);
        recorder.Record(FrameTraceEventKind.PreviewPresent, Stopwatch.GetTimestamp(), 10, 1);

        var status = recorder.GetStatus();
        Assert.False(status.Running);
        Assert.False(status.HasBuffer);
        Assert.Equal(0, status.Recorded);

        // Records made before start do not appear once a trace starts.
        Assert.True(recorder.Start(0).Success);
        Assert.Equal(0, recorder.GetStatus().Recorded);
    }

    [Theory]
    [InlineData(0, FrameTraceBuffer.DefaultCapacity)]
    [InlineData(-5, FrameTraceBuffer.DefaultCapacity)]
    [InlineData(1, FrameTraceBuffer.MinCapacity)]
    [InlineData(4095, FrameTraceBuffer.MinCapacity)]
    [InlineData(4096, 4096)]
    [InlineData(4097, 8192)]
    [InlineData(100_000, 131_072)]
    [InlineData(1 << 22, 1 << 22)]
    [InlineData((1 << 22) + 1, FrameTraceBuffer.MaxCapacity)]
    [InlineData(int.MaxValue, FrameTraceBuffer.MaxCapacity)]
    public void CapacityRoundsUpToAPowerOfTwoWithinRange(int requested, int expected)
    {
        Assert.Equal(expected, FrameTraceBuffer.NormalizeCapacity(requested));
        Assert.Equal(expected, new FrameTraceBuffer(requested).Capacity);
    }

    [Fact]
    public void WraparoundKeepsTheNewestCapacityEventsInOrder()
    {
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        var total = FrameTraceBuffer.MinCapacity + 100;
        for (var i = 0; i < total; i++)
        {
            buffer.Record(FrameTraceEventKind.CaptureRead, i * 3L, i * 2L, i);
        }

        var events = buffer.Copy();
        Assert.Equal(FrameTraceBuffer.MinCapacity, events.Count);
        Assert.Equal(100, events[0].Id);
        Assert.Equal(total - 1, events[^1].Id);
        for (var i = 1; i < events.Count; i++)
        {
            Assert.Equal(events[i - 1].Id + 1, events[i].Id);
        }

        Assert.Equal(total, buffer.Recorded);
        Assert.Equal(100, buffer.Overwritten);
    }

    [Fact]
    public void AnUnfilledBufferReturnsOnlyWhatWasRecorded()
    {
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        for (var i = 0; i < 10; i++)
        {
            buffer.Record(FrameTraceEventKind.CaptureDeliver, i, 1, i);
        }

        var events = buffer.Copy();
        Assert.Equal(10, events.Count);
        Assert.Equal(0, buffer.Overwritten);
        Assert.All(events, e => Assert.Equal(FrameTraceEventKind.CaptureDeliver, e.Kind));
    }

    [Fact]
    public void ASlotRewrittenDuringTheCopyIsDroppedNeverEmitted()
    {
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        for (var i = 0; i < FrameTraceBuffer.MinCapacity; i++)
        {
            buffer.Record(FrameTraceEventKind.PreviewRenderTotal, i * 3L, i * 2L, i);
        }

        // While the copy sits between its two stamp reads for the oldest slot, a writer laps the ring and
        // overwrites that slot (and only that slot) with a new event.
        var lapped = false;
        var events = buffer.Copy(afterFieldsCopied: index =>
        {
            if (index == 0 && !lapped)
            {
                lapped = true;
                buffer.Record(FrameTraceEventKind.PreviewRenderTotal, 999_000, 999_001, 999_002);
            }
        });

        Assert.True(lapped);
        Assert.DoesNotContain(events, e => e.Id == 0);
        Assert.DoesNotContain(events, e => e.Id == 999_002);
        Assert.Equal(FrameTraceBuffer.MinCapacity - 1, events.Count);
        Assert.All(events, e =>
        {
            Assert.Equal(e.Id * 3L, e.Ts);
            Assert.Equal(e.Id * 2L, e.Dur);
        });
    }

    [Fact]
    public async Task ACopyTakenWhileWritersRunNeverContainsATornEvent()
    {
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        using var stop = new CancellationTokenSource();
        var writers = Enumerable.Range(0, 4).Select(w => Task.Run(() =>
        {
            long id = w;
            while (!stop.IsCancellationRequested)
            {
                buffer.Record(FrameTraceEventKind.MjpegDecode, id * 3, id * 2, id);
                id += 4;
            }
        })).ToArray();

        var deadline = Stopwatch.StartNew();
        var copies = 0;
        while (deadline.ElapsedMilliseconds < 400)
        {
            foreach (var e in buffer.Copy())
            {
                Assert.Equal(e.Id * 3, e.Ts);
                Assert.Equal(e.Id * 2, e.Dur);
                Assert.Equal(FrameTraceEventKind.MjpegDecode, e.Kind);
            }

            copies++;
        }

        stop.Cancel();
        await Task.WhenAll(writers);
        Assert.True(copies > 0);
    }

    [Fact]
    public void MarkerTextIsJsonEscapedAndRoundTrips()
    {
        var markers = new PerfMarkerLog();
        var recorder = NewRecorder(markers);
        Assert.True(recorder.Start(0).Success);
        var tricky = "variant \"B\" \\ path\\to\nline2\ttab \u0001 control é 😀 </script>";
        markers.Add(tricky);

        using var document = Dump(recorder);
        var marker = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Single(e => e.GetProperty("ph").GetString() == "i" && e.GetProperty("name").GetString() == "marker");
        Assert.Equal(tricky, marker.GetProperty("args").GetProperty("text").GetString());
    }

    [Fact]
    public void MarkersBeforeTheTraceStartedAreNotEmittedButLaterOnesAre()
    {
        var markers = new PerfMarkerLog();
        markers.Add("before");
        Thread.Sleep(5);
        var recorder = NewRecorder(markers);
        Assert.True(recorder.Start(0).Success);
        Thread.Sleep(5);
        markers.Add("after");

        using var document = Dump(recorder);
        var texts = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Where(e => e.GetProperty("name").GetString() == "marker")
            .Select(e => e.GetProperty("args").GetProperty("text").GetString())
            .ToList();
        Assert.Equal(new[] { "after" }, texts);
    }

    [Fact]
    public void TheDumpUsesStageNamesAndThreadNamesNotNumbers()
    {
        var threads = new PerfThreadNames();
        var recorder = NewRecorder(threads: threads);
        Assert.True(recorder.Start(0).Success);
        threads.Set(Environment.CurrentManagedThreadId, "unit.test.thread");

        var start = Stopwatch.GetTimestamp();
        recorder.Record(FrameTraceEventKind.PreviewRenderTotal, start, Stopwatch.Frequency / 1000, 42);
        recorder.RecordInstant(FrameTraceEventKind.PreviewDrop, start + 5, 43);

        using var document = Dump(recorder);
        var root = document.RootElement;
        Assert.Equal("ms", root.GetProperty("displayTimeUnit").GetString());
        var events = root.GetProperty("traceEvents").EnumerateArray().ToList();

        var complete = events.Single(e => e.GetProperty("name").GetString() == "preview.render_total");
        Assert.Equal("X", complete.GetProperty("ph").GetString());
        Assert.Equal("preview", complete.GetProperty("cat").GetString());
        Assert.Equal(42, complete.GetProperty("args").GetProperty("id").GetInt64());
        Assert.InRange(complete.GetProperty("dur").GetDouble(), 990, 1010);

        var instant = events.Single(e => e.GetProperty("name").GetString() == "preview.drop");
        Assert.Equal("i", instant.GetProperty("ph").GetString());

        Assert.Contains(events, e =>
            e.GetProperty("ph").GetString() == "M"
            && e.GetProperty("name").GetString() == "thread_name"
            && e.GetProperty("args").GetProperty("name").GetString() == "unit.test.thread");
        Assert.Contains(events, e =>
            e.GetProperty("ph").GetString() == "M"
            && e.GetProperty("name").GetString() == "process_name");

        // Timestamps are relative to the earliest retained timestamp, never negative.
        Assert.All(events.Where(e => e.GetProperty("ph").GetString() != "M"), e => Assert.True(e.GetProperty("ts").GetDouble() >= 0));
    }

    [Fact]
    public void EveryEventKindHasANameAndCategory()
    {
        foreach (var kind in Enum.GetValues<FrameTraceEventKind>())
        {
            Assert.False(string.IsNullOrWhiteSpace(FrameTraceEventKinds.Name(kind)), kind.ToString());
            Assert.False(string.IsNullOrWhiteSpace(FrameTraceEventKinds.Category(kind)), kind.ToString());
        }

        Assert.StartsWith("kind-", FrameTraceEventKinds.Name((FrameTraceEventKind)9999));
    }

    [Fact]
    public async Task ADumpWhileWritersRunIsAlwaysValidJson()
    {
        var recorder = NewRecorder();
        Assert.True(recorder.Start(FrameTraceBuffer.MinCapacity).Success);
        using var stop = new CancellationTokenSource();
        var writers = Enumerable.Range(0, 3).Select(w => Task.Run(() =>
        {
            long id = w;
            while (!stop.IsCancellationRequested)
            {
                recorder.Record(FrameTraceEventKind.CaptureRead, Stopwatch.GetTimestamp(), 100, id);
                id += 3;
            }
        })).ToArray();

        for (var i = 0; i < 20; i++)
        {
            using var document = Dump(recorder);
            Assert.True(document.RootElement.GetProperty("traceEvents").GetArrayLength() > 0);
        }

        stop.Cancel();
        await Task.WhenAll(writers);
    }

    [Fact]
    public void StartWhileRunningIsRejectedStopFreezesAndDumpStillWorks()
    {
        var recorder = NewRecorder();
        var first = recorder.Start(0);
        Assert.True(first.Success);
        Assert.Equal(FrameTraceBuffer.DefaultCapacity, first.Capacity);

        var second = recorder.Start(8192);
        Assert.False(second.Success);
        Assert.False(string.IsNullOrWhiteSpace(second.Error));

        recorder.Record(FrameTraceEventKind.AudioRender, Stopwatch.GetTimestamp(), 5, 1);
        var stopped = recorder.Stop();
        Assert.False(stopped.Running);
        Assert.True(stopped.HasBuffer);
        Assert.Equal(1, stopped.Retained);

        // After stop, new records are ignored and the frozen buffer still dumps.
        recorder.Record(FrameTraceEventKind.AudioRender, Stopwatch.GetTimestamp(), 5, 2);
        using var document = Dump(recorder);
        Assert.Equal(1, document.RootElement.GetProperty("traceEvents").EnumerateArray().Count(e => e.GetProperty("name").GetString() == "audio.render"));

        // A second stop is harmless and a new start replaces the frozen buffer.
        Assert.False(recorder.Stop().Running);
        Assert.True(recorder.Start(4096).Success);
        Assert.Equal(0, recorder.GetStatus().Recorded);
    }

    [Fact]
    public void DumpWithoutAnyTraceReportsAnErrorInsteadOfAnEmptyFile()
    {
        var recorder = NewRecorder();
        using var stream = new MemoryStream();
        Assert.False(recorder.TryWriteChromeTrace(stream, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void EventsAreFortyBytes()
    {
        Assert.Equal(40, FrameTraceBuffer.EventSizeBytes);
    }
}
