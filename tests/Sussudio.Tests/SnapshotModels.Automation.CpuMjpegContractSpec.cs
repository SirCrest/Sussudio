using System;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    private static readonly string[] AutomationSnapshotCpuMjpegMetricProperties =
    {
        "MjpegDecoderCount",
        "MjpegReorderSampleCount",
        "MjpegPipelineSampleCount",
        "MjpegTotalDecoded",
        "MjpegTotalEmitted",
        "MjpegTotalDropped",
        "MjpegCompressedFramesQueued",
        "MjpegCompressedFramesDequeued",
        "MjpegCompressedDropsQueueFull",
        "MjpegCompressedDropsByteBudget",
        "MjpegCompressedDropsDisposed",
        "MjpegDecodeFailures",
        "MjpegReorderCollisions",
        "MjpegEmitFailures",
        "MjpegCompressedQueueDepth",
        "MjpegCompressedQueueBytes",
        "MjpegCompressedQueueByteBudget",
        "MjpegReorderSkips",
        "MjpegReorderBufferDepth",
    };

    private static readonly string[] MjpegDecoderAutomationSnapshotProperties =
    {
        "WorkerIndex",
        "SampleCount",
        "AvgMs",
        "P95Ms",
        "MaxMs",
    };

    private static void AssertAutomationSnapshotCpuMjpegMetricContract(Type snapshotType)
    {
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertContains(automationSnapshotText, "public int MjpegDecodeSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public int MjpegDecoderCount { get; init; }");
        AssertContains(automationSnapshotText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderAutomationSnapshot>();");
        AssertContains(automationSnapshotText, "public bool MjpegPreviewJitterEnabled { get; init; }");

        foreach (var propertyName in AutomationSnapshotCpuMjpegMetricProperties)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }

        var decoderType = RequireType("Sussudio.Models.MjpegDecoderAutomationSnapshot");
        var perDecoderProperty = snapshotType.GetProperty("MjpegPerDecoder")
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder missing.");
        var elementType = perDecoderProperty.PropertyType.GetElementType()
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder element type missing.");
        AssertEqual(decoderType, elementType, "AutomationSnapshot.MjpegPerDecoder[] element type");

        foreach (var propertyName in MjpegDecoderAutomationSnapshotProperties)
        {
            AssertNotNull(decoderType.GetProperty(propertyName), $"MjpegDecoderAutomationSnapshot.{propertyName}");
        }
    }

    private static void AssertAutomationSnapshotProperties(Type snapshotType, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }
    }
}
