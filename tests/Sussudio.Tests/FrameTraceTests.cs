using System.Diagnostics;
using System.Text.Json;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the frame-trace flight recorder (docs/architecture/performance-telemetry.md sections 2.4
// and 5): disabled recording, wraparound order, torn slots, capacity clamping, marker escaping and a dump taken
// while writers are running; hardening: a writer preempted across a ring wrap, chunked reads and dumps at chunk
// boundaries, ring reuse (no stale events, none while a dump reads the ring), and thread names that outlive their
// thread.
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

        var events = buffer.ReadAll();
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

        var events = buffer.ReadAll();
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
        var events = buffer.ReadAll(afterFieldsCopied: index =>
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
            foreach (var e in buffer.ReadAll())
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
        // Dedicated threads: pool-queued writers can start after the first dumps under a loaded parallel test run.
        var writers = Enumerable.Range(0, 3).Select(w => Task.Factory.StartNew(() =>
        {
            long id = w;
            while (!stop.IsCancellationRequested)
            {
                recorder.Record(FrameTraceEventKind.CaptureRead, Stopwatch.GetTimestamp(), 100, id);
                id += 3;
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

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

    [Fact]
    public void AWriterPreemptedAcrossARingWrapNeverLandsStaleFieldsOnANewerEvent()
    {
        // The writer claimed index 0 and was descheduled before storing anything. Meanwhile the ring wrapped and the
        // slot now belongs to index MinCapacity. When the old writer resumes it must find the slot taken and drop its
        // own (stale) event; before the ownership compare-exchange its stale Dur/Id/Kind landed on the newer event's
        // Ts, a torn event with a valid stamp that readers accepted.
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        for (var i = 0; i <= FrameTraceBuffer.MinCapacity; i++)
        {
            buffer.Record(FrameTraceEventKind.PreviewRenderTotal, i * 3L, i * 2L, i);
        }

        buffer.WriteClaimed(0, FrameTraceEventKind.CaptureRead, 999_999, 888_888, 777_777, 0);

        Assert.Equal(1, buffer.Abandoned);
        var events = buffer.ReadAll();
        Assert.DoesNotContain(events, e => e.Id == 777_777);
        var newest = Assert.Single(events, e => e.Id == FrameTraceBuffer.MinCapacity);
        Assert.Equal(FrameTraceBuffer.MinCapacity * 3L, newest.Ts);
        Assert.Equal(FrameTraceBuffer.MinCapacity * 2L, newest.Dur);
        Assert.Equal(FrameTraceEventKind.PreviewRenderTotal, newest.Kind);
        Assert.All(events, e =>
        {
            Assert.Equal(e.Id * 3L, e.Ts);
            Assert.Equal(e.Id * 2L, e.Dur);
        });
    }

    [Fact]
    public void AnUndisturbedTraceReportsNoAbandonedEvents()
    {
        var recorder = NewRecorder();
        Assert.True(recorder.Start(FrameTraceBuffer.MinCapacity).Success);
        for (var i = 0; i < FrameTraceBuffer.MinCapacity * 3; i++)
        {
            recorder.Record(FrameTraceEventKind.CaptureRead, i, 1, i);
        }

        using var document = Dump(recorder);
        var meta = document.RootElement.GetProperty("sussudio");
        Assert.Equal(0, meta.GetProperty("abandoned").GetInt64());
        Assert.Equal(FrameTraceBuffer.MinCapacity, meta.GetProperty("retained").GetInt64());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(1000)]
    [InlineData(4096)]
    [InlineData(5000)]
    public void EveryChunkSizeReadsTheSameEventsAsOneBigChunkAcrossAWrap(int chunkSize)
    {
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        var total = FrameTraceBuffer.MinCapacity + 300;
        for (var i = 0; i < total; i++)
        {
            buffer.Record(FrameTraceEventKind.CaptureRead, i * 3L, i * 2L, i);
        }

        var expected = buffer.ReadAll(chunkSize: 1 << 16);
        var actual = buffer.ReadAll(chunkSize);

        Assert.Equal(FrameTraceBuffer.MinCapacity, expected.Count);
        Assert.Equal(expected, actual);
        Assert.Equal(300, actual[0].Id);
        Assert.Equal(total - 1, actual[^1].Id);
    }

    [Fact]
    public void SkippedSlotsDoNotShortenAChunkOrEndTheReadEarly()
    {
        // Two slots in the middle are lapped during the read. A chunk of 3 must still come back full from the slots
        // after them, and the cursor must carry on to the end instead of treating a short chunk as the end.
        var buffer = new FrameTraceBuffer(FrameTraceBuffer.MinCapacity);
        for (var i = 0; i < FrameTraceBuffer.MinCapacity; i++)
        {
            buffer.Record(FrameTraceEventKind.CaptureRead, i * 3L, i * 2L, i);
        }

        var events = buffer.ReadAll(chunkSize: 3, afterFieldsCopied: index =>
        {
            // A writer one lap ahead claims the very slot being read: index + capacity maps to the same slot.
            if (index is 5 or 6)
            {
                buffer.WriteClaimed(FrameTraceBuffer.MinCapacity + index, FrameTraceEventKind.CaptureRead, 1, 1, 900_000 + index, 0);
            }
        });

        Assert.Equal(FrameTraceBuffer.MinCapacity - 2, events.Count);
        Assert.DoesNotContain(events, e => e.Id is 5 or 6);
        Assert.Equal(FrameTraceBuffer.MinCapacity - 1L, events[^1].Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    [InlineData(2048)]
    [InlineData(2049)]
    public void ADumpAtChunkBoundariesWritesEveryEventOnceAndInOrder(int count)
    {
        var recorder = NewRecorder();
        Assert.True(recorder.Start(0).Success);
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            recorder.Record(FrameTraceEventKind.CaptureRead, start + i * 10L, 4, i);
        }

        using var document = Dump(recorder);
        var events = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Where(e => e.GetProperty("name").GetString() == "capture.read")
            .ToList();
        Assert.Equal(count, events.Count);
        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i, events[i].GetProperty("args").GetProperty("id").GetInt64());
        }

        Assert.Equal(count, document.RootElement.GetProperty("sussudio").GetProperty("retained").GetInt64());
    }

    [Fact]
    public void ADumpOfAWrappedRingIsTheNewestEventsOldestFirstWithTheThreadsThatAppear()
    {
        var threads = new PerfThreadNames();
        var recorder = NewRecorder(threads: threads);
        Assert.True(recorder.Start(FrameTraceBuffer.MinCapacity).Success);
        threads.Set(Environment.CurrentManagedThreadId, "unit.test.writer");
        var start = Stopwatch.GetTimestamp();
        var total = FrameTraceBuffer.MinCapacity + 1500;
        for (var i = 0; i < total; i++)
        {
            recorder.Record(FrameTraceEventKind.CaptureRead, start + i * 10L, 4, i);
        }

        using var document = Dump(recorder);
        var events = document.RootElement.GetProperty("traceEvents").EnumerateArray().ToList();
        var reads = events.Where(e => e.GetProperty("name").GetString() == "capture.read").ToList();
        Assert.Equal(FrameTraceBuffer.MinCapacity, reads.Count);
        Assert.Equal(1500, reads[0].GetProperty("args").GetProperty("id").GetInt64());
        Assert.Equal(total - 1, reads[^1].GetProperty("args").GetProperty("id").GetInt64());
        Assert.Contains(events, e => e.GetProperty("ph").GetString() == "M"
            && e.GetProperty("name").GetString() == "thread_name"
            && e.GetProperty("args").GetProperty("name").GetString() == "unit.test.writer");
        var meta = document.RootElement.GetProperty("sussudio");
        Assert.Equal(1500, meta.GetProperty("overwritten").GetInt64());
        Assert.Equal(total, meta.GetProperty("recorded").GetInt64());

        // The origin is the earliest retained timestamp, so no event is placed before the start of the axis.
        Assert.All(reads, e => Assert.True(e.GetProperty("ts").GetDouble() >= 0));
    }

    [Fact]
    public void ARingIsReusedForTheNextTraceOfTheSameCapacityWithoutLeakingOldEvents()
    {
        const int capacity = 1 << 16;
        var recorder = NewRecorder();
        Assert.True(recorder.Start(capacity).Success);
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < 100; i++)
        {
            recorder.Record(FrameTraceEventKind.CaptureRead, start + i, 1, i);
        }

        recorder.Stop();

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.True(recorder.Start(capacity).Success);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // A fresh 65,536 x 40 byte ring is 2.6 MB; reusing the frozen one allocates almost nothing.
        Assert.True(allocated < 200_000, $"starting an equal-capacity trace allocated {allocated} bytes");
        Assert.Equal(0, recorder.GetStatus().Recorded);

        var second = Stopwatch.GetTimestamp();
        for (var i = 0; i < 10; i++)
        {
            recorder.Record(FrameTraceEventKind.CaptureRead, second + i, 1, 1_000 + i);
        }

        using var document = Dump(recorder);
        var ids = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Where(e => e.GetProperty("name").GetString() == "capture.read")
            .Select(e => e.GetProperty("args").GetProperty("id").GetInt64())
            .ToList();
        Assert.Equal(Enumerable.Range(1_000, 10).Select(i => (long)i).ToList(), ids);
    }

    [Fact]
    public void ADifferentCapacityGetsAFreshRing()
    {
        var recorder = NewRecorder();
        Assert.True(recorder.Start(1 << 12).Success);
        recorder.Stop();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var second = recorder.Start(1 << 16);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(second.Success);
        Assert.Equal(1 << 16, second.Capacity);
        Assert.True(allocated >= (1 << 16) * 40L, $"expected a new ring, allocated {allocated} bytes");
    }

    [Fact]
    public void ARingIsNotReusedWhileADumpIsStillReadingIt()
    {
        const int capacity = 1 << 16;
        var recorder = NewRecorder();
        Assert.True(recorder.Start(capacity).Success);
        var start = Stopwatch.GetTimestamp();
        const int events = 5_000;
        for (var i = 0; i < events; i++)
        {
            recorder.Record(FrameTraceEventKind.CaptureRead, start + i, 1, i);
        }

        recorder.Stop();

        // The dump parks inside its first stream write, so it is mid-dump (it holds the ring) while the next trace starts.
        using var inner = new MemoryStream();
        using var blocking = new BlockingFirstWriteStream(inner);
        var dumpResult = false;
        var dump = new Thread(() => dumpResult = recorder.TryWriteChromeTrace(blocking, out _)) { IsBackground = true };
        dump.Start();
        Assert.True(blocking.Entered.Wait(TimeSpan.FromSeconds(10)), "the dump never reached its first write");

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.True(recorder.Start(capacity).Success);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated >= capacity * 40L, $"the ring was reused under a running dump ({allocated} bytes allocated)");

        blocking.Release.Set();
        Assert.True(dump.Join(TimeSpan.FromSeconds(10)));
        Assert.True(dumpResult);

        using var document = JsonDocument.Parse(inner.ToArray());
        Assert.Equal(events, document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Count(e => e.GetProperty("name").GetString() == "capture.read"));
    }

    [Fact]
    public void RetiringAThreadNameRemovesItOnlyWhileTheIdStillMapsToThatRegistration()
    {
        var names = new PerfThreadNames();
        var first = names.Set(77, "capture.read");

        // The id is reused: a new registration takes it before the old thread's retire runs.
        var second = names.Set(77, "preview.render");
        Assert.False(names.Retire(77, first));
        Assert.Equal("preview.render", names.Snapshot()[77]);

        Assert.True(names.Retire(77, second));
        Assert.Equal("preview.render" + PerfThreadNames.RetiredSuffix, names.Snapshot()[77]);

        // A repeated retire is harmless.
        Assert.False(names.Retire(77, second));
    }

    [Fact]
    public void AnIdThatIsNamedAgainForgetsItsRetiredLabel()
    {
        var names = new PerfThreadNames();
        var entry = names.Set(12, "capture.read");
        Assert.True(names.Retire(12, entry));
        Assert.Equal("capture.read (retired)", names.Snapshot()[12]);

        names.Set(12, "audio.render");
        Assert.Equal("audio.render", names.Snapshot()[12]);
    }

    [Fact]
    public void RetiredLabelsAreBoundedAndKeepTheNewest()
    {
        var names = new PerfThreadNames();
        var total = PerfThreadNames.MaxRetiredLabels + 50;
        for (var id = 1; id <= total; id++)
        {
            names.Retire(id, names.Set(id, "role-" + id));
        }

        var snapshot = names.Snapshot();
        Assert.Equal(PerfThreadNames.MaxRetiredLabels, snapshot.Count);
        Assert.False(snapshot.ContainsKey(1));
        Assert.Equal("role-" + total + PerfThreadNames.RetiredSuffix, snapshot[total]);
    }

    [Fact]
    public void ADumpNamesAnExitedThreadThatLeftEventsAsRetiredAndSkipsOnesWithNoEvents()
    {
        var threads = new PerfThreadNames();
        var recorder = NewRecorder(threads: threads);
        Assert.True(recorder.Start(0).Success);

        var writerId = 0;
        var writer = new Thread(() =>
        {
            writerId = Environment.CurrentManagedThreadId;
            var entry = threads.Set(writerId, "capture.read");
            recorder.Record(FrameTraceEventKind.CaptureRead, Stopwatch.GetTimestamp(), 5, 1);
            threads.Retire(writerId, entry);
        });
        writer.Start();
        writer.Join();

        // Another thread registered and retired without ever recording an event.
        var silentId = writerId + 1000;
        threads.Retire(silentId, threads.Set(silentId, "audio.capture"));

        using var document = Dump(recorder);
        var named = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Where(e => e.GetProperty("ph").GetString() == "M" && e.GetProperty("name").GetString() == "thread_name")
            .ToDictionary(e => e.GetProperty("tid").GetInt32(), e => e.GetProperty("args").GetProperty("name").GetString());
        Assert.Equal("capture.read (retired)", named[writerId]);
        Assert.DoesNotContain(silentId, named.Keys);
    }

    [Fact]
    public void ADisposedHotThreadRegistrationRetiresItsTraceName()
    {
        var managedId = 0;
        var thread = new Thread(() =>
        {
            managedId = Environment.CurrentManagedThreadId;
            using var registration = HotThreads.Register("unit.test.hot-thread");
            Assert.Equal("unit.test.hot-thread", PerfThreadNames.Default.Snapshot()[managedId]);
        });
        thread.Start();
        thread.Join();

        Assert.Equal("unit.test.hot-thread" + PerfThreadNames.RetiredSuffix, PerfThreadNames.Default.Snapshot()[managedId]);
    }

    // The thread report's Priority string is read by tools: a named Win32 priority keeps its name, and any other value
    // (what MMCSS sets, for example 13 or 19) is labeled by its source with the raw number kept, never a bare number.
    [Theory]
    [InlineData(-15, false, "Idle")]
    [InlineData(-2, false, "Lowest")]
    [InlineData(-1, false, "BelowNormal")]
    [InlineData(0, false, "Normal")]
    [InlineData(1, false, "AboveNormal")]
    [InlineData(2, false, "Highest")]
    [InlineData(15, true, "TimeCritical")]
    [InlineData(13, true, "mmcss/13")]
    [InlineData(19, true, "mmcss/19")]
    [InlineData(7, false, "raw/7")]
    [InlineData(int.MaxValue, true, "Unknown")]
    public void ThreadPriorityIsNamedWhenItMapsAndLabeledWithItsRawNumberOtherwise(int priority, bool mmcss, string expected) =>
        Assert.Equal(expected, HotThreads.PriorityName(priority, mmcss));
}

internal static class FrameTraceBufferTestExtensions
{
    /// <summary>Reads every retained event through the production chunked reader, oldest first.</summary>
    public static List<FrameTraceEvent> ReadAll(this FrameTraceBuffer buffer, int chunkSize = 1024, Action<long>? afterFieldsCopied = null)
    {
        var events = new List<FrameTraceEvent>();
        var chunk = new FrameTraceEvent[chunkSize];
        var cursor = buffer.BeginRead();
        int count;
        while ((count = buffer.ReadChunk(ref cursor, chunk, afterFieldsCopied)) > 0)
        {
            for (var i = 0; i < count; i++)
            {
                events.Add(chunk[i]);
            }
        }

        return events;
    }
}

/// <summary>A write-only stream whose first write parks until released, so a test can hold a dump mid-flight.</summary>
internal sealed class BlockingFirstWriteStream : Stream
{
    private readonly Stream _inner;
    private int _writes;

    public BlockingFirstWriteStream(Stream inner) => _inner = inner;

    public ManualResetEventSlim Entered { get; } = new(false);

    public ManualResetEventSlim Release { get; } = new(false);

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (Interlocked.Increment(ref _writes) == 1)
        {
            Entered.Set();
            Release.Wait(TimeSpan.FromSeconds(30));
        }

        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (Interlocked.Increment(ref _writes) == 1)
        {
            Entered.Set();
            Release.Wait(TimeSpan.FromSeconds(30));
        }

        _inner.Write(buffer);
    }

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Entered.Dispose();
            Release.Dispose();
        }

        base.Dispose(disposing);
    }
}
