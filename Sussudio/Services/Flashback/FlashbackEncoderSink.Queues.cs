using System;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void CompleteWriter<TPacket>(Channel<TPacket>? channel)
    {
        channel?.Writer.TryComplete();
        SignalWork("complete_writer");
    }

    private void SignalWork(string operation)
    {
        try
        {
            _workAvailable.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"FLASHBACK_SINK_WORK_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private static void DecrementQueueDepth(ref int target, string queueName)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (current <= 0)
            {
                Logger.Log($"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW queue={queueName} depth={current - 1}");
                return;
            }

            if (Interlocked.CompareExchange(ref target, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private bool WaitForCancellation(TimeSpan timeout)
    {
        var cts = _cts;
        if (cts == null)
        {
            Thread.Sleep(timeout);
            return false;
        }

        try
        {
            return cts.Token.WaitHandle.WaitOne(timeout);
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
    }

    private void FailEncoding(Exception ex)
    {
        var shouldNotify = false;
        lock (_sync)
        {
            if (_encodingFailure == null)
            {
                _encodingFailure = ex;
                _started = false;
                shouldNotify = true;
            }
        }

        if (!shouldNotify)
        {
            return;
        }

        Logger.Log($"FLASHBACK_SINK_FATAL type={ex.GetType().Name} msg={ex.Message}");
        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);

        try
        {
            _onFatalError?.Invoke(ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }
}
