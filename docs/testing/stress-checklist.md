# Capture Reliability Checklist (Reset Baseline)

The previous automation harness was intentionally removed to reduce maintenance debt.
Use this checklist while a new automation stack is built in small, verifiable steps.

## Baseline gate

Run the current gate script (build only, timeout-bounded):

`powershell -NoProfile -ExecutionPolicy Bypass -File tools/reliability-gates.ps1 -Configuration Debug -Platform x64`

## Manual smoke pass

1. Launch the app and verify the main window opens in under 10 seconds.
2. Refresh devices and confirm the status text updates without freezing.
3. Start preview, wait 10 seconds, and stop preview.
4. Start a short recording, stop it, and verify a playable output file exists.
5. Close the app during idle and confirm the process exits cleanly.

## Manual stress pass

1. Run 50 preview start/stop cycles.
2. Run 50 recording start/stop cycles.
3. Keep a 20-minute recording under moderate disk pressure.
4. Confirm no orphan `ffmpeg.exe` process remains after each stop.
5. Review `%USERPROFILE%\\Documents\\ElgatoCapture_Debug.log` for explicit errors.
