namespace ElgatoCapture.Models;

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
