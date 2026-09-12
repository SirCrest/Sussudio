using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackForceRotateResultTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CompletionPreservesTypedOutcomeBeforeOrAfterCommit(bool committed, bool failed)
    {
        var request = CreateRequest();
        var expected = Result(failed ? "Failed" : "Completed", failed ? null : new[] { "segment.mp4" });
        if (committed) Assert.True(Invoke<bool>(request, "TryBeginCommit"));

        Complete(request, expected);

        Assert.Same(expected, Completion(request));
        Assert.True(Property<bool>(request, "IsCompleted"));
        Assert.False(Invoke<bool>(request, "TryCancel"));
        Assert.False(Invoke<bool>(request, "TryBeginCommit"));
        Complete(request, Result("Completed", new[] { "late.mp4" }));
        Assert.Same(expected, Completion(request));
    }

    [Fact]
    public void CancellationWinsBeforeCommitAndCannotBeOverwrittenByLaterFailure()
    {
        var request = CreateRequest();

        Assert.True(Invoke<bool>(request, "TryCancel"));
        var canceled = Completion(request);

        Assert.Equal("CanceledBeforeCommit", Property<object>(canceled, "Status").ToString());
        Assert.Empty(Property<IReadOnlyList<string>>(canceled, "SegmentPaths"));
        Assert.False(Invoke<bool>(request, "TryBeginCommit"));
        request.GetType().GetMethod("Fail")!.Invoke(request, null);
        Complete(request, Result("Completed", new[] { "late.mp4" }));
        Assert.Same(canceled, Completion(request));
    }

    [Fact]
    public void CommitRejectsCancellationAndLeavesCompletionToTheOwner()
    {
        var request = CreateRequest();

        Assert.True(Invoke<bool>(request, "TryBeginCommit"));
        Assert.False(Invoke<bool>(request, "TryCancel"));
        Assert.False(Property<bool>(request, "IsCompleted"));
        Assert.False(Property<Task>(request, "Task").IsCompleted);
        request.GetType().GetMethod("Fail")!.Invoke(request, null);

        Assert.Equal("Failed", Property<object>(Completion(request), "Status").ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedCompletionMakesPlannerRejectEvenWhenStablePathsExist(bool requireCompleteLiveEdge)
    {
        var request = CreateRequest();
        request.GetType().GetMethod("Fail")!.Invoke(request, null);
        var preserved = new[] { "original.mp4" };

        var plan = Planner.GetMethod("PlanLiveEdge", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { Completion(request), requireCompleteLiveEdge, preserved, preserved })!;

        Assert.Equal("ForceRotateFailed", Property<object>(plan, "FailureKind").ToString());
        Assert.Equal("Flashback export failed: live-edge segment rotation failed.", Property<string>(plan, "FailureMessage"));
        Assert.Same(preserved, Property<IReadOnlyList<string>>(plan, "PreservedArtifacts"));
        Assert.False(Property<bool>(plan, "ForceRotateFallbackUsed"));
        Assert.Null(plan.GetType().GetProperty("SegmentPaths")!.GetValue(plan));
    }

    [Fact]
    public void SuccessfulEmptyCompletionRetainsTheOptionalStablePathFallback()
    {
        var request = CreateRequest();
        Complete(request, Result("Completed", Array.Empty<string>()));
        var stable = new[] { "stable.mp4" };

        var plan = Planner.GetMethod("PlanLiveEdge", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { Completion(request), false, stable, stable })!;

        Assert.Equal("None", Property<object>(plan, "FailureKind").ToString());
        Assert.Same(stable, Property<IReadOnlyList<string>>(plan, "SegmentPaths"));
        Assert.True(Property<bool>(plan, "ForceRotateFallbackUsed"));
    }

    private static object CreateRequest()
        => Activator.CreateInstance(Sink.GetNestedType("ForceRotateRequest", BindingFlags.NonPublic)!, "prepared.mp4")!;

    private static object Result(string status, IReadOnlyList<string>? paths = null)
        => Model.GetMethod(status)!.Invoke(null, paths == null ? null : new object[] { paths })!;

    private static void Complete(object request, object result)
        => request.GetType().GetMethod("Complete")!.Invoke(request, new[] { result });

    private static object Completion(object request)
    {
        var task = Property<Task>(request, "Task");
        Assert.True(task.IsCompletedSuccessfully);
        return Property<object>(task, "Result");
    }

    private static T Invoke<T>(object target, string method)
        => (T)target.GetType().GetMethod(method)!.Invoke(target, null)!;

    private static T Property<T>(object target, string property)
        => (T)target.GetType().GetProperty(property)!.GetValue(target)!;

    private static Type Sink => TypeOf("Sussudio.Services.Flashback.FlashbackEncoderSink");
    private static Type Planner => TypeOf("Sussudio.Services.Flashback.FlashbackExportPlanner");
    private static Type Model => TypeOf("Sussudio.Models.FlashbackForceRotateResult");
    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
}
