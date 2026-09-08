using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackExportPlannerTests
{
    [Fact]
    public void RelativeRangeClampsToCurrentBuffer()
    {
        var result = ResolveRange(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(45), null, null, 10, 30);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Equal(TimeSpan.FromSeconds(10), Get<TimeSpan>(result, "InPoint"));
        Assert.Equal(TimeSpan.FromSeconds(40), Get<TimeSpan>(result, "OutPoint"));
    }

    [Fact]
    public void AbsoluteRangeRejectsEvictedInPoint()
    {
        var result = ResolveRange(null, null, TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(20), 10, 30);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Equal("Flashback export in point has been evicted from the buffer.", Get<string>(result, "FailureMessage"));
    }

    [Fact]
    public void AbsoluteRangeRejectsEvictedOutPoint()
    {
        var result = ResolveRange(null, null, TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(10), 10, 30);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Equal("Flashback export out point has been evicted from the buffer.", Get<string>(result, "FailureMessage"));
    }

    [Fact]
    public void AbsoluteRangeRejectsEmptyRange()
    {
        var result = ResolveRange(null, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20), 10, 30);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Equal("Flashback export range is empty or invalid.", Get<string>(result, "FailureMessage"));
    }

    [Fact]
    public void RelativeRangeSaturatesPtsOverflow()
    {
        var result = ResolveRange(
            TimeSpan.FromSeconds(2),
            null,
            null,
            null,
            TimeSpan.MaxValue - TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(10));

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Equal(TimeSpan.MaxValue, Get<TimeSpan>(result, "InPoint"));
    }

    [Fact]
    public void RelativeRangeKeepsOpenEndedOutPoint()
    {
        var result = ResolveRange(TimeSpan.FromSeconds(2), null, null, null, 10, 30);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Equal(TimeSpan.MaxValue, Get<TimeSpan>(result, "OutPoint"));
    }

    [Fact]
    public void LastNRangeUsesCurrentLiveTail()
    {
        var planner = PlannerType;
        var timing = New(TimingType, TimeSpan.FromSeconds(100), TimeSpan.FromSeconds(30));
        var result = Invoke(planner, "ResolveLastNRange", 5d, timing);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Equal(TimeSpan.FromSeconds(125), Get<TimeSpan>(result, "InPoint"));
        Assert.Equal(TimeSpan.MaxValue, Get<TimeSpan>(result, "OutPoint"));
    }

    [Fact]
    public void StablePathsAreSampledOnlyForFailureOrEmptyRotation()
    {
        Assert.False(NeedsStablePaths(null));
        Assert.False(NeedsStablePaths(ForceRotate("Completed", new[] { "rotated.ts" })));
        Assert.True(NeedsStablePaths(ForceRotate("Failed")));
        Assert.True(NeedsStablePaths(ForceRotate("CommittedPending")));
        Assert.True(NeedsStablePaths(ForceRotate("Completed", Array.Empty<string>())));
        Assert.True(NeedsStablePaths(ForceRotate("CanceledBeforeCommit")));
    }

    [Fact]
    public void FailedForceRotateUsesExportFailureText()
    {
        var plan = PlanLiveEdge(ForceRotate("Failed"), requireCompleteLiveEdge: false, new[] { "one.ts" });

        Assert.Equal("ForceRotateFailed", Get<object>(plan, "FailureKind").ToString());
        Assert.Equal("Flashback export failed: live-edge segment rotation failed.", Get<string>(plan, "FailureMessage"));
    }

    [Fact]
    public void FailedForceRotatePreservesProvidedArtifacts()
    {
        var plan = PlanLiveEdge(
            ForceRotate("Failed"),
            requireCompleteLiveEdge: false,
            new[] { "fallback.ts" },
            new[] { "artifact.ts" });

        Assert.Equal(new[] { "artifact.ts" }, Get<IReadOnlyList<string>>(plan, "PreservedArtifacts"));
    }

    [Fact]
    public void CommittedPendingForceRotateUsesExportFailureText()
    {
        var plan = PlanLiveEdge(ForceRotate("CommittedPending"), requireCompleteLiveEdge: false, new[] { "one.ts" });

        Assert.Equal("ForceRotateCommittedPending", Get<object>(plan, "FailureKind").ToString());
        Assert.Equal("Flashback export failed: live-edge segment rotation committed but did not complete before timeout.", Get<string>(plan, "FailureMessage"));
    }

    [Fact]
    public void EmptyCompletedRotationFallsBackToCompletedSegments()
    {
        var plan = PlanLiveEdge(ForceRotate("Completed", Array.Empty<string>()), requireCompleteLiveEdge: false, new[] { "one.ts" });

        Assert.Null(GetNullable(plan, "FailureMessage"));
        Assert.True(Get<bool>(plan, "ForceRotateFallbackUsed"));
        Assert.Equal(new[] { "one.ts" }, Get<IReadOnlyList<string>>(plan, "SegmentPaths"));
    }

    [Fact]
    public void CompletedRotationUsesReturnedSegments()
    {
        var plan = PlanLiveEdge(ForceRotate("Completed", new[] { "rotated.ts" }), requireCompleteLiveEdge: false, new[] { "stable.ts" });

        Assert.False(Get<bool>(plan, "ForceRotateFallbackUsed"));
        Assert.Equal(new[] { "rotated.ts" }, Get<IReadOnlyList<string>>(plan, "SegmentPaths"));
    }

    [Fact]
    public void CanceledBeforeCommitFallsBackToCompletedSegments()
    {
        var plan = PlanLiveEdge(ForceRotate("CanceledBeforeCommit"), requireCompleteLiveEdge: false, new[] { "stable.ts" });

        Assert.Null(GetNullable(plan, "FailureMessage"));
        Assert.True(Get<bool>(plan, "ForceRotateFallbackUsed"));
        Assert.Equal(new[] { "stable.ts" }, Get<IReadOnlyList<string>>(plan, "SegmentPaths"));
    }

    [Fact]
    public void RequiredLiveEdgeRejectsEmptyCompletedRotation()
    {
        var plan = PlanLiveEdge(ForceRotate("Completed", Array.Empty<string>()), requireCompleteLiveEdge: true, new[] { "one.ts" });

        Assert.Equal("IncompleteLiveEdge", Get<object>(plan, "FailureKind").ToString());
        Assert.Equal("Flashback recording finalize failed: live-edge segment was not closed before timeout.", Get<string>(plan, "FailureMessage"));
    }

    [Fact]
    public void RequiredLiveEdgeUsesRecordingMessageForCommittedPendingRotation()
    {
        var plan = PlanLiveEdge(ForceRotate("CommittedPending"), requireCompleteLiveEdge: true, new[] { "one.ts" });

        Assert.Equal("ForceRotateCommittedPending", Get<object>(plan, "FailureKind").ToString());
        Assert.Equal("Flashback recording finalize failed: live-edge segment was not closed before timeout.", Get<string>(plan, "FailureMessage"));
    }

    [Fact]
    public void RequestMapsSegmentMetadataAndRepairsReversedPts()
    {
        var path = New(PathSnapshotType, "one.ts", "C:\\segments\\one.ts");
        var metadata = New(MetadataType, "C:\\segments\\one.ts", 2000L, 1000L);
        var requestPlan = CreateRequest(
            new[] { path },
            new[] { metadata },
            activeFilePath: null);

        var request = Get<object>(requestPlan, "Request");
        var segment = Assert.Single(Get<System.Collections.IEnumerable>(request, "Segments").Cast<object>());
        Assert.Equal(TimeSpan.FromSeconds(2), Get<TimeSpan>(segment, "StartPts"));
        Assert.Equal(TimeSpan.FromSeconds(2), Get<TimeSpan>(segment, "EndPts"));
    }

    [Fact]
    public void RequestSkeletonPreservesExportPolicy()
    {
        var requestPlan = CreateRequest(null, Array.Empty<object>(), "active.ts");
        var request = Get<object>(requestPlan, "Request");

        Assert.False(Get<bool>(request, "FastStart"));
        Assert.True(Get<bool>(request, "Force"));
        Assert.Equal(TimeSpan.Zero, Get<TimeSpan>(request, "InPoint"));
        Assert.Equal(TimeSpan.FromSeconds(10), Get<TimeSpan>(request, "OutPoint"));
        Assert.Equal("output.mp4", Get<string>(request, "OutputPath"));
    }

    [Theory]
    [InlineData("active.ts")]
    [InlineData("active.mp4")]
    public void RequestUsesActiveFileOnlyWhenNoSegmentsExist(string activePath)
    {
        var requestPlan = CreateRequest(null, Array.Empty<object>(), activePath);

        Assert.True(Get<bool>(requestPlan, "UsesActiveFileFallback"));
        var request = Get<object>(requestPlan, "Request");
        Assert.Equal(activePath, Get<string>(request, "InputPath"));
        Assert.Null(GetNullable(request, "Segments"));

        var segment = New(PathSnapshotType, "one.ts", "C:\\segments\\one.ts");
        var segmentedPlan = CreateRequest(new[] { segment }, Array.Empty<object>(), activePath);
        Assert.False(Get<bool>(segmentedPlan, "UsesActiveFileFallback"));
        var segmentedRequest = Get<object>(segmentedPlan, "Request");
        Assert.Null(GetNullable(segmentedRequest, "InputPath"));
        Assert.Single(Get<System.Collections.IEnumerable>(segmentedRequest, "Segments").Cast<object>());
    }

    [Fact]
    public void RequestLeavesPtsUnsetWhenSegmentMetadataIsMissing()
    {
        var path = New(PathSnapshotType, "one.ts", "C:\\segments\\one.ts");
        var requestPlan = CreateRequest(new[] { path }, Array.Empty<object>(), activeFilePath: null);
        var segment = Assert.Single(Get<System.Collections.IEnumerable>(Get<object>(requestPlan, "Request"), "Segments").Cast<object>());

        Assert.Null(GetNullable(segment, "StartPts"));
        Assert.Null(GetNullable(segment, "EndPts"));
    }

    [Fact]
    public void RequestRejectsNoSegmentsWithoutActiveFile()
    {
        var requestPlan = CreateRequest(null, Array.Empty<object>(), activeFilePath: null);

        Assert.Null(GetNullable(requestPlan, "Request"));
        Assert.Equal("Flashback buffer has no active file", Get<string>(requestPlan, "FailureMessage"));
    }

    private static object ResolveRange(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts,
        double validStartSeconds,
        double bufferedDurationSeconds)
        => ResolveRange(
            inPoint,
            outPoint,
            inPointFilePts,
            outPointFilePts,
            TimeSpan.FromSeconds(validStartSeconds),
            TimeSpan.FromSeconds(bufferedDurationSeconds));

    private static object ResolveRange(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts,
        TimeSpan validStart,
        TimeSpan bufferedDuration)
        => Invoke(
            PlannerType,
            "ResolveRange",
            New(RangeSelectionType, inPoint, outPoint, inPointFilePts, outPointFilePts),
            New(TimingType, validStart, bufferedDuration));

    private static object PlanLiveEdge(
        object forceRotateResult,
        bool requireCompleteLiveEdge,
        IReadOnlyList<string> stablePaths,
        IReadOnlyList<string>? preservedArtifacts = null)
        => Invoke(
            PlannerType,
            "PlanLiveEdge",
            forceRotateResult,
            requireCompleteLiveEdge,
            stablePaths,
            preservedArtifacts ?? stablePaths);

    private static bool NeedsStablePaths(object? forceRotateResult)
        => (bool)Invoke(PlannerType, "NeedsStableSegmentPaths", forceRotateResult);

    private static object CreateRequest(object[]? paths, object[] metadata, string? activeFilePath)
        => Invoke(
            PlannerType,
            "CreateRequest",
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            "output.mp4",
            true,
            CreateTypedArray(PathSnapshotType, paths),
            CreateTypedArray(MetadataType, metadata),
            activeFilePath);

    private static object ForceRotate(string status, IReadOnlyList<string>? paths = null)
    {
        var method = ForceRotateResultType.GetMethod(status, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"FlashbackForceRotateResult.{status} not found.");
        return paths == null
            ? method.Invoke(null, null)!
            : method.Invoke(null, new object[] { paths })!;
    }

    private static Array? CreateTypedArray(Type elementType, object[]? values)
    {
        if (values == null)
        {
            return null;
        }

        var array = Array.CreateInstance(elementType, values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            array.SetValue(values[index], index);
        }

        return array;
    }

    private static object Invoke(Type type, string methodName, params object?[] arguments)
        => type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
               ?.Invoke(null, arguments)
           ?? throw new InvalidOperationException($"{type.Name}.{methodName} returned null or was not found.");

    private static object New(Type type, params object?[] arguments)
        => Activator.CreateInstance(type, arguments)
           ?? throw new InvalidOperationException($"Unable to create {type.Name}.");

    private static T Get<T>(object instance, string propertyName)
        => (T)(instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
               ?.GetValue(instance)
           ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was null or missing."));

    private static object? GetNullable(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(instance);

    private static Type PlannerType => TypeOf("Sussudio.Services.Flashback.FlashbackExportPlanner");
    private static Type RangeSelectionType => TypeOf("Sussudio.Services.Flashback.FlashbackExportRangeSelection");
    private static Type TimingType => TypeOf("Sussudio.Services.Flashback.FlashbackExportBufferTiming");
    private static Type PathSnapshotType => TypeOf("Sussudio.Services.Flashback.FlashbackExportPathSnapshot");
    private static Type MetadataType => TypeOf("Sussudio.Services.Flashback.FlashbackExportSegmentMetadata");
    private static Type ForceRotateResultType => TypeOf("Sussudio.Models.FlashbackForceRotateResult");

    private static Type TypeOf(string name)
        => SussudioAssembly.Load().GetType(name)
           ?? throw new InvalidOperationException($"{name} not found.");
}
