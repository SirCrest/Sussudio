static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership()
    {
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Result.cs")
            .Replace("\r\n", "\n");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewD3DResult.cs")
            .Replace("\r\n", "\n");
        var previewVisualCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewResult.cs")
            .Replace("\r\n", "\n");

        AssertContains(resultText, "var previewResult = BuildPreviewResultProjection(analysis);");
        AssertContains(resultText, "var previewD3DResult = BuildPreviewD3DResultProjection(analysis);");
        AssertContains(resultText, "var previewVisualCadenceResult = BuildPreviewVisualCadenceResultProjection(analysis);");
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
