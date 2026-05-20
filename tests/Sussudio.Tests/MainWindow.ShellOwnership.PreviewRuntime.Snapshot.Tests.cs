using System.Threading.Tasks;

static partial class Program
{
    private static Task PreviewRuntimeSnapshotController_OwnsSnapshotMapping()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotMapperText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotMapper.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotSurfaceProjectionPolicyText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotSurfaceProjectionPolicy.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotStartupProjectionPolicyText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotStartupProjectionPolicy.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotGpuPlaybackProjectionPolicyText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotHealthInputFactoryText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotHealthInputFactory.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotHealthPolicyText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotHealthPolicy.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotInputText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotInput.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "=> await WindowUiDispatchController.InvokeWithRetryAsync(");
        AssertContains(previewRuntimeSnapshotText, "GetPreviewRuntimeSnapshot,");
        AssertContains(previewRuntimeSnapshotText, "\"Failed to enqueue preview snapshot operation.\",");
        AssertContains(previewRuntimeSnapshotText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertContains(previewRuntimeSnapshotText, "return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotText, "D3DRenderer = _previewRendererHostController.Renderer,");
        AssertContains(previewRuntimeSnapshotText, "PreviewSourceAttached = _previewRendererHostController.IsCpuPreviewSourceAttached,");
        AssertContains(previewRuntimeSnapshotText, "GpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible,");
        AssertContains(previewRuntimeSnapshotText, "FramesArrived = _previewRendererHostController.FramesArrived,");
        AssertContains(previewRuntimeSnapshotText, "PreviewMinPresentationIntervalMs = _previewRendererHostController.PreviewMinPresentationIntervalMs,");
        AssertContains(previewRuntimeSnapshotText, "StartupState = CurrentPreviewStartupState.ToString(),");
        AssertContains(previewRuntimeSnapshotText, "GpuPositionEventCount = PreviewStartupGpuPositionEventCount");
        AssertContains(previewRuntimeSnapshotInputText, "internal sealed class PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotInputText, "public D3D11PreviewRenderer? D3DRenderer { get; init; }");
        AssertContains(previewRuntimeSnapshotInputText, "public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }");
        AssertContains(previewRuntimeSnapshotInputText, "public long GpuPositionEventCount { get; init; }");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "internal sealed class PreviewRuntimeSnapshotInput");

        AssertContains(previewRuntimeSnapshotControllerText, "internal static class PreviewRuntimeSnapshotController");
        AssertContains(previewRuntimeSnapshotControllerText, "public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeSnapshotControllerText, "var d3dProjection = PreviewRuntimeD3DProjection.Build(input);");
        AssertContains(previewRuntimeSnapshotControllerText, "var healthInput = PreviewRuntimeSnapshotHealthInputFactory.Build(");
        AssertContains(previewRuntimeSnapshotControllerText, "Environment.TickCount64,");
        AssertContains(previewRuntimeSnapshotControllerText, "var health = PreviewRuntimeSnapshotHealthPolicy.Evaluate(healthInput);");
        AssertContains(previewRuntimeSnapshotControllerText, "return PreviewRuntimeSnapshotMapper.Build(input, d3dProjection, health, DateTimeOffset.UtcNow);");
        AssertContains(previewRuntimeSnapshotMapperText, "internal static class PreviewRuntimeSnapshotMapper");
        AssertContains(previewRuntimeSnapshotMapperText, "public static PreviewRuntimeSnapshot Build(");
        AssertContains(previewRuntimeSnapshotMapperText, "var surface = PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate(input, d3dProjection, health);");
        AssertContains(previewRuntimeSnapshotMapperText, "var startup = PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate(input, health);");
        AssertContains(previewRuntimeSnapshotMapperText, "var gpuPlayback = PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate(input, d3dProjection);");
        AssertContains(previewRuntimeSnapshotMapperText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotMapperText, "TimestampUtc = timestampUtc,");
        AssertContains(previewRuntimeSnapshotMapperText, "IsPreviewing = surface.IsPreviewing,");
        AssertContains(previewRuntimeSnapshotMapperText, "FramesArrived = surface.FramesArrived,");
        AssertContains(previewRuntimeSnapshotMapperText, "StartupState = startup.State,");
        AssertContains(previewRuntimeSnapshotMapperText, "StartupElapsedMs = startup.ElapsedMs,");
        AssertContains(previewRuntimeSnapshotMapperText, "BlankSuspected = surface.BlankSuspected,");
        AssertContains(previewRuntimeSnapshotMapperText, "StallSuspected = surface.StallSuspected,");
        AssertContains(previewRuntimeSnapshotMapperText, "GpuPlaybackState = gpuPlayback.PlaybackState,");
        AssertContains(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = gpuPlayback.PositionEventCount");

        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "internal static class PreviewRuntimeSnapshotSurfaceProjectionPolicy");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "public static PreviewRuntimeSnapshotSurfaceProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "GpuActive: d3dProjection.GpuActive,");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "BlankSuspected: health.BlankSuspected,");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "internal static class PreviewRuntimeSnapshotStartupProjectionPolicy");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "public static PreviewRuntimeSnapshotStartupProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "ElapsedMs: health.StartupElapsedMs,");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "RecoveryAttemptCount: input.StartupRecoveryAttemptCount,");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "internal static class PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "public static PreviewRuntimeSnapshotGpuPlaybackProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "PlaybackState: d3dProjection.GpuPlaybackState,");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "PositionEventCount: input.GpuPositionEventCount);");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "internal static class PreviewRuntimeSnapshotHealthInputFactory");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "public static PreviewRuntimeSnapshotHealthInput Build(");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "RendererAttached = d3dProjection.RendererAttached,");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "CurrentTick = currentTick,");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "UtcNow = utcNow");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "internal static class PreviewRuntimeSnapshotHealthPolicy");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "public static PreviewRuntimeSnapshotHealth Evaluate(PreviewRuntimeSnapshotHealthInput input)");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "var startupTimedOut = input.IsPreviewing");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "input.FramesArrived > 30");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "input.CurrentTick - input.LastPresentedTick > 3000");

        AssertContains(agentMapText, "PreviewRuntimeSnapshotInput.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotSurfaceProjectionPolicy.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotStartupProjectionPolicy.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotHealthInputFactory.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotHealthPolicy.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotInput.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotSurfaceProjectionPolicy.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotStartupProjectionPolicy.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotHealthInputFactory.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotHealthPolicy.cs");

        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuActive = d3dProjection.GpuActive,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "FramesArrived = d3dProjection.FramesArrived,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupElapsedMs = health.StartupElapsedMs,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupRecoveryAttemptCount = input.StartupRecoveryAttemptCount,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPlaybackState = d3dProjection.GpuPlaybackState,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = input.GpuPositionEventCount");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "return new PreviewRuntimeSnapshot");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "new PreviewRuntimeSnapshotHealthInput");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "RendererAttached = d3dProjection.RendererAttached,");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "var startupTimedOut = input.IsPreviewing");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "input.LastPresentedTick > 0");
        AssertDoesNotContain(previewRuntimeSnapshotText, "TaskCompletionSource<PreviewRuntimeSnapshot>");
        AssertDoesNotContain(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetRenderCpuTimingMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameOwnershipMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameLatencyWaitMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetPipelineLatencyMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "_dispatcherQueue.TryEnqueue");
        AssertDoesNotContain(previewRuntimeSnapshotText, "const int maxAttempts = 3;");
        AssertDoesNotContain(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertDoesNotContain(previewRuntimeSnapshotText, "await Task.Delay(50, cancellationToken).ConfigureAwait(false);");
        AssertDoesNotContain(mainWindowText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");

        return Task.CompletedTask;
    }
}
