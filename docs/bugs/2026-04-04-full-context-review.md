# Full-Context Code Review — 2026-04-04

Bugs found by loading the entire ElgatoCapture app (53K LOC, 97 files) into
a single 1M-token context window and cross-referencing all layers at once.

---

## 1. Analog Audio Gain Slider Floods XU Commands

**Severity:** High
**Risk:** Hardware damage (same class as 2026-03-12 AT SET bricking incident)

**Files:**
- `MainWindow.Bindings.cs:644` — `AnalogAudioGainSlider.ValueChanged`
- `MainViewModel.cs:923-943` — `OnAnalogAudioGainPercentChanged`
- `MainViewModel.AudioControls.cs:237-263` — `ApplyAnalogAudioGainAsync`

**Problem:**
Every slider `ValueChanged` tick sets `AnalogAudioGainPercent`, which triggers
`OnAnalogAudioGainPercentChanged`, which enqueues `ApplyAnalogAudioGainAsync` —
sending an XU `SetAnalogGainAsync` write to the capture device hardware.
Dragging the slider generates dozens of XU writes per second.

The `_gainFlashDebounceCts` only debounces the flash-persist step, not the
actual gain write. There is no throttle on the XU write itself.

**Fix options:**
- (A) Debounce: throttle `OnAnalogAudioGainPercentChanged` to fire at most
  every ~200ms.
- (B) Move the XU write to `PointerCaptureLost` (slider release), matching
  the pattern used by the preview volume slider.

---

## 2. CaptureSettingsGrid Narrow Mode Hides Video Format

**Severity:** Medium
**Risk:** Video Format dropdown invisible when window is narrow

**Files:**
- `MainWindow.Bindings.cs:812-817` — `CaptureSettingsGrid_SizeChanged`
- `MainWindow.xaml:862-874` — CaptureSettingsGrid column definitions

**Problem:**
In narrow mode (<700px), the code collapses `VideoFormatColumn` (column 0) to
`Width = 0`, then moves `VideoFormatPanel` to Row 1, Column 0 — the same
zero-width column:

```csharp
VideoFormatColumn.Width = new GridLength(0);  // Column 0 → zero width
Grid.SetColumn(VideoFormatPanel, 0);          // Panel → zero-width column
```

Columns 1-4 still have star width. The Video Format dropdown becomes invisible
and inaccessible.

**Fix:** Place `VideoFormatPanel` in a non-collapsed column in narrow mode
(e.g., column 1), or don't collapse column 0 — instead clear its star width
and let the panel's row-1 placement provide the layout.

---

## 3. Flashback In/Out Points Set Twice with Divergent Values

**Severity:** Medium
**Risk:** Timeline markers don't match actual export range

**Files:**
- `MainWindow.Flashback.cs:219-228` — `FlashbackInButton_Click`, `FlashbackOutButton_Click`
- `MainViewModel.Automation.cs:100-104` — `FlashbackSetInPoint`, `FlashbackSetOutPoint`
- `Services/Flashback/FlashbackPlaybackController.cs:222-232` — `SetInPoint`, `SetOutPoint`
- `ViewModels/MainViewModel.cs:380-383` — `FlashbackInPoint`, `FlashbackOutPoint` properties

**Problem:**
```csharp
private void FlashbackInButton_Click(object sender, RoutedEventArgs e)
{
    ViewModel.FlashbackSetInPoint();                                  // reads controller.PlaybackPosition
    ViewModel.FlashbackInPoint = ViewModel.FlashbackPlaybackPosition; // reads VM property separately
}
```

`FlashbackSetInPoint()` calls `controller.SetInPoint()` which captures
`controller.PlaybackPosition`. Then the code-behind separately reads
`ViewModel.FlashbackPlaybackPosition`. These are two reads at two different
moments — if playback advances between them, the controller's in-point and
the VM's in-point diverge.

The controller holds the real export value; the VM value drives the UI markers.
Result: the yellow in/out markers on the timeline don't match the actual export
trim range.

**Fix:** Have `FlashbackSetInPoint()` return the captured position, and use
that same value for both the controller and the VM property.

---

## 4. Debounce Timers Not Stopped on Window Close

**Severity:** Low-Medium
**Risk:** UI manipulation during shutdown, possible crash

