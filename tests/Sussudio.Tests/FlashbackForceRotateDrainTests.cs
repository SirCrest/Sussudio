using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackForceRotateDrainTests
{
    [Theory]
    [InlineData(0, 128, 1)]
    [InlineData(1, 128, 1)]
    [InlineData(128, 128, 1)]
    [InlineData(129, 128, 2)]
    [InlineData(384, 128, 3)]
    [InlineData(17, 16, 2)]
    [InlineData(49, 24, 3)]
    public void ProducerTrickleCannotExceedStartingQueueBound(int queuedAtStart, int batchLimit, int expectedBatches)
    {
        var calls = 0;
        var result = Drain(CreateRequest(), queuedAtStart, batchLimit, () => { calls++; return true; }, initialRounds: 5);

        Assert.True(result.CanContinue);
        Assert.Equal(expectedBatches, calls);
        Assert.Equal(5 + expectedBatches, result.InFlightRounds);
    }

    [Fact]
    public void EmptyQueueStopsWithoutCountingAnInFlightBatch()
    {
        var calls = 0;
        var result = Drain(CreateRequest(), 256, 128, () => { calls++; return false; }, initialRounds: 5);

        Assert.True(result.CanContinue);
        Assert.Equal(1, calls);
        Assert.Equal(5, result.InFlightRounds);
    }

    [Fact]
    public void QueueExhaustionStopsBeforeTheStartingBound()
    {
        var calls = 0;
        var result = Drain(CreateRequest(), 512, 128, () => ++calls <= 2);

        Assert.True(result.CanContinue);
        Assert.Equal(3, calls);
        Assert.Equal(2, result.InFlightRounds);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void CancellationDuringABatchStopsThePhase(bool batchMadeProgress, int expectedRounds)
    {
        var request = CreateRequest();
        var calls = 0;
        var result = Drain(request, 512, 128, () =>
        {
            calls++;
            Assert.True((bool)request.GetType().GetMethod("TryCancel")!.Invoke(request, null)!);
            return batchMadeProgress;
        });

        Assert.False(result.CanContinue);
        Assert.Equal(1, calls);
        Assert.Equal(expectedRounds, result.InFlightRounds);
    }

    [Fact]
    public void CompletedRequestStopsAfterTheCurrentBatch()
    {
        var request = CreateRequest();
        var calls = 0;
        var result = Drain(request, 512, 128, () =>
        {
            calls++;
            request.GetType().GetMethod("CompleteEmpty")!.Invoke(request, null);
            return true;
        });

        Assert.False(result.CanContinue);
        Assert.Equal(1, calls);
        Assert.Equal(1, result.InFlightRounds);
    }

    [Fact]
    public void DrainFailurePropagatesWithoutAnotherAttempt()
    {
        var failure = new InvalidOperationException("drain failed");
        var calls = 0;
        var invocation = Assert.Throws<TargetInvocationException>(() => Drain(CreateRequest(), 512, 128, () =>
        {
            calls++;
            throw failure;
        }));

        Assert.Same(failure, invocation.InnerException);
        Assert.Equal(1, calls);
    }

    private static (bool CanContinue, int InFlightRounds) Drain(
        object request, int queuedAtStart, int batchLimit, Func<bool> drainBatch, int initialRounds = 0)
    {
        var method = SinkType.GetMethod("TryDrainForceRotatePhase", BindingFlags.Static | BindingFlags.NonPublic)!;
        object[] arguments = { request, queuedAtStart, batchLimit, "test", drainBatch, initialRounds };
        var canContinue = (bool)method.Invoke(null, arguments)!;
        return (canContinue, (int)arguments[5]);
    }

    private static object CreateRequest()
        => Activator.CreateInstance(SinkType.GetNestedType("ForceRotateRequest", BindingFlags.NonPublic)!, "prepared.ts")!;

    private static Type SinkType
        => SussudioAssembly.Load().GetType("Sussudio.Services.Flashback.FlashbackEncoderSink", throwOnError: true)!;
}
