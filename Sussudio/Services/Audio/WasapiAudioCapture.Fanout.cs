using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioCapture
{
    public void AttachRecordingSink(IRecordingSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        Volatile.Write(ref _recordingSink, sink);
    }

    public void DetachRecordingSink()
    {
        Volatile.Write(ref _recordingSink, null);
    }

    public void AttachFlashbackSink(IRecordingSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        Volatile.Write(ref _flashbackSink, sink);
    }

    public void DetachFlashbackSink()
    {
        Volatile.Write(ref _flashbackSink, null);
    }

    public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)
    {
        // Runs on the WASAPI capture thread. Writers must copy/enqueue
        // synchronously and return a completed task; incomplete tasks are
        // rejected instead of being waited on in the callback path.
        Volatile.Write(ref _audioWriter, writer);
    }

    internal void SetPlayback(WasapiAudioPlayback? playback)
    {
        Volatile.Write(ref _playback, playback);
    }

    private static void InvokeHotAudioWriter(
        Func<ReadOnlyMemory<byte>, Task> writer,
        ReadOnlyMemory<byte> samples,
        string target)
        => CompleteHotAudioWrite(writer(samples), target);

    private static void WriteAudioToSinkOnCaptureThread(
        IRecordingSink sink,
        ReadOnlyMemory<byte> samples,
        string target)
        => CompleteHotAudioWrite(sink.WriteAudioAsync(samples), target);

    private static void CompleteHotAudioWrite(Task task, string target)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (!task.IsCompleted)
        {
            throw new InvalidOperationException(
                $"{target} audio writer returned an incomplete Task on the WASAPI capture thread. " +
                "Audio writers must copy/enqueue synchronously and return Task.CompletedTask.");
        }

        task.GetAwaiter().GetResult();
    }
}
