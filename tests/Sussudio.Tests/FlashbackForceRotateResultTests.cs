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
    private static Type Model => TypeOf("Sussudio.Models.FlashbackForceRotateResult");
    private static Type TypeOf(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
}
