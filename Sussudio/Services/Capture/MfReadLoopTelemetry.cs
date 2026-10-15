using System.Diagnostics;
using Sussudio.Services.Performance;

namespace Sussudio.Services.Capture;

/// <summary>
/// Performance telemetry for one run of the Media Foundation read loop (docs/architecture/performance-telemetry.md
/// sections 2.2 and 2.6). One instance per read loop, used only by the read-loop thread, so there are no locks and
/// no interlocked state of its own; a restarted capture starts with a fresh instance and a fresh ingress
/// classification. Observation only: nothing here changes what the loop reads, drops or delivers.
/// Records <c>capture.readsample_wait</c>, <c>capture.arrival_interval</c>, <c>capture.ingress_lag</c>,
/// <c>capture.deliver</c> and the <c>CaptureRead</c>, <c>CaptureSampleTime</c> and <c>CaptureDeliver</c> trace stages.
/// </summary>
internal sealed class MfReadLoopTelemetry
{
    // Gaps longer than this are a pause or a restart, not a cadence sample.
    private static readonly long MaxArrivalIntervalTicks = 5 * Stopwatch.Frequency;

    private readonly IngressClockClassifier _classifier = new();
    private readonly long _frequency = Stopwatch.Frequency;
    private long _readStartTick;
    private long _lastArrivalTick;

    public MfReadLoopTelemetry()
    {
        PerfIngressClock.Publish(PerfIngressClockKind.Unknown, long.MinValue);
    }

    /// <summary>Stamps the start of a <c>ReadSample</c> call (QPC), beside the loop's own TickCount64 stamp.</summary>
    public void BeginReadSample() => _readStartTick = Stopwatch.GetTimestamp();

    /// <summary>
    /// A sample arrived: <paramref name="arrivalTick"/> is the QPC timestamp taken right after <c>ReadSample</c>
    /// returned, <paramref name="mfTimestamp100ns"/> the sample time Media Foundation stamped on it (zero or
    /// negative when it carried none). Call only for a successful read that produced a sample.
    /// </summary>
    public void OnSampleArrived(long arrivalTick, long mfTimestamp100ns)
    {
        var waitTicks = arrivalTick - _readStartTick;
        PerfMetrics.CaptureReadSampleWait.RecordTicks(waitTicks);
        FrameTrace.Record(FrameTraceEventKind.CaptureRead, _readStartTick, waitTicks, arrivalTick);

        var lastArrival = _lastArrivalTick;
        _lastArrivalTick = arrivalTick;
        if (lastArrival > 0)
        {
            var intervalTicks = arrivalTick - lastArrival;
            if (intervalTicks <= MaxArrivalIntervalTicks)
            {
                PerfMetrics.CaptureArrivalInterval.RecordTicks(intervalTicks);
            }
        }

        if (mfTimestamp100ns > 0)
        {
            RecordIngress(arrivalTick, mfTimestamp100ns);
        }
    }

    /// <summary>Records the <c>DeliverFrame</c> duration; call from a finally block so a failed delivery still counts.</summary>
    public void OnDelivered(long deliverStartTick, long arrivalTick)
    {
        var deliverTicks = Stopwatch.GetTimestamp() - deliverStartTick;
        PerfMetrics.CaptureDeliver.RecordTicks(deliverTicks);
        FrameTrace.Record(FrameTraceEventKind.CaptureDeliver, deliverStartTick, deliverTicks, arrivalTick);
    }

    private void RecordIngress(long arrivalTick, long mfTimestamp100ns)
    {
        var arrival100Ns = QpcMath.TicksToHundredNs(arrivalTick, _frequency);
        var offset100Ns = arrival100Ns - mfTimestamp100ns;
        if (_classifier.Observe(offset100Ns))
        {
            PerfIngressClock.Publish(_classifier.Kind, _classifier.MinOffset100Ns);
        }

        if (!_classifier.IsQpcDomain)
        {
            return;
        }

        // 100 ns to microseconds; a sample stamped slightly in the future records as zero.
        PerfMetrics.CaptureIngressLag.Record(offset100Ns / 10);

        var sampleQpcTick = QpcMath.HundredNsToTicks(mfTimestamp100ns, _frequency);
        PerfIngressSamples.Record(arrivalTick, sampleQpcTick);
        FrameTrace.RecordInstant(FrameTraceEventKind.CaptureSampleTime, sampleQpcTick, arrivalTick);
    }
}
