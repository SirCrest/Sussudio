using System;

namespace Sussudio.Controllers;

internal enum PreviewReinitCompletionPresentation
{
    None,
    RevealUnavailablePlaceholder,
    ResetConfirmedVisual,
    ShowStartPreviewButton
}

internal sealed class PreviewReinitCompletionPresentationContext
{
    public required bool IsPreviewReinitializing { get; init; }

    public required bool IsPreviewing { get; init; }

    public required bool IsFirstVisualConfirmed { get; init; }

    public required string AttemptLabel { get; init; }

    public required string CallerName { get; init; }

    public required Action UpdateDeviceApplyButtonState { get; init; }

    public required Action RevealUnavailablePlaceholder { get; init; }

    public required Action StopPreviewStartupOverlay { get; init; }

    public required Action ResetPreviewContentTransform { get; init; }

    public required Action ShowStartPreviewButtonPresentation { get; init; }
}

internal sealed class PreviewReinitTransitionController
{
    public bool IsAnimating { get; private set; }

    public void BeginAnimateOut(string reason, string callerName)
    {
        IsAnimating = true;
        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=true caller={callerName}");
        Logger.Log($"PREVIEW_REINIT_ANIMATE_OUT reason={reason}");
    }

    public PreviewReinitCompletionPresentation GetCompletionPresentation(
        bool isPreviewReinitializing,
        bool isPreviewing,
        bool isFirstVisualConfirmed)
    {
        if (!isPreviewReinitializing && IsAnimating)
        {
            if (!isPreviewing)
            {
                return PreviewReinitCompletionPresentation.RevealUnavailablePlaceholder;
            }

            if (isFirstVisualConfirmed)
            {
                return PreviewReinitCompletionPresentation.ResetConfirmedVisual;
            }
        }
        else if (!isPreviewReinitializing && !isPreviewing)
        {
            return PreviewReinitCompletionPresentation.ShowStartPreviewButton;
        }

        return PreviewReinitCompletionPresentation.None;
    }

    public void HandleReinitializingChanged(PreviewReinitCompletionPresentationContext context)
    {
        context.UpdateDeviceApplyButtonState();
        switch (GetCompletionPresentation(
            context.IsPreviewReinitializing,
            context.IsPreviewing,
            context.IsFirstVisualConfirmed))
        {
            case PreviewReinitCompletionPresentation.RevealUnavailablePlaceholder:
                Clear(context.CallerName, logWhenInactive: false);
                context.RevealUnavailablePlaceholder();
                break;

            case PreviewReinitCompletionPresentation.ResetConfirmedVisual:
                ResetConfirmedVisualTransition(
                    context.AttemptLabel,
                    "reinit-stop-failed",
                    context.CallerName);
                context.StopPreviewStartupOverlay();
                context.ResetPreviewContentTransform();
                break;

            case PreviewReinitCompletionPresentation.ShowStartPreviewButton:
                context.ShowStartPreviewButtonPresentation();
                break;
        }
    }

    public void CompleteFirstVisualTransition(string attemptLabel, string callerName)
    {
        if (!IsAnimating)
        {
            return;
        }

        Logger.Log($"PREVIEW_REINIT_ANIMATE_IN attempt={attemptLabel}");
        Clear(callerName, logWhenInactive: false);
    }

    public void ResetConfirmedVisualTransition(string attemptLabel, string reason, string callerName)
    {
        Logger.Log($"PREVIEW_REINIT_ANIMATE_RESET attempt={attemptLabel} reason={reason}");
        Clear(callerName, logWhenInactive: false);
    }

    public void ClearForStartupReset(bool preserveReinitAnimation, string callerName)
    {
        if (preserveReinitAnimation)
        {
            return;
        }

        Clear(callerName);
    }

    public void Clear(string callerName, bool logWhenInactive = true, string? operationName = null)
    {
        if (!IsAnimating && !logWhenInactive)
        {
            return;
        }

        IsAnimating = false;
        var message = $"D3D11_RENDERER_REINIT_FLAG flag=false caller={callerName}";
        if (operationName is null)
        {
            Logger.Log(message);
        }
        else
        {
            Logger.Log(message, operationName);
        }
    }
}
