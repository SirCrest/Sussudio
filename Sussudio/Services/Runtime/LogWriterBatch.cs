using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Runtime;

/// <summary>
/// One pass of the logger's writer task (<c>Logger.RunLogWriterAsync</c>): drains a bounded number of queued lines into
/// the text for the log file and hands each line it writes, including the dropped-lines warning, to the log tap.
/// </summary>
internal static class LogWriterBatch
{
    /// <summary>
    /// Drains up to <paramref name="maxEntries"/> queued lines and returns the text to append to the log file, empty
    /// when nothing was queued or dropped. Lines the logger dropped since the last pass (<paramref name="unreportedDropped"/>,
    /// taken and reset here) add a warning after the batch with that count and <paramref name="droppedTotal"/>.
    /// </summary>
    public static string Drain(
        ChannelReader<string> queue,
        int maxEntries,
        ref long unreportedDropped,
        ref long droppedTotal,
        Action<string> tap)
    {
        var batch = new StringBuilder();
        var entries = 0;
        while (entries < maxEntries && queue.TryRead(out var entry))
        {
            batch.Append(entry);
            tap(entry);
            entries++;
        }

        // The tap gets the warning too: log tail has no other sign that lines were lost.
        var droppedInBatch = Interlocked.Exchange(ref unreportedDropped, 0);
        if (droppedInBatch > 0)
        {
            var warning = $"[{DateTime.Now.ToString("HH:mm:ss.fff")}] [Logger] Warning: log channel saturated, dropped_in_batch={droppedInBatch} dropped_total={Interlocked.Read(ref droppedTotal)}\n";
            batch.Append(warning);
            tap(warning);
        }

        return batch.ToString();
    }
}
