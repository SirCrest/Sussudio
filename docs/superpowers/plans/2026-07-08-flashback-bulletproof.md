# Flashback Bulletproofing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate the remaining ways the Flashback system can lose user history, die silently, or stall — found in the 2026-07-08 failure-mode investigation.

**Architecture:** Six independent work packages over the existing subsystem (no redesign): harden the fatal-error path in `CaptureService`, add a real disk-space policy to `FlashbackBufferManager`, close capture-side gaps in `FlashbackEncoderSink`, remove the redundant-decode resume path in `FlashbackPlaybackController`, and surface health to the UI. Packages are file-disjoint so Wave 1 runs as parallel Sonnet subagents.

**Tech Stack:** WinUI 3 / .NET 8, FFmpeg.AutoGen (libav), NVENC, xUnit (reflection + source-text contract tests).

---

## Investigation findings (2026-07-08)

Context: the 2026-06-10 investigation (`docs/flashback-hardening-investigation.md`) was largely fixed in commit `76913bf0` (audio-prebuffer gating, off-lock eviction deletes, WASAPI handshake, drift policy, live-edge lead). This investigation covered the domains that doc did not: crash/fatal paths, disk exhaustion, export-time capture behavior, recovery, and what remains from the old doc.

| # | Severity | Finding | Evidence |
|---|----------|---------|----------|
| F1 | **P0** | A fatal encoder error **purges the entire DVR buffer and never restarts flashback**. `BeginFlashbackBackendCleanup` calls `DisposeFlashbackPreviewBackendAsync(purgeSegments: true)`; history survives only if the exception is a GPU TDR (`IsGpuDeviceLost`). No auto-restart exists — the backend returns only via manual toggle/restart/preview-restart. The enabled toggle stays on; the only user signal is a transient status string. | `CaptureService.cs:714-776`, `639-660` |
| F2 | **P0** | **No runtime free-disk-space policy.** Eviction enforces only the configured duration/byte budget (`FlashbackBufferManager.UpdateDiskBytes:323-381`). If the drive fills from outside or the budget overshoots the disk, the encoder hits a native write failure → fatal → (today, via F1) purge + dead. Free space is probed once at startup and merely reported. | `FlashbackBufferManager.cs:139`, `FlashbackStartupCacheCleanup.cs:127-144` |
| F3 | **P0** | **Recovery-preserved sessions leak forever.** `.flashback-recovery-preserve` marker dirs are skipped unconditionally by both startup cleanup passes; nothing ever surfaces, recovers, or expires them. | `FlashbackStartupCacheCleanup.cs:67-71`, `283-287` |
| F4 | **P1** | **Resume still does redundant decode passes** (June-10 P1-4; fix option B was taken, not A). `PrimePlaybackAudioBuffer` decodes ≤96 frames, releases every video frame, rewind-seeks, and playback re-decodes the interval. Software-decode resumes stall for seconds. | `FlashbackPlaybackController.cs:2251-2401` |
| F5 | **P1** | **Every export drops live video frames.** During force-rotate drain, video producers are rejected unconditionally (`force_rotate_draining`), while audio has a 65% queue-guard ratio. Every export punches a video gap into the DVR buffer for the drain duration. | `FlashbackEncoderSink.cs:1437-1463` vs `1355-1366`, `2356-2509` |
| F6 | **P1** | **Persistent rotation failure grows the active segment unbounded.** `RotateSegment` failure advances `_segmentStartPts` and retries next boundary; nothing escalates after repeated failures, the active segment never completes, and eviction cannot reclaim it. | `FlashbackEncoderSink.cs:2144-2216` |
| F7 | **P1** | **Stop-recording can lose tail frames.** `EndRecordingAsync` waits a fixed 100 ms then snapshots `LatestPts` as the recording end; frames still queued (video queue holds up to 180) get PTS past the end point and are excluded from the export. | `FlashbackEncoderSink.cs:685-760` |
| F8 | **P1** | **Involuntary snap-to-live is silent.** `SetState` only logs; decode-error/near-live/software-budget snaps reach the UI only via the 250 ms poll, with no notification. (June-10 P2-4, still open.) | `FlashbackPlaybackController.cs:880-886`, `PlaybackFrames.cs:1090-1160` |
| F9 | **P2** | June-10 P2 seams still open: pause-from-live shows a keyframe up to ~1 s stale (P2-1, `ThreadCommands.cs:555-618`); scrub is keyframe-only with no settle refinement (P2-2); first interaction is cold (P2-3, `ThreadCommands.cs:135`); `GapFromLive` reports 0 before first decoded frame (P2-6). |
| F10 | **verify** | Video PTS is frame-count-derived while audio PTS is sample-count-derived (`FlashbackEncoderSink.ResolveEncoderPts:2124-2142`). Sustained video-frame drops at the sink should produce cumulative A/V drift in the buffer. Needs a runtime probe before any fix — do not implement on theory. |

Not broken (verified during investigation, listed so nobody "fixes" them): eviction deletes are off-lock with retry+parking; export fails loudly (classified reasons) when segments in range are missing/corrupt, with a `force` escape hatch; exporter temp-file leases + orphan cleanup are solid; decoder falls back D3D11VA→software at init and snaps to live on mid-decode errors; HDR/P010 negotiation fails closed per the project rail.

---

## Execution model (Sonnet subagents)

- **Dispatch:** one `code-implementer` subagent per task, `model: "sonnet"`. Wave 1 = Tasks 1–4 in parallel (file-disjoint). Wave 2 = Tasks 5–6 after Wave 1 merges (Task 5 consumes Task 1+4 APIs; Task 6 edits the same files as Task 4).
- **File ownership is exclusive per task.** A subagent must not edit files outside its task's `Files:` list. Each task adds tests in its **own new test file** (never edit `XUnit.FlashbackContractsTests.cs` — it is shared and would collide across parallel agents).
- **Subagents do not build or run the app.** They implement + write tests and report. The orchestrator builds and runs tests once per wave (build requires the app closed — check MCP `get_app_state` first; see CLAUDE.md):

