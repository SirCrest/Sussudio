using System;
using System.Threading;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private void EncodingLoop(CancellationToken cancellationToken)
    {
        try
        {
            var videoQueue = _videoQueue ?? throw new InvalidOperationException("Video queue is not initialized.");
            var audioQueue = _audioQueue ?? throw new InvalidOperationException("Audio queue is not initialized.");
            var microphoneQueue = _microphoneQueue;
            var gpuQueue = _gpuQueue;
            var cudaQueue = _cudaQueue;

            while (true)
            {
                var madeProgress = false;

                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                if (cudaQueue != null)
                {
                    madeProgress = DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit) || madeProgress;
                }
                if (gpuQueue != null)
                {
                    madeProgress = DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit) || madeProgress;
                }

                madeProgress = DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit) || madeProgress;

                // Audio again catches samples that arrived while video encoding
                // was consuming its bounded batch.
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                if (videoQueue.Reader.Completion.IsCompleted &&
                    audioQueue.Reader.Completion.IsCompleted &&
                    (microphoneQueue == null || microphoneQueue.Reader.Completion.IsCompleted) &&
                    (gpuQueue == null || gpuQueue.Reader.Completion.IsCompleted) &&
                    (cudaQueue == null || cudaQueue.Reader.Completion.IsCompleted) &&
                    Volatile.Read(ref _videoQueueDepth) == 0 &&
                    Volatile.Read(ref _audioQueueDepth) == 0 &&
                    Volatile.Read(ref _microphoneQueueDepth) == 0 &&
                    Volatile.Read(ref _gpuQueueDepth) == 0 &&
                    Volatile.Read(ref _cudaQueueDepth) == 0)
                {
                    break;
                }

                if (madeProgress)
                {
                    continue;
                }

                _workAvailable.Wait(cancellationToken);
            }

            _encoder.FlushAndClose();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ReturnRemainingVideoBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);
            ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);
        }
        catch (Exception ex)
        {
            _encodingFailure = ex;
            lock (_sync) { _started = false; }
            Logger.Log($"LIBAV_SINK_ENCODING_LOOP_FAIL type={ex.GetType().Name} msg={ex.Message}");
            ReturnRemainingVideoBuffers(_videoQueue);
            ReturnRemainingBuffers(_audioQueue, ref _audioQueueDepth);
            ReturnRemainingBuffers(_microphoneQueue, ref _microphoneQueueDepth);
            ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);
            ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);
            try
            {
                _encoder.Dispose();
            }
            catch
            {
                // Preserve the original failure.
            }

            try
            {
                OnEncodingFailed?.Invoke(ex);
            }
            catch
            {
                // Best effort: callback must not mask the original failure.
            }
        }
    }

}
