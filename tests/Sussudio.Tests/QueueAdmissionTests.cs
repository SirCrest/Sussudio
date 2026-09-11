using System.Reflection;
using System.Threading.Channels;
using Xunit;

namespace Sussudio.Tests;

// QueueAdmission is the claim/high-water/rollback sequence that LibAvRecordingSink
// and FlashbackEncoderSink share on their bounded publish paths. The sink contract
// tests assert only that each lane delegates to it; the behaviour it guarantees for
// all of them is pinned here.
public sealed class QueueAdmissionTests
{
    [Fact]
    public void AcceptedWritesKeepTheirClaimAndRaiseTheHighWaterMark()
    {
        var spy = new RollbackSpy();
        var lane = TrackedLane(capacity: 2, spy);

        Assert.True(lane.Write(1));
        Assert.True(lane.Write(2));

        Assert.Equal(2, lane.Depth);
        Assert.Equal(2, lane.MaxDepth);
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public void ARefusedWriteRollsTheClaimBackAndTagsTheLane()
    {
        var spy = new RollbackSpy();
        var lane = TrackedLane(capacity: 1, spy);

        Assert.True(lane.Write(1));
        Assert.False(lane.Write(2));

        // The refused claim is handed back, so depth again reflects the single
        // queued packet. The high-water mark tracks admitted depth, not attempted
        // depth, so the rejected claim must not leave a phantom peak behind.
        Assert.Equal(1, lane.Depth);
        Assert.Equal(1, lane.MaxDepth);
        Assert.Equal(1, spy.Calls);
        Assert.Equal("video_write_failed", spy.LastQueueName);
    }

    [Fact]
    public void TheHighWaterMarkNeverRegressesWhenTheLaneDrains()
    {
        var spy = new RollbackSpy();
        var lane = TrackedLane(capacity: 4, spy);

        Assert.True(lane.Write(1));
        Assert.True(lane.Write(2));
        Assert.True(lane.Write(3));
        lane.Drain();
        Assert.True(lane.Write(4));

        Assert.Equal(3, lane.Depth);
        Assert.Equal(3, lane.MaxDepth);
    }

    [Fact]
    public void TheAudioOverloadAdmitsWithoutRecordingAHighWaterMark()
    {
        var spy = new RollbackSpy();
        var lane = UntrackedLane(capacity: 1, spy);

        Assert.True(lane.Write(1, "microphone"));
        Assert.False(lane.Write(2, "microphone_after_evict"));

        Assert.Equal(1, lane.Depth);
        Assert.Equal(1, spy.Calls);
        Assert.Equal("microphone_after_evict_write_failed", spy.LastQueueName);
    }

    private static TrackedLaneHarness TrackedLane(int capacity, RollbackSpy spy)
        => new(Channel.CreateBounded<int>(capacity), spy);

    private static UntrackedLaneHarness UntrackedLane(int capacity, RollbackSpy spy)
        => new(Channel.CreateBounded<int>(capacity), spy);

    // MethodInfo.Invoke copies `ref` arguments back into the argument array, so the
    // harnesses keep one array alive and read the lane counters out of it.
    private sealed class TrackedLaneHarness(Channel<int> queue, RollbackSpy spy)
    {
        private readonly object?[] _args = [queue, 0, 0, 0, "video", spy.AsRollback()];

        public int Depth => (int)_args[2]!;

        public int MaxDepth => (int)_args[3]!;

        public bool Write(int packet)
        {
            _args[1] = packet;
            return (bool)Method(parameterCount: 6).Invoke(null, _args)!;
        }

        public void Drain()
        {
            Assert.True(queue.Reader.TryRead(out _));
            _args[2] = Depth - 1;
        }
    }

    private sealed class UntrackedLaneHarness(Channel<int> queue, RollbackSpy spy)
    {
        private readonly object?[] _args = [queue, 0, 0, string.Empty, spy.AsRollback()];

        public int Depth => (int)_args[2]!;

        public bool Write(int packet, string queueName)
        {
            _args[1] = packet;
            _args[3] = queueName;
            return (bool)Method(parameterCount: 5).Invoke(null, _args)!;
        }
    }

    private sealed class RollbackSpy
    {
        public int Calls { get; private set; }

        public string? LastQueueName { get; private set; }

        public void Rollback(ref int depth, string queueName)
        {
            Calls++;
            LastQueueName = queueName;
            depth--;
        }

        public Delegate AsRollback()
            => Delegate.CreateDelegate(
                AdmissionType.GetNestedType("DepthRollback", BindingFlags.NonPublic)!,
                this,
                nameof(Rollback));
    }

    private static Type AdmissionType
        => SussudioAssembly.Load().GetType("Sussudio.Services.Runtime.QueueAdmission", throwOnError: true)!;

    private static MethodInfo Method(int parameterCount)
        => AdmissionType.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Single(candidate => candidate.Name == "TryWrite" && candidate.GetParameters().Length == parameterCount)
            .MakeGenericMethod(typeof(int));
}