```bash
dotnet build Sussudio/Sussudio.csproj -p:Platform=x64 -p:StageLatestBuild=true
dotnet run --project tests/Sussudio.Tests/ -- "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll"
```

  Known gotcha: after a `StageLatestBuild` build the harness may falsely flag the dll stale (root-dir mtime) — touch the dll or rebuild without staging.
- **Rails for every subagent prompt:** never rename an `AutomationProperties.AutomationId`; never add an HDR→SDR fallback; no `try/catch` around `AccessViolationException`; match surrounding code style; read `temp/logs/Sussudio_Debug.log` tokens named in the task for runtime verification steps run by the orchestrator.
- **Review gate:** orchestrator reads each subagent's diff before the wave build. Anything touching lock ordering (`_indexLock`, `_sync`, `_videoQueueSync`) gets a line-by-line read.

---

### Task 1: Fatal-error path — preserve history, bounded auto-restart (F1)

**Files:**
- Modify: `Sussudio/Services/Capture/CaptureService.cs:714-776` (`BeginFlashbackBackendCleanup`), field block near `_flashbackCleanupInProgress`
- Modify: `Sussudio/Services/Capture/CaptureService.Flashback.cs` (`EnsureFlashbackPreviewBackendAsync`, add restart helper)
- Test: `tests/Sussudio.Tests/XUnit.FlashbackFatalPathContractsTests.cs` (new)

- [ ] **Step 1: Write failing source-text contract tests** (follow the `AssertContains`-on-source pattern used in `XUnit.AutomationContractsTests.cs`):

```csharp
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackFatalPathContractsTests
{
    private static string ReadCaptureServiceSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Capture/CaptureService.cs"));

    [Fact]
    public void FlashbackBackendCleanup_DoesNotPurgeSegments()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = SourceSlice.Method(source, "private void BeginFlashbackBackendCleanup");
        Assert.Contains("purgeSegments: false", cleanup);
        Assert.DoesNotContain("purgeSegments: true", cleanup);
    }

    [Fact]
    public void FlashbackBackendCleanup_PreservesRecoverySegmentsForAllFatalErrors()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = SourceSlice.Method(source, "private void BeginFlashbackBackendCleanup");
        // Preserve must run unconditionally, not only inside the IsGpuDeviceLost branch.
        Assert.Contains("PreserveRecoverySegments(\"backend_fatal\")", cleanup);
    }

    [Fact]
    public void FlashbackBackendCleanup_SchedulesBoundedAutoRestart()
    {
        var source = ReadCaptureServiceSource();
        var cleanup = SourceSlice.Method(source, "private void BeginFlashbackBackendCleanup");
        Assert.Contains("TryScheduleFlashbackAutoRestart", cleanup);

        var flashbackSource = File.ReadAllText(TestPaths.Repo("Sussudio/Services/Capture/CaptureService.Flashback.cs"));
        Assert.Contains("MaxFlashbackAutoRestartAttempts = 2", flashbackSource);
        Assert.Contains("FLASHBACK_AUTO_RESTART", flashbackSource);
    }
}
```

If `TestPaths`/`SourceSlice` helpers do not exist in the test project, copy the repo-root resolution and method-slicing helpers already used by `XUnit.AutomationContractsTests.cs` into this file as private statics — do not modify shared helper files.

- [ ] **Step 2: Run the new tests, verify all three FAIL** (purge is currently `true`, no restart helper exists).

- [ ] **Step 3: Implement the cleanup change** in `BeginFlashbackBackendCleanup`:

```csharp
// Replace the IsGpuDeviceLost-only preserve with an unconditional preserve.
// A fatal error must never destroy the user's DVR history; bounded startup
// cleanup (retire markers + Task 2 aging) reclaims the disk later.
_flashbackBackend.PreserveRecoverySegments("backend_fatal");
if (IsGpuDeviceLost(ex))
{
    Logger.Log($"FLASHBACK_BACKEND_FATAL_DEVICE_LOST type={ex.GetType().Name} preserving_segments=true");
}
```

and inside the `Task.Run` body change the dispose call to `purgeSegments: false`, then after `StatusChanged?.Invoke(...)` add:

```csharp
TryScheduleFlashbackAutoRestart(ex, generationAtFault);
```

- [ ] **Step 4: Implement the restart helper** in `CaptureService.Flashback.cs`:

