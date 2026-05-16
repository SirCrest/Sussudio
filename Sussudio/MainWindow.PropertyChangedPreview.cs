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
}
