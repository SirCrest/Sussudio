static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership()
    {
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Result.cs")
            .Replace("\r\n", "\n");
        var compositionText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Composition.cs")
            .Replace("\r\n", "\n");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewD3DResult.cs")
            .Replace("\r\n", "\n");
        var previewVisualCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewResult.cs")
            .Replace("\r\n", "\n");

        AssertContains(compositionText, "Preview: BuildPreviewResultProjection(analysis)");
        AssertContains(compositionText, "PreviewD3D: BuildPreviewD3DResultProjection(analysis)");
        AssertContains(compositionText, "PreviewVisualCadence: BuildPreviewVisualCadenceResultProjection(analysis)");
        AssertContains(resultText, "var previewResult = resultProjections.Preview;");
        AssertContains(resultText, "var previewD3DResult = resultProjections.PreviewD3D;");
        AssertContains(resultText, "var previewVisualCadenceResult = resultProjections.PreviewVisualCadence;");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewResultProjection BuildPreviewResultProjection(");
        AssertContains(previewD3DResultText, "private readonly record struct DiagnosticSessionPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "private static DiagnosticSessionPreviewD3DResultProjection BuildPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "var previewD3DMetrics = analysis.PreviewD3DMetrics;");
        AssertContains(previewVisualCadenceResultText, "private readonly record struct DiagnosticSessionPreviewVisualCadenceResultProjection(");
        AssertContains(previewVisualCadenceResultText, "private static DiagnosticSessionPreviewVisualCadenceResultProjection BuildPreviewVisualCadenceResultProjection(");
        AssertContains(previewVisualCadenceResultText, "var visualCadenceMetrics = analysis.VisualCadenceMetrics;");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DInputUploadCpuP99MsAtEnd: previewD3DMetrics.InputUploadCpuP99MsAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved");
        AssertContains(previewVisualCadenceResultText, "VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd");
        AssertContains(previewVisualCadenceResultText, "VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd");
        AssertContains(resultText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DResult.PreviewD3DInputUploadCpuP99MsAtEnd,");
        AssertContains(resultText, "VisualCadenceOutputFpsAtEnd = previewVisualCadenceResult.VisualCadenceOutputFpsAtEnd,");
        AssertDoesNotContain(resultText, "GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\")");
        AssertDoesNotContain(previewResultText, "previewD3DMetrics");
        AssertDoesNotContain(previewResultText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertDoesNotContain(previewResultText, "analysis.VisualCadenceMetrics");
        AssertDoesNotContain(previewResultText, "VisualCadenceOutputFpsAtEnd");
        AssertDoesNotContain(resultText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewResult");
        AssertDoesNotContain(resultText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DMetrics");
        AssertDoesNotContain(resultText, "VisualCadenceOutputFpsAtEnd = previewResult");
        AssertDoesNotContain(resultText, "VisualCadenceOutputFpsAtEnd = visualCadenceMetrics");
    }
}
