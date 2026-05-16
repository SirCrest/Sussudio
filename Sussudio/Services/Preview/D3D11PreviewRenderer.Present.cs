using System;
using System.Diagnostics;
using System.Threading;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private void PresentAndTrackFrame(
        PendingFrame frame,
        string rendererMode,
        string firstFrameMessage,
        long totalStart,
        long inputUploadTicks,
        long renderTicks,
        ref long presentTicks)
    {
        var swapChain = _swapChain ?? throw new InvalidOperationException("Swap chain is not initialized.");

        TryCaptureFrameBeforePresent(rendererMode);
        var presentStart = Stopwatch.GetTimestamp();
        var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);
        var presentEnd = Stopwatch.GetTimestamp();
        presentTicks += presentEnd - presentStart;
        if (presentResult.Failure)
        {
            throw new InvalidOperationException($"SwapChain.Present failed: 0x{presentResult.Code:X8}.");
        }

        if (Interlocked.Exchange(ref _firstFrameRaised, 1) == 0)
        {
            Logger.Log(firstFrameMessage);
            if (!_dispatcherQueue.TryEnqueue(() => FirstFrameRendered?.Invoke()))
            {
                Logger.Log("D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
            }
        }

        Interlocked.Increment(ref _framesRendered);
        var presentIntervalMs = TrackPresentCadence(frame.CountForPresentCadence);
        TrackDxgiFrameStatistics();
        var estimatedVisibleTick = EstimateVisibleTick(presentEnd);
        TrackFramePresented(frame, presentEnd, estimatedVisibleTick);
        TrackPipelineLatency(frame.ArrivalTick, estimatedVisibleTick);
        var totalTicks = Stopwatch.GetTimestamp() - totalStart;
        TrackRenderCpuTiming(inputUploadTicks, renderTicks, presentTicks, totalTicks);
        RecordSlowFrameDiagnostic(frame, presentIntervalMs, inputUploadTicks, renderTicks, presentTicks, totalTicks, presentEnd, estimatedVisibleTick);
    }
}
