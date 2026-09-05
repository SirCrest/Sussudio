using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class AppEmergencyFinalizationTests
{
    [Fact]
    public void SuccessfulStopDoesNotMarkRecordingUnresolved()
    {
        var starts = 0;
        var messages = new List<string>();

        Finalize(() => { starts++; return Task.CompletedTask; }, messages.Add);

        Assert.Equal(1, starts);
        Assert.Empty(messages);
    }

    [Theory]
    [InlineData("UI")]
    [InlineData("AppDomain")]
    public void SynchronousStartFailureMarksRecordingUnresolved(string source)
    {
        var starts = 0;
        var messages = new List<string>();

        Finalize(() => { starts++; throw new InvalidOperationException("enqueue rejected"); }, messages.Add, source);

        Assert.Equal(1, starts);
        Assert.Equal($"Emergency recording finalization failed after {source}: enqueue rejected", Assert.Single(messages));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FaultedStopMarksFailureIncludingTaskTimeout(bool timeoutFailure)
    {
        Exception failure = timeoutFailure
            ? new TimeoutException("encoder drain failed")
            : new IOException("encoder drain failed");
        var messages = new List<string>();

        Finalize(() => Task.FromException(failure), messages.Add);

        Assert.Equal("Emergency recording finalization failed after UI: encoder drain failed", Assert.Single(messages));
    }

    [Fact]
    public void CanceledStopMarksRecordingUnresolved()
    {
        var messages = new List<string>();

        Finalize(() => Task.FromCanceled(new CancellationToken(true)), messages.Add);

        Assert.StartsWith("Emergency recording finalization failed after UI:", Assert.Single(messages));
    }

    [Fact]
    public void ObservationTimeoutLeavesStopTaskRunningAndMarksOnce()
    {
        var stop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var messages = new List<string>();

        Finalize(() => stop.Task, messages.Add);

        Assert.Contains("remained unresolved", Assert.Single(messages));
        Assert.False(stop.Task.IsCompleted);
        stop.SetResult();
        Assert.Single(messages);
    }

    [Fact]
    public void MarkerFailureIsNotRetried()
    {
        var marks = 0;
        var failure = new IOException("recovery storage unavailable");

        var invocation = Assert.Throws<TargetInvocationException>(() => Finalize(
            () => new TaskCompletionSource().Task,
            _ => { marks++; throw failure; }));

        Assert.Same(failure, invocation.InnerException);
        Assert.Equal(1, marks);
    }

    private static void Finalize(Func<Task> stop, Action<string> mark, string source = "UI")
    {
        var method = SussudioAssembly.Load().GetType("Sussudio.App", throwOnError: true)!
            .GetMethod("FinalizeRecordingForEmergency", BindingFlags.Static | BindingFlags.NonPublic)!;
        method.Invoke(null, new object[] { stop, mark, source, TimeSpan.Zero });
    }
}
