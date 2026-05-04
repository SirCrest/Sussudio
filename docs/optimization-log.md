# Optimization Log

This log tracks performance and reliability discoveries for Sussudio/Flashback work. Keep entries evidence-oriented: what changed, why, how it was verified, and what remains uncertain.

## 2026-05-04

### Flashback Export Force-Rotate Cancellation

- Discovery: More buffering was not the promising path for 4K120 1% lows; the useful work has been tightening lifecycle, playback, and export frametime stalls.
- Discovery: Force-rotate export requests could time out or be canceled while the encoder thread kept draining deep queues, leaving `_forceRotateDraining` true and delaying recovery.
- Change: Force-rotate now drains audio, microphone, GPU, and video queues in bounded batches, checking for request completion between batches.
- Discovery: A plain `Task.IsCompleted` check before `RotateSegment` still left a tiny race where timeout/cancel could complete the request immediately after the check.
- Change: Force-rotate requests now use an atomic `ForceRotateRequest` state machine. Cancellation can win only while the request is pending; rotation can begin only after `TryBeginCommit` claims the request.
- Discovery: Once commit wins, waiting forever for the committed result would weaken timeout/cancel behavior.
- Change: The timeout-after-commit path now waits only `ForceRotateCommittedGraceMs` before returning empty and logging `FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED_PENDING`; the cancellation-after-commit path logs and rethrows without blocking.
- Verification so far: `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` passed after the bounded drain and request-state changes.
- Verification so far: `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false` passed after the request-state changes.
- Commit: `bc50f1b Harden flashback force-rotate cancellation`.
- Verification: `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` passed after the bounded committed-wait change.
- Verification: `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false` passed after the bounded committed-wait change.
- Review notes: One subagent passed the request-state ownership model. Another correctly flagged the unbounded committed wait; a final targeted subagent review passed after the bounded grace-window change.

### Live Performance Notes

- Snapshot before the latest diagnostic attempt showed Flashback active, zero Flashback video/audio queue depth, no Flashback drops, source cadence near 120fps, and high-motion visual cadence near 120fps.
- The long `flashback-rotated-export` diagnostic was intentionally stopped when the unbounded committed-wait issue was found, so that sample is not counted as final evidence.
- Diagnostic runner note: launching `dotnet run ... diagnostic-session` through `Start-Process` can leave the actual helper running under a different `dotnet` PID than the initial process. Track future long samples by command line/output directory and verify no other `diagnostic-session` helpers are active before starting a new one.
- Diagnostic UX note: background `dotnet` helpers must be launched with `-WindowStyle Hidden` when `Start-Process` is used; otherwise visible terminal windows can appear. Direct Codex-shell runs do not need admin and do not open extra terminal windows.
- Partial live evidence: repeated `flashback-rotated-export` attempts produced multi-GB MP4 exports and logged `FLASHBACK_EXPORT_SEGMENTS_OK` plus `VerifyFile`/ffprobe activity. These confirm force-rotate/export can complete on the rebuilt app, but they are not a clean final 180-second diagnostic verdict because the runner emitted no summary and later closed the app during cleanup.
- Discovery: `flashback-rotated-export` can successfully export and verify, then lose the final diagnostic verdict if a later automation pipe timeout/no-response escapes from cleanup or the extra `--verify` pass.
- Change: The diagnostic runner now converts non-connect automation pipe exceptions into structured failed command responses, preserving warnings, artifacts, and the final PASS/FAIL summary instead of exiting with a terse process-level failure.
