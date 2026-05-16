using System;
using System.Threading.Tasks;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewLifecycleEventControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Func<bool> ShouldBeginPreviewStartupAttempt { get; init; }
    public required Action BeginPreviewStartupAttempt { get; init; }
    public required Action PrimePreviewAudioFadeIn { get; init; }
    public required Func<bool> IsPreviewReinitAnimating { get; init; }
    public required Action PreparePreviewStartupPresentation { get; init; }
    public required Action StopPreviewStartupWatchdog { get; init; }
    public required Action StartPreviewStartupWatchdog { get; init; }
    public required Action StopPreviewStartupOverlay { get; init; }
    public required Action<PreviewStartupState, string?> SetPreviewStartupState { get; init; }
    public required Func<string> GetPreviewStartupAttemptLabel { get; init; }
    public required Func<Task> StartPreviewRendererAsync { get; init; }
    public required Func<bool> IsPreviewFirstVisualConfirmed { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
    public required Action<string> SchedulePreviewStartupFailureStop { get; init; }
    public required Action ShowStopPreviewButtonPresentation { get; init; }
    public required Action ShowStartPreviewButtonPresentation { get; init; }
    public required Action ApplyHdrToggleEnabledState { get; init; }
    public required Func<Task> StopPreviewRendererAsync { get; init; }
    public required Action<bool> ResetPreviewStartupTracking { get; init; }
    public required Action HandlePreviewReinitializingChanged { get; init; }
}

internal sealed class PreviewLifecycleEventController
{
    private readonly PreviewLifecycleEventControllerContext _context;
    private bool _stopRequestedByUser;

    public PreviewLifecycleEventController(PreviewLifecycleEventControllerContext context)
    {
        _context = context;
    }

    public bool StopRequestedByUser => _stopRequestedByUser;

    public void SetStopRequestedByUser(bool value)
        => _stopRequestedByUser = value;

    public async Task<bool> TryHandlePropertyChangedAsync(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                await HandlePreviewingChangedAsync();
                return true;

            case nameof(MainViewModel.IsPreviewReinitializing):
                _context.HandlePreviewReinitializingChanged();
                return true;

            default:
                return false;
        }
    }

    public void HandlePreviewStartRequested()
    {
        _stopRequestedByUser = false;
        if (_context.ShouldBeginPreviewStartupAttempt())
        {
            _context.BeginPreviewStartupAttempt();
        }

        _context.PrimePreviewAudioFadeIn();
        if (!_context.ViewModel.IsPreviewReinitializing && !_context.IsPreviewReinitAnimating())
        {
            _context.PreparePreviewStartupPresentation();
        }
    }

    public void HandlePreviewStopRequested()
    {
        _stopRequestedByUser = _stopRequestedByUser || !_context.ViewModel.IsPreviewReinitializing;
        _context.StopPreviewStartupWatchdog();
        _context.StopPreviewStartupOverlay();
    }

    private async Task HandlePreviewingChangedAsync()
    {
        if (_context.ViewModel.IsPreviewing)
        {
            await HandlePreviewStartedAsync();
            return;
        }

        await HandlePreviewStoppedAsync();
    }

    private async Task HandlePreviewStartedAsync()
    {
        _stopRequestedByUser = false;
        if (_context.ShouldBeginPreviewStartupAttempt())
        {
            _context.BeginPreviewStartupAttempt();
        }

        _context.SetPreviewStartupState(PreviewStartupState.StartingSession, null);
        Logger.Log($"PREVIEW_SESSION_STARTED attempt={_context.GetPreviewStartupAttemptLabel()}");
        if (!_context.ViewModel.IsPreviewReinitializing && !_context.IsPreviewReinitAnimating())
        {
            _context.PreparePreviewStartupPresentation();
        }

        _context.SetPreviewStartupState(PreviewStartupState.RendererAttaching, null);
        try
        {
            await _context.StartPreviewRendererAsync();
        }
        catch (Exception ex)
        {
            var attachFailureReason = $"renderer-attach-failed:{ex.Message}";
            _context.SetPreviewStartupState(PreviewStartupState.Failed, attachFailureReason);
            _context.StopPreviewStartupWatchdog();
            _context.RevealPreviewUnavailablePlaceholder();
            Logger.Log(
                $"PREVIEW_RENDERER_ATTACH_FAILED attempt={_context.GetPreviewStartupAttemptLabel()} " +
                $"reason={attachFailureReason}");
            _context.SchedulePreviewStartupFailureStop(attachFailureReason);
            throw;
        }

        if (!_context.IsPreviewFirstVisualConfirmed())
        {
            _context.SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual, null);
            _context.StartPreviewStartupWatchdog();
        }

        _context.ShowStopPreviewButtonPresentation();
        _context.ApplyHdrToggleEnabledState();
    }

    private async Task HandlePreviewStoppedAsync()
    {
        _context.StopPreviewStartupWatchdog();
        _context.StopPreviewStartupOverlay();
        // During reinit, the renderer is kept alive (render thread stopped
        // by ViewModel_PreviewRendererStopRequested, instance preserved).
        // StartPreviewRendererAsync will reuse it via Start().
        if (!_context.ViewModel.IsPreviewReinitializing)
        {
            await _context.StopPreviewRendererAsync();
        }

        if (!_context.ViewModel.IsPreviewReinitializing && !_context.IsPreviewReinitAnimating())
        {
            _context.RevealPreviewUnavailablePlaceholder();
        }

        if (_context.ViewModel.IsPreviewReinitializing)
        {
            _context.ShowStopPreviewButtonPresentation();
        }
        else
        {
            _context.ShowStartPreviewButtonPresentation();
        }

        _context.ApplyHdrToggleEnabledState();
        _context.ResetPreviewStartupTracking(
            _context.ViewModel.IsPreviewReinitializing || _context.IsPreviewReinitAnimating());
    }
}
