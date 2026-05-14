using System;
using System.Threading.Tasks;
using Sussudio.Services.Audio;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task DisposeTransientRecordingBackendAsync(
        IRecordingSink? sink,
        WasapiAudioCapture? wasapiCapture,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video recording stop failed during rollback: {ex.Message}");
            }
        }

        if (sink != null)
        {
            try
            {
                await sink.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink stop failed during rollback: {ex.Message}");
            }

            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink dispose failed during rollback: {ex.Message}");
            }
        }

        if (unifiedVideoCapture != null)
        {
            if (sink is LibAvRecordingSink libAvSink)
            {
                var libAvDrainTask = libAvSink.EncodingCompletionTask;
                if (!libAvDrainTask.IsCompleted)
                {
                    _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                        libAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_start_rollback");
                    unifiedVideoCapture = null;
                }
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video stop failed during rollback: {ex.Message}");
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video dispose failed during rollback: {ex.Message}");
            }
        }

        if (wasapiCapture != null)
        {
            try
            {
                await wasapiCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient WASAPI capture dispose failed during rollback: {ex.Message}");
            }
        }

    }
}
