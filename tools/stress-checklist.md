# Capture Reliability Stress Checklist

Run `powershell -ExecutionPolicy Bypass -File tools/reliability-gates.ps1` first.

## Lifecycle loops
1. Run 200 `Start Preview` / `Stop Preview` cycles.
2. Run 200 `Start Recording` / `Stop Recording` cycles.
3. Confirm no hangs, deadlocks, or app crashes.

## Shutdown behavior
1. Start recording.
2. Close the app while recording is active.
3. Confirm no orphan `ffmpeg.exe` process remains.

## Sustained pressure
1. Record for at least 20 minutes.
2. Add storage pressure (large background copy to same drive).
3. Confirm recording continues and diagnostics show bounded queue depths.

## Compatibility checks
1. Validate startup with and without ffmpeg on PATH.
2. Validate two app instances do not collide on named pipes.
3. Confirm fallback status/logging is clear when no compatible capture device is found.

## Diagnostics
1. Open `%USERPROFILE%\\Documents\\ElgatoCapture_Debug.log`.
2. Verify `CaptureDiagnostics:` entries include queue depth, drops, latency, and mux result.