```csharp
private const int MaxFlashbackAutoRestartAttempts = 2;
private static readonly TimeSpan FlashbackAutoRestartDelay = TimeSpan.FromSeconds(2);
private static readonly TimeSpan FlashbackAutoRestartHealthyWindow = TimeSpan.FromSeconds(60);
private int _flashbackAutoRestartAttempts;
private long _lastFlashbackAutoRestartTick;

private void TryScheduleFlashbackAutoRestart(Exception cause, long generationAtFault)
{
    // Reset the attempt counter when the last restart survived the healthy window.
    var now = Environment.TickCount64;
    var last = Interlocked.Read(ref _lastFlashbackAutoRestartTick);
    if (last > 0 && now - last > (long)FlashbackAutoRestartHealthyWindow.TotalMilliseconds)
    {
        Interlocked.Exchange(ref _flashbackAutoRestartAttempts, 0);
    }

    if (!_flashbackEnabled || _isRecording)
    {
        Logger.Log($"FLASHBACK_AUTO_RESTART_SKIP reason=state enabled={_flashbackEnabled} recording={_isRecording}");
        return;
    }

    var attempt = Interlocked.Increment(ref _flashbackAutoRestartAttempts);
    if (attempt > MaxFlashbackAutoRestartAttempts)
    {
        Logger.Log($"FLASHBACK_AUTO_RESTART_GIVE_UP attempts={attempt - 1} cause={cause.GetType().Name}");
        StatusChanged?.Invoke(this, "Flashback stopped after repeated errors — use Restart Flashback to retry.");
        return;
    }

    Interlocked.Exchange(ref _lastFlashbackAutoRestartTick, now);
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(FlashbackAutoRestartDelay).ConfigureAwait(false);
            await RunTransitionAsync(CurrentSessionState, async transitionToken =>
            {
                if (CurrentSessionGeneration != generationAtFault ||
                    !_flashbackEnabled || _isRecording || _flashbackBackend.Sink != null)
                {
                    Logger.Log("FLASHBACK_AUTO_RESTART_SKIP reason=stale_or_already_running");
                    return;
                }

                var capture = _videoPipeline.Capture;
                var settings = _currentSettings;
                if (capture == null || settings == null)
                {
                    Logger.Log("FLASHBACK_AUTO_RESTART_SKIP reason=no_capture_or_settings");
                    return;
                }

                await EnsureFlashbackPreviewBackendAsync(capture, settings, transitionToken).ConfigureAwait(false);
                Logger.Log($"FLASHBACK_AUTO_RESTART_OK attempt={attempt}");
                StatusChanged?.Invoke(this, "Flashback recovered after an error.");
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception restartEx)
        {
            Logger.Log($"FLASHBACK_AUTO_RESTART_FAIL attempt={attempt} type={restartEx.GetType().Name} msg='{restartEx.Message}'");
            StatusChanged?.Invoke(this, $"Flashback restart failed: {restartEx.Message}");
        }
    });
}
```

Adjust to the actual `RunTransitionAsync` signature in `CaptureService` (it is used throughout `CaptureService.Flashback.cs` — copy an existing call shape). The generation guard must stay.

- [ ] **Step 5: Run the three contract tests, verify PASS.**

- [ ] **Step 6: Commit** — `git commit -m "fix(flashback): preserve history on fatal errors and auto-restart the backend"`

**Behavioral note for the orchestrator's runtime verification:** kill the encoder mid-session (e.g. lock a segment file to force a rotation fatal, or use an injected fault if available) and confirm log order: `FLASHBACK_SINK_FATAL` → `FLASHBACK_RECOVERY_PRESERVE reason=backend_fatal` → `FLASHBACK_AUTO_RESTART_OK` with segments intact on disk.

---

### Task 2: Disk-space guard + preserved-session aging (F2, F3)

