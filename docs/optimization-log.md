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
- Verification: A hidden long-run `flashback-rotated-export` diagnostic completed with `PASS` in `temp\diag-force-rotate-20260504-0241-hidden`. It sampled 151 snapshots over a 180-second session, exported a 4.19 GB MP4 from 2 Flashback segments, passed strict verification, kept Flashback playback/export command failures at zero, and left the Sussudio app running.
- Performance evidence from that run: present 1% low was 116.16fps, but visual cadence held 120fps with 26,129 visual samples, 0.1% repeats, longest repeat run 1, high-motion confidence, zero preview scheduler drops/deadline drops/underflows, and zero D3D frame-stats failures.
- Discovery: A 180-second `flashback-playback` diagnostic in `temp\diag-flashback-playback-20260504-0245-hidden` disproved the suspected 120fps-as-60fps playback path: playback ended at 119.99fps with 119.97fps 1% low, zero dropped frames, zero submit failures, and visual cadence at 120fps.
- Remaining playback target: that same run failed because of a transient two-sample dip around offset 81.245s where playback 1% low hit 80.37fps. The max phase was FFmpeg video `send_packet` (~12.39ms), not preview submit, with no segment switch and no drops. The playback thread now registers with MMCSS `Playback` priority 1 to reduce rare decoder feed scheduling spikes.
- Follow-up evidence: priority 1 improved the playback min 1% low to 95.07fps in `temp\diag-flashback-playback-20260504-0253-mmcss-hidden`, but still missed the 96fps diagnostic floor. Raising the default to priority 2 regressed the min to 81.82fps in `temp\diag-flashback-playback-20260504-0259-mmcss2-hidden`, so priority 1 remains the better default and the remaining work is decoder-feed spike reduction, not more thread-priority pressure.
- Rejected experiment: increasing Flashback D3D11VA `extra_hw_frames` from 4 to 16 was tested in `temp\diag-flashback-playback-20260504-0307-hwframes-hidden`. The 180-second run kept visual cadence near 120fps, but playback min 1% low regressed to 83.11fps and the max decode phase remained FFmpeg `send_packet` with no segment switch, drops, or submit failures. The code was reverted; this is not the best option.
