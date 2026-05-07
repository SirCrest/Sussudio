namespace Sussudio.Models;

// Coarse capture lifecycle state surfaced to UI and automation snapshots.
public enum CaptureSessionState
{
    Uninitialized,
    Initializing,
    Ready,
    Previewing,
    Recording,
    CleaningUp,
    Faulted,
    Disposed
}
