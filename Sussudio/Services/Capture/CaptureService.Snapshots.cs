using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Snapshot projection for diagnostics, automation, and verification. These
// methods translate live service objects into immutable DTOs without mutating
// capture state, so they can be polled frequently by ssctl/MCP.
public partial class CaptureService
{
    private string ResolveRecordingBackendName()
    {
        if (IsFlashbackRecordingBackendOwnedByRecording())
            return "Flashback";
        return _isRecording && _libavSink != null ? "LibAv" : "None";
    }

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        // CaptureHealthSnapshot inherits from CaptureDiagnosticsSnapshot,
        // so the full snapshot satisfies the diagnostics contract directly.
        return GetHealthSnapshot();
    }

    private static long ComputeTickAge(long tick)
    {
        if (tick == 0) return -1;
        return Math.Max(0, Environment.TickCount64 - tick);
    }
}