**Files:**
- Modify: `Sussudio/Services/Flashback/FlashbackBufferManager.cs` (`UpdateDiskBytes:323-381`, new members)
- Modify: `Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs` (preserve-skip branches at `:67-71` and `:283-287`)
- Modify: `Sussudio/Services/Flashback/FlashbackEncoderSink.cs` — **one line region only**: the low-disk check inside `OnVideoFrameEncoded` (coordinate: Task 3 owns this file; this task defines the API, Task 3's agent applies the call. See "Cross-task seam" below.)
- Test: `tests/Sussudio.Tests/XUnit.FlashbackDiskPolicyTests.cs` (new)

**Cross-task seam:** to keep Wave 1 file-disjoint, Task 2 implements everything in `FlashbackBufferManager` + `FlashbackStartupCacheCleanup` and exposes `bool IsDiskCriticallyLow`. Task 3's agent (owner of `FlashbackEncoderSink.cs`) adds the sink-side check. Both sides are specified in their own tasks.

- [ ] **Step 1: Write failing unit tests** (real behavioral tests — this logic is pure enough):

```csharp
using System;
using System.IO;
using Xunit;
using Sussudio.Services.Flashback;

namespace Sussudio.Tests;

public sealed class FlashbackDiskPolicyTests
{
    [Fact]
    public void LowFreeSpace_ShrinksEffectiveBudget_AndEvicts()
    {
        var options = new FlashbackBufferOptions
        {
            BufferDuration = TimeSpan.FromMinutes(5),
            SegmentDuration = TimeSpan.FromMinutes(1),
            // New test seam: injectable free-space provider.
            FreeDiskBytesProvider = () => 1L * 1024 * 1024 * 1024 // 1 GiB free
        };
        using var manager = new FlashbackBufferManager(options);
        // SoftMinFreeDiskBytes default is 2 GiB => manager must report pressure.
        Assert.True(manager.IsDiskSpaceLow);
        Assert.False(manager.IsDiskCriticallyLow);
    }

    [Fact]
    public void CriticallyLowFreeSpace_SetsCriticalFlag()
    {
        var options = new FlashbackBufferOptions
        {
            FreeDiskBytesProvider = () => 256L * 1024 * 1024 // 256 MiB
        };
        using var manager = new FlashbackBufferManager(options);
        Assert.True(manager.IsDiskCriticallyLow);
    }

    [Fact]
    public void PreservedRecoveryDirectory_OlderThanRetention_IsDeleted()
    {
        var temp = Directory.CreateTempSubdirectory("fbtest_").FullName;
        try
        {
            var session = Path.Combine(temp, new string('a', 32));
            Directory.CreateDirectory(session);
            File.WriteAllText(Path.Combine(session, ".flashback-recovery-preserve"),
                DateTimeOffset.UtcNow.AddDays(-10).ToString("O"));
            File.WriteAllText(Path.Combine(session, "fb_dummy_0000.ts"), "x");
            File.SetLastWriteTimeUtc(Path.Combine(session, "fb_dummy_0000.ts"), DateTime.UtcNow.AddDays(-10));

            FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(temp, Path.Combine(temp, "current"));

            Assert.False(Directory.Exists(session)); // expired preserve => reclaimed
        }
        finally { Directory.Delete(temp, recursive: true); }
    }

    [Fact]
    public void PreservedRecoveryDirectory_WithinRetention_IsKept()
    {
        var temp = Directory.CreateTempSubdirectory("fbtest_").FullName;
        try
        {
            var session = Path.Combine(temp, new string('b', 32));
            Directory.CreateDirectory(session);
            File.WriteAllText(Path.Combine(session, ".flashback-recovery-preserve"),
                DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
            File.WriteAllText(Path.Combine(session, "fb_dummy_0000.ts"), "x");
            File.SetLastWriteTimeUtc(Path.Combine(session, "fb_dummy_0000.ts"), DateTime.UtcNow.AddDays(-1));

            FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(temp, Path.Combine(temp, "current"));

            Assert.True(Directory.Exists(session));
        }
        finally { Directory.Delete(temp, recursive: true); }
    }
}
```

Check `FlashbackBufferOptions` (in `Sussudio/Models/Capture/CaptureModels.cs` or nearby — locate with grep) before writing; add the `FreeDiskBytesProvider` property there as `public Func<long>? FreeDiskBytesProvider { get; init; }` defaulting to null (null → use `FlashbackStartupCacheCleanup.TryGetTempDriveAvailableFreeBytes`). The tests must compile against the real option type. Note `IsDiskSpaceLow`/`IsDiskCriticallyLow` may need an `Initialize` call first depending on where the probe runs — if so, initialize with a temp directory in the tests.

- [ ] **Step 2: Run tests, verify FAIL** (no such members yet).

- [ ] **Step 3: Implement the free-space policy in `FlashbackBufferManager`:**

```csharp
private const long SoftMinFreeDiskBytes = 2L * 1024 * 1024 * 1024;   // evict below this
private const long HardMinFreeDiskBytes = 512L * 1024 * 1024;        // critical below this
private const int FreeDiskProbeIntervalMs = 5_000;
private long _lastFreeDiskProbeMs;
private long _lastFreeDiskBytes = -1;

public bool IsDiskSpaceLow
{
    get { var free = ProbeFreeDiskBytes(); return free >= 0 && free < SoftMinFreeDiskBytes; }
}

public bool IsDiskCriticallyLow
{
    get { var free = ProbeFreeDiskBytes(); return free >= 0 && free < HardMinFreeDiskBytes; }
}

private long ProbeFreeDiskBytes()
{
    var now = Environment.TickCount64;
    if (now - Interlocked.Read(ref _lastFreeDiskProbeMs) < FreeDiskProbeIntervalMs)
    {
        return Interlocked.Read(ref _lastFreeDiskBytes);
    }

    Interlocked.Exchange(ref _lastFreeDiskProbeMs, now);
    var free = _options.FreeDiskBytesProvider?.Invoke()
        ?? FlashbackStartupCacheCleanup.TryGetTempDriveAvailableFreeBytes(_options.TempDirectory);
    Interlocked.Exchange(ref _lastFreeDiskBytes, free);
    return free;
}
```

In `UpdateDiskBytes`, after the existing over-budget branch and **inside the same `lock (_indexLock)`**, add low-space eviction (the probe itself must be called *before* taking the lock — capture `var freeBytes = ProbeFreeDiskBytes();` at method top so no syscall runs under `_indexLock`):

```csharp
// Free-space pressure: evict oldest completed segments regardless of the
// configured budget when the drive itself is running out. Never evict the
// last completed segment (playback/export need at least one).
if (!(Volatile.Read(ref _evictionPauseCount) > 0) &&
    freeBytes >= 0 && freeBytes < SoftMinFreeDiskBytes &&
    _completedSegments.Count > 1)
{
    var deficit = SoftMinFreeDiskBytes - freeBytes;
    long reclaimed = 0;
    while (_completedSegments.Count > 1 && reclaimed < deficit)
    {
        var oldest = _completedSegments[0];
        if (!TryCreatePendingEvictionDelete(oldest.Path, oldest.SizeBytes, "low_disk", out var pendingDelete))
        {
            break;
        }
        EnsurePendingEvictionDeleteList(ref pendingDeletes).Add(pendingDelete);
        reclaimed = AddNonNegativeSaturated(reclaimed, oldest.SizeBytes);
        _completedSegmentBytes = SubtractNonNegative(_completedSegmentBytes, oldest.SizeBytes);
        _totalDiskBytes = SubtractNonNegative(_totalDiskBytes, oldest.SizeBytes);
        _completedSegments.RemoveAt(0);
    }
    if (reclaimed > 0)
    {
        Interlocked.Exchange(ref _validStartPtsTicks,
            Math.Max(Interlocked.Read(ref _validStartPtsTicks),
                     _completedSegments[0].StartPts.Ticks));
        Logger.Log($"FLASHBACK_BUFFER_LOW_DISK_EVICT free_bytes={freeBytes} reclaimed_bytes={reclaimed} remaining_segments={_completedSegments.Count}");
    }
}
```

Note: `_validStartPtsTicks` must advance to the new oldest segment's start so playback/UI don't reference evicted time. Respect `_evictionPauseCount` (recording/export in flight) — during a pause we accept the risk and do not evict.

- [ ] **Step 4: Implement preserved-session aging in `FlashbackStartupCacheCleanup`:**

```csharp
internal static readonly TimeSpan RecoveryPreserveRetention = TimeSpan.FromDays(7);

private static bool IsPreserveMarkerActive(string sessionDirectory, DateTime nowUtc)
{
    var markerPath = Path.Combine(sessionDirectory, RecoveryPreserveMarkerFileName);
    if (!File.Exists(markerPath))
    {
        return false;
    }

    DateTime markerUtc;
    try
    {
        var text = File.ReadAllText(markerPath).Trim();
        markerUtc = DateTimeOffset.TryParse(text, out var parsed)
            ? parsed.UtcDateTime
            : File.GetLastWriteTimeUtc(markerPath);
    }
    catch
    {
        markerUtc = File.GetLastWriteTimeUtc(markerPath);
    }

    if (nowUtc - markerUtc <= RecoveryPreserveRetention)
    {
        return true;
    }

    Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_EXPIRED dir='{sessionDirectory}' age_days={(nowUtc - markerUtc).TotalDays:F1}");
    return false;
}
```

Replace both raw `File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName))` skip-checks (in `CleanupStaleSessionDirectories` and `CleanupSessionCacheBudget`) with `IsPreserveMarkerActive(fullPath, nowUtc)` (`CleanupSessionCacheBudget` needs a `var nowUtc = DateTime.UtcNow;` local). Duplicate the helper into `FlashbackStartupSessionCacheBudget` or hoist it — both classes are in the same file; a `file`-scoped static helper class is fine.

- [ ] **Step 5: Run the four tests, verify PASS.**
- [ ] **Step 6: Commit** — `git commit -m "feat(flashback): runtime free-disk eviction policy and preserved-session aging"`

---

### Task 3: Encoder sink — export-drain gap, rotation escalation, stop-tail, disk-critical stop (F5, F6, F7 + Task 2 seam)

**Files:**
- Modify: `Sussudio/Services/Flashback/FlashbackEncoderSink.cs`
- Test: `tests/Sussudio.Tests/XUnit.FlashbackSinkHardeningTests.cs` (new)

- [ ] **Step 1: Write failing source-text contract tests:**

```csharp
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackSinkHardeningTests
{
    private static string Source() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackEncoderSink.cs"));

    [Fact]
    public void VideoEnqueue_DuringForceRotateDrain_UsesQueueGuardRatio()
    {
        var method = SourceSlice.Method(Source(), "private string? GetVideoEnqueueRejectReason");
        // Unconditional rejection is the bug; the guard must consider queue depth
        // exactly like the audio path (ForceRotateQueueGuardRatio).
        Assert.Contains("IsForceRotateQueueGuarded", method);
    }

    [Fact]
    public void RotateSegment_EscalatesAfterConsecutiveFailures()
    {
        var source = Source();
        Assert.Contains("MaxConsecutiveRotationFailures = 3", source);
        var method = SourceSlice.Method(source, "private bool RotateSegment");
        Assert.Contains("_consecutiveRotationFailures", method);
        Assert.Contains("FailEncoding", method);
    }

    [Fact]
    public void EndRecording_WaitsForQueueDrain_NotFixedDelay()
    {
        var method = SourceSlice.Method(Source(), "public async Task<FinalizeResult> EndRecordingAsync");
        Assert.DoesNotContain("Task.Delay(100", method);
        Assert.Contains("WaitForEncodeQueueDrainAsync", method);
    }

    [Fact]
    public void EncodingLoop_FailsFast_WhenDiskCriticallyLow()
    {
        var method = SourceSlice.Method(Source(), "private void OnVideoFrameEncoded");
        Assert.Contains("IsDiskCriticallyLow", method);
    }
}
```

(Reuse the same `TestPaths`/`SourceSlice` private helpers pattern as Task 1 — copy into this file.)

- [ ] **Step 2: Run tests, verify all four FAIL.**

- [ ] **Step 3: Force-rotate video guard (F5).** In `GetVideoEnqueueRejectReason` (`:1437`), replace:

```csharp
if (Volatile.Read(ref _forceRotateDraining))
{
    return "force_rotate_draining";
}
```

with depth-aware gating that mirrors the audio path:

```csharp
if (Volatile.Read(ref _forceRotateDraining))
{
    var depth = isGpu ? Volatile.Read(ref _gpuQueueDepth) : Volatile.Read(ref _videoQueueDepth);
    var capacity = isGpu ? GpuQueueCapacity : Volatile.Read(ref _videoQueueCapacity);
    if (IsForceRotateQueueGuarded(depth, capacity))
    {
        return "force_rotate_draining";
    }
}
```

**Bound the drain** so producers feeding during the drain can't extend it indefinitely: in `ProcessPendingForceRotate` (`:2356`), snapshot the depths once before the drain loops (`var videoBudget = Volatile.Read(ref _videoQueueDepth);` etc.) and pass those snapshots as the `maxPackets` argument of each `Drain*Packets` call chain instead of looping `while (Drain...(reader, BatchLimit))` unbounded — e.g. loop while `drained < snapshotBudget` draining in existing batch sizes. Frames enqueued after the snapshot land in the post-rotation segment, which is correct: exports cut by PTS range, so extra frames after the out-point are harmless, and the DVR buffer keeps continuity (the gap F5 describes disappears).

- [ ] **Step 4: Rotation-failure escalation (F6).** Add a field + const near the other counters:

```csharp
private const int MaxConsecutiveRotationFailures = 3;
private int _consecutiveRotationFailures;
```

In `RotateSegment` success path (just before `return true`): `Interlocked.Exchange(ref _consecutiveRotationFailures, 0);`. In the catch block, after the existing `Interlocked.Increment(ref _segmentRotationFailures);`:

```csharp
var consecutive = Interlocked.Increment(ref _consecutiveRotationFailures);
if (consecutive >= MaxConsecutiveRotationFailures)
{
    Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL_ESCALATE consecutive={consecutive}");
    FailEncoding(new IOException(
        $"Flashback segment rotation failed {consecutive} consecutive times: {ex.Message}", ex));
}
```

`FailEncoding` already notifies `_onFatalError`, which (after Task 1) preserves history and auto-restarts — a fresh sink gets a fresh segment file, which is the actual recovery for a wedged rotation.

- [ ] **Step 5: Drain-aware recording end (F7).** Replace `await Task.Delay(100, cancellationToken)` in `EndRecordingAsync` with:

```csharp
await WaitForEncodeQueueDrainAsync(TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);
```

and add:

```csharp
private async Task WaitForEncodeQueueDrainAsync(TimeSpan timeout, CancellationToken cancellationToken)
{
    var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
    while (Environment.TickCount64 < deadline)
    {
        if (Volatile.Read(ref _videoQueueDepth) == 0 &&
            Volatile.Read(ref _audioQueueDepth) == 0 &&
            Volatile.Read(ref _microphoneQueueDepth) == 0 &&
            Volatile.Read(ref _gpuQueueDepth) == 0)
        {
            return;
        }

        if (Volatile.Read(ref _encodingFailure) != null || _encodingTask?.IsCompleted == true)
        {
            return; // loop is dead; waiting longer cannot drain anything
        }

        await Task.Delay(15, cancellationToken).ConfigureAwait(false);
    }

    Logger.Log($"FLASHBACK_RECORDING_END_DRAIN_TIMEOUT vq={Volatile.Read(ref _videoQueueDepth)} aq={Volatile.Read(ref _audioQueueDepth)}");
}
```

- [ ] **Step 6: Disk-critical fail-fast (Task 2 seam).** In `OnVideoFrameEncoded`, inside the existing 4 Hz disk-bytes refresh branch (`if (nowMs - _lastDiskBytesUpdateMs >= 250)`), add after `UpdateDiskBytes`:

```csharp
if (_bufferManager.IsDiskCriticallyLow)
{
    FailEncoding(new IOException(
        "Flashback stopped: drive with the flashback cache is critically low on space."));
    return;
}
```

(Compiles only after Task 2 merges — in Wave 1 both land together before the wave build; if Task 2 slips, gate this step behind its merge.)

- [ ] **Step 7: Run the four tests, verify PASS.**
- [ ] **Step 8: Commit** — `git commit -m "fix(flashback): close export-drain gap, escalate rotation failures, drain-aware stop, disk-critical stop"`

---

### Task 4: Playback resume — keep prebuffered frames, add StateChanged event (F4, F8-API)

**Files:**
- Modify: `Sussudio/Services/Flashback/FlashbackPlaybackController.cs` (`PrimePlaybackAudioBuffer:2251-2364`, `SetState:880-886`, constants block)
- Modify: `Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs` (snap paths pass a reason)
- Modify: `Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs` (state calls updated to pass reasons)
- Test: `tests/Sussudio.Tests/XUnit.FlashbackResumeHardeningTests.cs` (new)

- [ ] **Step 1: Write failing contract tests:**

```csharp
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackResumeHardeningTests
{
    private static string ControllerSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Services/Flashback/FlashbackPlaybackController.cs"));

    [Fact]
    public void Prime_KeepsCpuFrames_InPrebufferQueue()
    {
        var method = SourceSlice.Method(ControllerSource(), "private void PrimePlaybackAudioBuffer");
        Assert.Contains("prebufferedFrames.Enqueue(", method);
        Assert.Contains("PlaybackAudioPrebufferMaxHeldFrames", method);
    }

    [Fact]
    public void Prime_SkipsRewind_WhenAllFramesKept()
    {
        var method = SourceSlice.Method(ControllerSource(), "private void PrimePlaybackAudioBuffer");
        // The rewind (and its re-decode) must only run when frames were released.
        Assert.Contains("if (releasedAnyFrame && decodedFrames > 0)", method);
    }

    [Fact]
    public void SetState_RaisesStateChangedEvent()
    {
        var source = ControllerSource();
        Assert.Contains("public event Action<FlashbackPlaybackState, FlashbackPlaybackState, string>? StateChanged;", source);
        var method = SourceSlice.Method(source, "private void SetState");
        Assert.Contains("StateChanged?.Invoke(oldState, newState, reason)", method);
    }
}
```

- [ ] **Step 2: Run tests, verify FAIL.**

- [ ] **Step 3: Keep-frames prime (F4).** Add a constant next to the other `PlaybackAudioPrebuffer*` constants:

```csharp
// Cap on decoded video frames held across the audio prebuffer. CPU frames only:
// a D3D11VA frame pins a decoder-pool surface, and pool depth is not guaranteed
// to cover the prebuffer budget, so hardware frames keep the release+rewind path.
private const int PlaybackAudioPrebufferMaxHeldFrames = 32;
```

In `PrimePlaybackAudioBuffer`, replace the unconditional release of decoded frames:

```csharp
decodedFrames++;
ReleaseHeldFrameBestEffort(frame, $"audio_prebuffer_{operation}");
prebufferReleasedFrames++;
```

with keep-or-release (all-or-nothing per resume: once one frame is released, release the rest — a partial queue plus a forward decoder position would leave a hole in the middle):

```csharp
decodedFrames++;
if (!releasedAnyFrame &&
    !frame.IsD3D11Texture &&
    prebufferedFrames.Count < PlaybackAudioPrebufferMaxHeldFrames)
{
    prebufferedFrames.Enqueue(frame);
}
else
{
    if (!releasedAnyFrame && prebufferedFrames.Count > 0)
    {
        // Cap hit (or hw frame appeared): fall back wholesale to the rewind path.
        ClearPrebufferedFrames(prebufferedFrames, $"prebuffer_cap_{operation}");
    }
    releasedAnyFrame = true;
    ReleaseHeldFrameBestEffort(frame, $"audio_prebuffer_{operation}");
    prebufferReleasedFrames++;
}
```

with `var releasedAnyFrame = false;` declared alongside the other locals. Then gate the rewind:

```csharp
if (releasedAnyFrame && decodedFrames > 0)
{
    rewound = TryRewindPlaybackAudioPrebuffer(decoder, ref fileOpen, resumeTarget, operation, prebufferAudioGateTicks, cancellationToken);
}
```

Check the discard branch above it (`bufferedMs > PlaybackAudioPrebufferDiscardThresholdMs`): it already calls `ClearPrebufferedFrames`, which correctly releases kept frames — but it must also set `releasedAnyFrame = true` so the rewind still runs in that path. Verify `DecodedVideoFrame` exposes `IsD3D11Texture` (see `FlashbackDecoder.cs:798`); if the property name differs, use the actual one. Add `released_any={releasedAnyFrame} held={prebufferedFrames.Count}` to the existing `FLASHBACK_PLAYBACK_AUDIO_PREBUFFER` log line.

**Invariant to preserve:** `prebufferedFrames` is cleared at the start of every command (`ThreadCommands.cs:382`), on thread exit, and consumed first by `TryReadNextPlaybackFrame` (`PlaybackFrames.cs:589`). Do not add new clear sites; the existing ones already cover the kept frames.

- [ ] **Step 4: StateChanged event (F8 producer side).** In `FlashbackPlaybackController.cs`:

```csharp
public event Action<FlashbackPlaybackState, FlashbackPlaybackState, string>? StateChanged;

private void SetState(FlashbackPlaybackState newState, string reason = "")
{
    var oldState = _state;
    if (oldState == newState) return;
    _state = newState;
    Logger.Log($"FLASHBACK_PLAYBACK_STATE {oldState} -> {newState} reason='{reason}'");
    try
    {
        StateChanged?.Invoke(oldState, newState, reason);
    }
    catch (Exception ex)
    {
        Logger.Log($"FLASHBACK_PLAYBACK_STATE_EVENT_WARN type={ex.GetType().Name} msg='{ex.Message}'");
    }
}
```

Update the involuntary-transition call sites to pass reasons — in `RestoreLiveAfterDecoderPlaybackFailure` (`PlaybackFrames.cs:1141-1160`) pass the `operation` string it already receives (`SetState(FlashbackPlaybackState.Live, operation)`); user-initiated sites (`ThreadCommands.cs` GoLive/Play/Pause/Seek handlers) pass `"user"`. Every existing `SetState(...)` call keeps compiling via the default parameter; only add reasons where the value is at hand — but the two named above are required.

- [ ] **Step 5: Run tests, verify PASS.**
- [ ] **Step 6: Commit** — `git commit -m "perf(flashback): keep prebuffered frames on resume; raise StateChanged with reason"`

**Runtime verification (orchestrator):** with `FlashbackGpuDecode=false` at 1080p, resume mid-buffer; `FLASHBACK_PLAYBACK_AUDIO_PREBUFFER ... released_any=false rewound=false` and `FLASHBACK_PLAYBACK_CMD_COMPLETE kind=Play duration_ms=` should drop ~3× vs. baseline. With GPU decode on, `released_any=true rewound=true` (unchanged path).

---

### Task 5: UI health surfacing (F1-UI, F8-UI)

**Files:**
- Modify: `Sussudio/ViewModels/MainViewModel.FlashbackState.cs`
- Modify: `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`
- Modify: `Sussudio/MainWindow.xaml` + `Sussudio/MainWindow.xaml.cs` (only if a new InfoBar element is required — reuse the existing disk-warning InfoBar pattern)
- Test: `tests/Sussudio.Tests/XUnit.FlashbackUiHealthTests.cs` (new)

**Depends on:** Task 1 (failure status), Task 4 (`StateChanged`). Wave 2.

- [ ] **Step 1: Write failing contract tests** asserting: (a) `MainViewModel.FlashbackState.cs` contains a `FlashbackHealthMessage` property raised via the existing `PropertyChanged` switch pattern; (b) the VM subscribes `StateChanged` and marshals via `DispatcherQueue`; (c) any new XAML control carries a **new** `AutomationProperties.AutomationId` (`FlashbackHealthInfoBar`) and no existing AutomationId string in `MainWindow.xaml` changed (snapshot-compare the list of AutomationIds against the checked-in baseline the tests in `XUnit.AutomationContractsTests.cs` use — follow that file's existing pattern for AutomationId inventory).

- [ ] **Step 2: Run tests, verify FAIL.**

- [ ] **Step 3: Implement.** Three behaviors, following the manual-binding conventions (PropertyChanged switch + `SetupBindings` — no x:Bind):
  1. **Involuntary snap notice:** subscribe to `FlashbackPlaybackController.StateChanged` (the controller instance is reachable the same way the 250 ms poll reaches playback state — go through the session coordinator; add a pass-through event if the VM has no direct reference). When `newState == Live` and `reason` is not `"user"` / `"preview_detach"`, set `FlashbackHealthMessage = "Returned to live — playback error."` and clear it after 5 s via `DispatcherQueueTimer`.
  2. **Dead-backend banner:** in the existing 250 ms poll (`UpdateFlashbackStatus`, around `MainViewModel.FlashbackState.cs:380-410`), when the flashback toggle is enabled but `bufferStatus` reports inactive, set `FlashbackHealthMessage = "Flashback is not running — use Restart Flashback."` (persistent, not timed). Bind the InfoBar's action button to the existing restart command path in `FlashbackUiControllers.cs`.
  3. **Wire the InfoBar** in `MainWindow.xaml` next to the disk-warning InfoBar, `AutomationProperties.AutomationId="FlashbackHealthInfoBar"`.

  Note: because the playback controller is rebuilt on every backend cycle (`FlashbackBackendResources.CycleSinkOnlyAsync` creates a new `FlashbackPlaybackController`), the subscription must re-attach when the controller instance changes — do this in the poll: cache the last-seen controller reference, resubscribe on change, and unsubscribe from the stale one.

- [ ] **Step 4: Run tests, verify PASS.**
- [ ] **Step 5: Commit** — `git commit -m "feat(flashback): surface playback snaps and dead-backend state in UI"`

---

### Task 6: UX seams — pause-frame accuracy, pre-warm, GapFromLive (F9)

**Files:**
- Modify: `Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs` (pause path `:555-618`)
- Modify: `Sussudio/Services/Flashback/FlashbackPlaybackController.cs` (`GapFromLive` getter `:170-180`; pre-warm API)
- Modify: `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` (call pre-warm when the timeline panel opens / `StartStatusPolling`)
- Test: `tests/Sussudio.Tests/XUnit.FlashbackUxSeamTests.cs` (new)

**Depends on:** Task 4 merged (same files). Wave 2. Scrub settle refinement (June-10 P2-2) is **deliberately excluded** — it needs decoder-loop work that isn't safely Sonnet-sized; revisit after this plan lands.

- [ ] **Step 1: Write failing contract tests** asserting: (a) `HandlePauseCommand`'s pause-from-live branch contains a bounded forward-decode from the keyframe to the pause target (`DecodeForwardToTarget` or the actual helper the implementer factors out — pin the call, and pin a frame-budget constant `PauseFromLiveMaxForwardDecodeFrames`); (b) `FlashbackPlaybackController.cs` contains `public void PreWarm()` that calls `EnsurePlaybackThread` without issuing a command; (c) the `GapFromLive` getter contains a fallback branch for `_lastVideoPtsTicks == 0` computing from `LatestPts - (PlaybackPosition + frozenValidStart)` equivalent state.

- [ ] **Step 2: Run tests, verify FAIL.**

- [ ] **Step 3: Implement.**
  1. **Pause-frame accuracy:** after `SeekAndDisplayKeyframe` succeeds in the pause-from-live branch, forward-decode (release each intermediate frame) until frame PTS ≥ pause target or `PauseFromLiveMaxForwardDecodeFrames` (`(int)Math.Ceiling(_bufferManager.EncodeFrameRate)` clamped to [30, 240] — one GOP) frames decoded, then display the final frame the same way `SeekAndDisplayKeyframe` submits it. Honor `commandChannel.Reader.TryPeek` between frames — bail to the existing keyframe display if a command is queued.
  2. **Pre-warm:** `PreWarm()` = `EnsurePlaybackThread(CommandKind.Pause)` guarded by `_initialized && !_disposedFlag`; swallow-and-log failures (`FLASHBACK_PLAYBACK_PREWARM`). Call it from the UI controller where the flashback timeline panel becomes visible (`FlashbackUiControllers.cs` — locate `StartStatusPolling` or panel-visibility handler and call once).
  3. **GapFromLive fallback:** in the getter, when `_lastVideoPtsTicks == 0` and state is not Live, return `LatestPts - (PlaybackPosition + ValidStartPts)` clamped non-negative (read the exact deferred-display fields used at `:170-180` and mirror their semantics).

- [ ] **Step 4: Run tests, verify PASS.**
- [ ] **Step 5: Commit** — `git commit -m "feat(flashback): accurate pause frame, playback pre-warm, GapFromLive fallback"`

---

### Task 7: Wave verification + runtime evidence (orchestrator, not a subagent)

- [ ] **Step 1:** After each wave: confirm app closed (MCP `get_app_state`; `window_action(close, armClose=true)` if previewing), build, run full test suite (expect ≥ the current 907 passing plus the new files), fix staleness gotcha if the harness flags the dll.
- [ ] **Step 2:** Read `temp/logs/Sussudio_Debug.log` after launching the app; watch ≤60 s per the CLAUDE.md tail protocol.
- [ ] **Step 3:** Run the existing diagnostic scenarios (`tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs`, `...StressScenario.cs`) via ssctl; compare `PlaybackDroppedFrames`, `RenderSilenceCount`, `MaxCommandQueueLatencyMs`, `FLASHBACK_AV_DRIFT` against a pre-change baseline run (capture the baseline **before** Wave 1 merges).
- [ ] **Step 4:** Targeted runtime checks per task: Task 1 fatal-path log sequence; Task 3 export-during-capture with no `FLASHBACK_SINK_VIDEO_QUEUE_REJECT reason=force_rotate_draining` storm and no buffer gap around the export PTS; Task 4 software-decode resume timing.
- [ ] **Step 5 (F10 probe, before any drift fix is even planned):** long capture session with induced video queue saturation (stress scenario), then check `FLASHBACK_AV_DRIFT` trend vs `_droppedVideoFrames`. File the result in `docs/experiment_log.md`; only if drift correlates does a future task get written.

---

## Self-review notes

- **Spec coverage:** F1→Task 1, F2/F3→Task 2, F5/F6/F7→Task 3, F4/F8→Task 4, F1-UI/F8-UI→Task 5, F9→Task 6 (minus scrub-settle, explicitly deferred), F10→Task 7 Step 5 (probe only, per the instrument-before-theorizing rule).
- **Known compile-order seam:** Task 3 Step 6 depends on Task 2's `IsDiskCriticallyLow`; both are Wave 1 and the wave builds together. If Task 2 is rejected in review, skip Task 3 Step 6 and its fourth contract test.
- **Line numbers** are anchors from commit `e762c8c2`; implementers must re-locate by symbol name, not raw line, if drift occurred.
- **Type consistency check:** `IsDiskCriticallyLow` (Tasks 2/3), `StateChanged`/`SetState(newState, reason)` (Tasks 4/5), `PlaybackAudioPrebufferMaxHeldFrames` (Task 4 code + test), `TryScheduleFlashbackAutoRestart` (Task 1 code + test) — names match across tasks.
