using System;
using System.Threading.Tasks;
using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio;

// Preview-specific ViewModel events and property projections. Preview
// startup/teardown choreography and its PropertyChanged routes live here.
public sealed partial class MainWindow
{
    private async Task<bool> TryHandlePreviewPropertyChangedAsync(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                await HandlePreviewingChangedAsync();
                return true;

            case nameof(MainViewModel.IsPreviewReinitializing):
                HandlePreviewReinitializingChanged();
                return true;

            default:
                return false;
        }
    }

    private void ViewModel_PreviewStartRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = false;
        if (ShouldBeginPreviewStartupAttempt)
        {
            BeginPreviewStartupAttempt();
        }

        PrimePreviewAudioFadeIn();
        if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
        {
            PreparePreviewStartupPresentation();
        }
    }

    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _isPreviewReinitAnimating = true;
        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=true caller={nameof(ViewModel_PreviewReinitRequested)}");
        Logger.Log($"PREVIEW_REINIT_ANIMATE_OUT reason={reason}");
        await AnimatePreviewOutAsync();
    }

    private Task ViewModel_PreviewRendererStopRequested()
    {
        // Stop the render thread before the capture pipeline teardown. This ensures
        // no native D3D calls (VideoProcessorBlt/Present) are in flight when
        // UnifiedVideoCapture disposes the shared D3D11 device and DXGI manager.
        //
        // IMPORTANT: this only drains and detaches the active renderer. The later
        // attach step may replace the SwapChainPanel surface for HDR/SDR or mode
        // changes because WinUI can keep native DXGI state behind a panel even
        // after SetSwapChain(null). Replacing the surface happens after capture
        // teardown so the old renderer is no longer receiving frames.
        var renderer = _previewRendererHostController.Renderer;
        if (renderer != null)
        {
            Logger.Log("PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
            try
            {
                DisposeD3DPreviewRendererForReinit();
            }
            catch (TimeoutException ex)
            {
                // Render thread did not exit before its stop timeout. The renderer's
                // stop path has already logged details and the fresh attach path will
                // replace the panel surface if needed. Swallow the exception so reinit
                // can continue rather than crashing the UI thread mid-resolution-change.
                Logger.Log($"PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
            }
        }

        return Task.CompletedTask;
    }

    private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = _previewStopRequestedByUser || !ViewModel.IsPreviewReinitializing;
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
    }

    private async Task HandlePreviewingChangedAsync()
    {
        if (ViewModel.IsPreviewing)
        {
            _previewStopRequestedByUser = false;
            if (ShouldBeginPreviewStartupAttempt)
            {
                BeginPreviewStartupAttempt();
            }

            SetPreviewStartupState(PreviewStartupState.StartingSession);
            Logger.Log($"PREVIEW_SESSION_STARTED attempt={PreviewStartupAttemptLabel}");
            if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
            {
                PreparePreviewStartupPresentation();
            }

            SetPreviewStartupState(PreviewStartupState.RendererAttaching);
            try
            {
                await StartPreviewRendererAsync();
            }
            catch (Exception ex)
            {
                var attachFailureReason = $"renderer-attach-failed:{ex.Message}";
                SetPreviewStartupState(PreviewStartupState.Failed, attachFailureReason);
                StopPreviewStartupWatchdog();
                RevealPreviewUnavailablePlaceholder();
                Logger.Log($"PREVIEW_RENDERER_ATTACH_FAILED attempt={PreviewStartupAttemptLabel} reason={attachFailureReason}");
                SchedulePreviewStartupFailureStop(attachFailureReason);
                throw;
            }

            if (!IsPreviewFirstVisualConfirmed)
            {
                SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual);
                StartPreviewStartupWatchdog();
            }

            ShowStopPreviewButtonPresentation();
            ApplyHdrToggleEnabledState();
            return;
        }

        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        // During reinit, the renderer is kept alive (render thread stopped
        // by ViewModel_PreviewRendererStopRequested, instance preserved).
        // StartPreviewRendererAsync will reuse it via Start().
        if (!ViewModel.IsPreviewReinitializing)
        {
            await StopPreviewRendererAsync();
        }

        if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
        {
            RevealPreviewUnavailablePlaceholder();
        }

        if (ViewModel.IsPreviewReinitializing)
        {
            ShowStopPreviewButtonPresentation();
        }
        else
        {
            ShowStartPreviewButtonPresentation();
        }

        ApplyHdrToggleEnabledState();
        ResetPreviewStartupTracking(preserveReinitAnimation: ViewModel.IsPreviewReinitializing || _isPreviewReinitAnimating);
    }

    private void HandlePreviewReinitializingChanged()
    {
        UpdateDeviceApplyButtonState();
        if (!ViewModel.IsPreviewReinitializing && _isPreviewReinitAnimating)
        {
            if (!ViewModel.IsPreviewing)
            {
                _isPreviewReinitAnimating = false;
                Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(HandleViewModelPropertyChangedAsync)}");
                RevealPreviewUnavailablePlaceholder();
            }
            else if (IsPreviewFirstVisualConfirmed)
            {
                Logger.Log($"PREVIEW_REINIT_ANIMATE_RESET attempt={PreviewStartupAttemptLabel} reason=reinit-stop-failed");
                _isPreviewReinitAnimating = false;
                Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(HandleViewModelPropertyChangedAsync)}");
                StopPreviewStartupOverlay();
                ResetPreviewContentTransform();
            }
        }
        else if (!ViewModel.IsPreviewReinitializing && !ViewModel.IsPreviewing)
        {
            ShowStartPreviewButtonPresentation();
        }
    }
}
