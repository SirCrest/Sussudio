# Fix: Reinit Crash — Stop Renderer Before Pipeline Teardown

**Date:** 2026-04-05
**Status:** Design
**Blocks:** Internal demo shipping

## Problem

The 2nd pipeline reinit (resolution/HDR/FPS change) crashes the app with an uncatchable `AccessViolationException`. The 1st reinit from a fresh launch works. All 11 BLOCKED tests in the QA matrix (2026-04-04) trace to this bug.

## Root Cause

The D3D11 preview renderer is alive and actively calling `VideoProcessorBlt`/`Present` on the shared D3D11 device during the entire pipeline teardown sequence. The teardown order is:

```
StopVideoPreviewAsync (coordinator thread):
  1. DisposeFlashbackPreviewBackendAsync   ← drains encoder (100s of ms)
  2. unifiedVideoCapture.StopAsync         ← stops source reader read loop
  3. unifiedVideoCapture.DisposeAsync      ← releases MF source reader COM + SharedD3DDeviceManager
IsPreviewing = false (UI thread):
  4. StopPreviewRendererAsync              ← renderer finally stopped
```

The renderer runs through steps 1–3. On main (no flashback), step 1 doesn't exist and steps 2–3 are fast (~ms). On the Flashback branch, the flashback encoder drain widens the window to hundreds of milliseconds.

The 1st reinit works because the initial session uses MJPG CPU decode (no D3D sharing). After reinit, the new session switches to NV12 with a D3D11-backed source reader sharing the device with the renderer. The 2nd reinit then tears down a D3D-backed source reader while the renderer is actively using the same shared device — the COM `ReleaseComObject` on the source reader triggers MF-internal DXGI cleanup that races with the renderer's native D3D calls.

## Fix

Stop the renderer **before** the capture pipeline teardown, not after.

### Current flow

```
ReinitializeDeviceAsync:
  NotifyPreviewReinitRequestedAsync  → animate out (visual only)
  StopPreviewAsync:
    PreviewStopRequested event       → UI stops watchdog/overlay
    await StopVideoPreviewAsync()    → flashback drain + UVC dispose (renderer alive)
    IsPreviewing = false             → PropertyChanged → StopPreviewRendererAsync
```

### Fixed flow

```
ReinitializeDeviceAsync:
  NotifyPreviewReinitRequestedAsync  → animate out (visual only)
  NotifyRendererStopAsync            → NEW: renderer.Stop() + SetPreviewFrameSink(null)
  StopPreviewAsync:
    PreviewStopRequested event       → UI stops watchdog/overlay
    await StopVideoPreviewAsync()    → flashback drain + UVC dispose (renderer already dead)
    IsPreviewing = false             → PropertyChanged → StopPreviewRendererAsync (no-op, already cleaned up)
```

### Why this works for both crash hypotheses

- **If AVE is in renderer** (swapchain race): renderer is stopped before the timing window opens. No native D3D calls in flight during teardown.
- **If AVE is in source reader** (DXGI handle cleanup): stopping the renderer first releases its AddRef on the shared device. The source reader's DXGI handle is the only remaining reference — clean single-owner disposal.

### Changes

#### 1. MainViewModel — add pre-teardown renderer stop event

**File:** `MainViewModel.cs`

Add a new event:
```csharp
public event Func<Task>? PreviewRendererStopRequested;
```

Add helper:
```csharp
private async Task NotifyRendererStopAsync()
{
    var handlers = PreviewRendererStopRequested;
    if (handlers == null) return;
    foreach (Func<Task> handler in handlers.GetInvocationList())
    {
        await handler();
    }
}
```

#### 2. MainViewModel.Capture — call renderer stop before pipeline teardown

**File:** `MainViewModel.Capture.cs`, in `ReinitializeDeviceAsync`

After `NotifyPreviewReinitRequestedAsync(reason)` and before `StopPreviewAsync`:
```csharp
await NotifyRendererStopAsync();
```

#### 3. MainWindow — subscribe and implement

**File:** `MainWindow.xaml.cs` (subscription), `MainWindow.PropertyChanged.cs` (handler)

Subscribe:
```csharp
ViewModel.PreviewRendererStopRequested += ViewModel_PreviewRendererStopRequested;
```

Handler:
```csharp
private Task ViewModel_PreviewRendererStopRequested()
{
    var renderer = _d3dRenderer;
    if (renderer != null)
    {
        ViewModel.SetPreviewFrameSink(null);
        renderer.Stop();
    }
    return Task.CompletedTask;
}
```

This stops the render thread synchronously (joins it) and disconnects the frame sink. The renderer object stays alive — `CleanupPreviewResources` in `StopPreviewRendererAsync` will dispose it when `IsPreviewing` goes false.

#### 4. MainWindow.PreviewRenderer — make StopPreviewRendererAsync idempotent