**Files:**
- `MainWindow.Animations.cs:430-494` — `_liveSignalDebounceTimer`, `_liveSignalHideDebounceTimer`
- `MainWindow.FullScreen.cs:490-512` — `_fullScreenAutoHideTimer`
- `MainWindow.WindowManagement.cs:100-183` — `MainWindow_Closed`

**Problem:**
`_liveSignalDebounceTimer`, `_liveSignalHideDebounceTimer`, and
`_fullScreenAutoHideTimer` are never stopped in `MainWindow_Closed`. Their
tick callbacks don't check `_isWindowClosing` and manipulate UI elements
(visibility, opacity, animations) during the shutdown sequence.

Most other timers are explicitly stopped in `MainWindow_Closed`
(`_audioMeterAnimationTimer`, `_statsPollTimer`, `_flashbackStatusTimer`).
These three are missed.

**Fix:** Add cleanup in `MainWindow_Closed` before the
`ViewModel.PropertyChanged -= ...` line:
```csharp
_liveSignalDebounceTimer?.Stop();
_liveSignalHideDebounceTimer?.Stop();
StopFullScreenAutoHideTimer();
```

---

## 5. Audio Meter Uses Legacy DispatcherTimer

**Severity:** Low-Medium
**Risk:** Visual jitter in audio meter animation

**Files:**
- `MainWindow.AudioMeter.cs:157` — `InitializeAudioMeterBrushes`
- `MainWindow.xaml.cs:151` — `_audioMeterAnimationTimer` declaration

**Problem:**
The audio meter animation is the only timer using `DispatcherTimer`
(WPF/UWP legacy). Every other timer in the codebase uses
`DispatcherQueueTimer` (WinUI 3 native).

`DispatcherTimer` in WinUI 3 has worse precision and can skip ticks under
load. At 16ms intervals (60fps audio meter), this causes visible jitter
compared to the smooth 30Hz flashback timer using `DispatcherQueueTimer`.

**Fix:** Replace with `DispatcherQueueTimer`:
```csharp
_audioMeterAnimationTimer = _dispatcherQueue.CreateTimer();
_audioMeterAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
_audioMeterAnimationTimer.IsRepeating = true;
_audioMeterAnimationTimer.Tick += (_, _) => AnimateAudioMeterTick();
```

Update the field type from `DispatcherTimer?` to `DispatcherQueueTimer?` in
`MainWindow.xaml.cs`.

---

## 6. EnsurePreviewPlaybackStarted Is a Dead No-Op

**Severity:** Low
**Risk:** Dead code, misleading name

**Files:**
- `MainWindow.PreviewStartup.cs:313-318`

**Problem:**
```csharp
private void EnsurePreviewPlaybackStarted(string reason, bool recoveryAttempt)
{
    Logger.Log($"PREVIEW_START_PLAY_SKIPPED ...");
}
```

Method only logs "SKIPPED" and never ensures anything. Leftover from the
MediaPlayer-based renderer replaced by D3D11. No current callers exist.

**Fix:** Delete the method.

---

## 7. Volume Change During Entrance Animation Is Lost

**Severity:** Low
**Risk:** User's manual volume adjustment silently discarded

**Files:**
- `MainWindow.Bindings.cs:397-408` — `PreviewVolumeSlider` event handlers
- `MainWindow.Animations.cs:264-296` — entrance animation completion

**Problem:**
If the user grabs the volume slider during the entrance fade-in animation,
`_isVolumeFadingIn` is `true`, so `PointerCaptureLost` skips
`SavePreviewVolume()`. The animation completion handler then overwrites
`PreviewVolume` with `_savedPreviewVolume`. The user's manual adjustment
vanishes.

**Fix:** In `PointerCaptureLost`, if the user explicitly interacts during
fade-in, cancel the animation and honor their choice:
```csharp
PreviewVolumeSlider.PointerCaptureLost += (s, e) =>
{
    if (_isVolumeFadingIn)
    {
        _isVolumeFadingIn = false;
        ViewModel.SuppressVolumeSave = false;
        ViewModel.VolumeSaveOverride = null;
        _savedPreviewVolume = ViewModel.PreviewVolume;
    }
    ViewModel.SavePreviewVolume();
};
```
