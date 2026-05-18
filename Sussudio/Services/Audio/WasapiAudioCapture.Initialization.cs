using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioCapture
{
    public Task InitializeAsync(string audioDeviceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) != 0)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(audioDeviceId))
        {
            throw new ArgumentException("Audio device id is required.", nameof(audioDeviceId));
        }

        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IAudioClient? audioClient = null;
        IAudioClient3? audioClient3 = null;
        IAudioCaptureClient? audioCaptureClient = null;
        AutoResetEvent? captureEvent = null;
        IntPtr mixFormat = IntPtr.Zero;
        IntPtr desiredFormat = IntPtr.Zero;
        IntPtr selectedFormat = IntPtr.Zero;
        var useDesiredFormat = false;

        try
        {
            enumerator = WasapiComInterop.CreateDeviceEnumerator();
            var hrGetDevice = enumerator.GetDevice(audioDeviceId, out device);
            if (hrGetDevice < 0)
            {
                throw new InvalidOperationException(
                    $"WASAPI audio capture device '{audioDeviceId}' was not found (hr=0x{hrGetDevice:X8}).");
            }

            audioClient = WasapiComInterop.ActivateAudioClient(device, out audioClient3);
            WasapiComInterop.ThrowIfFailed(
                audioClient.GetMixFormat(out mixFormat),
                "IAudioClient.GetMixFormat(capture)");

            desiredFormat = WasapiComInterop.AllocFloatStereo48kFormat();
            var hrFormat = audioClient.IsFormatSupported(
                WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                desiredFormat,
                out var closestMatch);
            if (closestMatch != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(closestMatch);
            }

            useDesiredFormat = hrFormat == WasapiComInterop.S_OK;
            selectedFormat = useDesiredFormat ? desiredFormat : mixFormat;
            _captureFormat = WasapiComInterop.ReadAudioFormat(selectedFormat);

            if (!WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, selectedFormat))
            {
                WasapiComInterop.ThrowIfFailed(
                    audioClient.Initialize(
                        WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                        WasapiComInterop.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                        0,
                        0,
                        selectedFormat,
                        IntPtr.Zero),
                    "IAudioClient.Initialize(capture)");
            }

            captureEvent = new AutoResetEvent(false);
            WasapiComInterop.ThrowIfFailed(
                audioClient.SetEventHandle(captureEvent.SafeWaitHandle.DangerousGetHandle()),
                "IAudioClient.SetEventHandle(capture)");

            var iidCaptureClient = WasapiComInterop.IID_IAudioCaptureClient;
            WasapiComInterop.ThrowIfFailed(
                audioClient.GetService(ref iidCaptureClient, out var captureClientObject),
                "IAudioClient.GetService(IAudioCaptureClient)");
            audioCaptureClient = (IAudioCaptureClient)captureClientObject;

            _fastPathCopy = _captureFormat.SampleRate == OutputSampleRate &&
                            _captureFormat.Channels == OutputChannels &&
                            _captureFormat.SampleType == WasapiSampleType.Float32;
            _resampleRemainderNumerator = 0;
            _deviceEnumerator = enumerator;
            _device = device;
            _audioClient = audioClient;
            _audioClient3 = audioClient3;
            _audioCaptureClient = audioCaptureClient;
            _captureEvent = captureEvent;
            Interlocked.Exchange(ref _audioFramesArrived, 0);
            Interlocked.Exchange(ref _audioFramesWrittenToSink, 0);
            Interlocked.Exchange(ref _audioLevelLastFireTick, 0);
            Interlocked.Exchange(ref _audioLevelEventsFired, 0);
            Interlocked.Exchange(ref _audioLevelEventsLastFireTickMs, 0);
            Interlocked.Exchange(ref _captureCallbackCount, 0);
            Interlocked.Exchange(ref _lastCaptureCallbackTickMs, 0);
            Interlocked.Exchange(ref _captureCallbackSevereGapCount, 0);
            Interlocked.Exchange(ref _audioDataDiscontinuityCount, 0);
            Interlocked.Exchange(ref _audioTimestampErrorCount, 0);
            Volatile.Write(ref _captureCallbackSilenceCount, 0);
            lock (_captureCallbackIntervalLock)
            {
                Array.Clear(_captureCallbackIntervalWindowMs, 0, _captureCallbackIntervalWindowMs.Length);
                _captureCallbackIntervalCount = 0;
                _captureCallbackIntervalIndex = 0;
            }
            Interlocked.Exchange(ref _initialized, 1);

            Logger.Log(
                "WASAPI capture initialized: " +
                $"device={audioDeviceId} " +
                $"selected={(useDesiredFormat ? "f32-48k-stereo" : "mix-format")} " +
                $"sample_rate={_captureFormat.SampleRate} " +
                $"channels={_captureFormat.Channels} " +
                $"bits={_captureFormat.BitsPerSample} " +
                $"type={_captureFormat.SampleType}");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiAudioCapture.InitializeAsync: {ex.Message}");
            captureEvent?.Dispose();
            WasapiComInterop.ReleaseComObject(ref audioCaptureClient);
            WasapiComInterop.ReleaseComObject(ref audioClient3);
            WasapiComInterop.ReleaseComObject(ref audioClient);
            WasapiComInterop.ReleaseComObject(ref device);
            WasapiComInterop.ReleaseComObject(ref enumerator);
            throw;
        }
        finally
        {
            if (desiredFormat != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(desiredFormat);
            }

            if (mixFormat != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(mixFormat);
            }
        }
    }
}