**File:** `MainWindow.PreviewRenderer.cs`, `CleanupPreviewResources`

Already safe: `renderer.Stop()` checks `_renderThread == null` under lock and returns early if already stopped. `renderer.Dispose()` checks `_disposed` with CAS. No change needed, but worth a comment.

#### 5. MainWindow.WindowManagement — unsubscribe on close

**File:** `MainWindow.WindowManagement.cs`

Add alongside existing unsubscriptions:
```csharp
ViewModel.PreviewRendererStopRequested -= ViewModel_PreviewRendererStopRequested;
```

### 6. CaptureService — stop renderer before fatal cleanup too

**File:** `CaptureService.cs`

The `BeginFatalCaptureCleanup` path (device hot-unplug, source reader fatal error) runs `CleanupAsync` on `Task.Run` — same shared-device race as reinit. Add a `PreCleanupRequested` event that fires before `CleanupAsync`.

```csharp
public event Action? PreCleanupRequested;
```

In `BeginFatalCaptureCleanup`, fire before `CleanupAsync`:
```csharp
try { PreCleanupRequested?.Invoke(); }
catch (Exception preEx) { Logger.Log($"PreCleanupRequested handler warning: {preEx.Message}"); }
```

**File:** `MainViewModel.cs`

Subscribe to `PreCleanupRequested`, invoke `PreviewRendererStopRequested` synchronously:
```csharp
private void OnCapturePreCleanupRequested()
{
    var handlers = PreviewRendererStopRequested;
    if (handlers != null)
    {
        foreach (Func<Task> handler in handlers.GetInvocationList())
        {
            try { handler().GetAwaiter().GetResult(); }
            catch (Exception ex) { Logger.Log($"PreCleanup renderer stop warning: {ex.Message}"); }
        }
    }
}
```

The handler returns `Task.CompletedTask` (synchronous), so `GetAwaiter().GetResult()` completes immediately. `renderer.Stop()` handles cross-thread swapchain unbind internally via `UnbindSwapChainFromPanel` (dispatcher dispatch + 2s wait).

### What this does NOT change

- Zero-copy shared D3D11 device architecture — preserved
- Renderer creation/destruction lifecycle — still fresh renderer per reinit cycle
- Flashback teardown ordering — unchanged
- The `_inNativeCall` fence, `_swapChainBound` CAS, `HandleDeviceLost` guards — all stay as defense-in-depth

### Edge cases

**Renderer stop timeout:** `Stop()` joins the render thread with a 3s timeout then unconditional join. If the render thread is stuck in a native call (device-lost), this blocks the UI thread. Acceptable — the existing `HandleDeviceLost` checks `_stopRequested` and bails, so the render thread should exit promptly.

**Concurrent reinit:** `_previewReinitializeGate` (SemaphoreSlim(1,1)) prevents concurrent `ReinitializeDeviceAsync` calls. The new `NotifyRendererStopAsync` runs inside this gate.

**No renderer active:** If reinit fires before a renderer was created (e.g., device init failure), `_d3dRenderer` is null and the handler is a no-op.

**Animation race:** `NotifyPreviewReinitRequestedAsync` starts the fade-out animation. `NotifyRendererStopAsync` then stops the renderer, which makes the panel go black. The fade-out animation is on `PreviewContentGrid.Opacity`, not on the swapchain content — the visual transition is: content fading out → content goes black (renderer stopped) → pipeline reinit → new renderer starts → content fades back in. The black frame during fade-out is imperceptible.

## Testing

1. `ecctl set resolution 2560x1440` then `ecctl set resolution 1920x1080` — 2nd reinit should not crash
2. Chain: 1080p → 1440p → 4K → 1080p (4 reinits in sequence)
3. HDR toggle on/off/on without restart
4. FPS chain: 120 → 60 → 30 → 120
5. Video format chain: Auto → MJPG → NV12
6. Verify flashback continues encoding through all reinit cycles (0 dropped frames)
7. Record after multiple reinits — verify output integrity

## Files Modified

| File | Change |
|------|--------|
| `MainViewModel.cs` | Add `PreviewRendererStopRequested` event + `NotifyRendererStopAsync` helper |
| `MainViewModel.Capture.cs` | Call `NotifyRendererStopAsync` before `StopPreviewAsync` in `ReinitializeDeviceAsync` |
| `MainWindow.xaml.cs` | Subscribe to `PreviewRendererStopRequested` |
| `MainWindow.PropertyChanged.cs` | Add `ViewModel_PreviewRendererStopRequested` handler |
| `MainWindow.WindowManagement.cs` | Unsubscribe on close |
| `CaptureService.cs` | Add `PreCleanupRequested` event, fire before `CleanupAsync` in `BeginFatalCaptureCleanup` |
| `MainViewModel.cs` | Subscribe to `PreCleanupRequested`, forward to `PreviewRendererStopRequested` |
