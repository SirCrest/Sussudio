using System;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewStartupState CurrentPreviewStartupState
        => _previewStartupSessionController.State;

    private string PreviewStartupAttemptLabel
        => _previewStartupSessionController.AttemptLabel;

    private string? PreviewStartupAttemptId
        => _previewStartupSessionController.AttemptId;

    private DateTimeOffset? PreviewStartupRequestedUtc
        => _previewStartupSessionController.RequestedUtc;

    private string? PreviewStartupMissingSignals
    {
        get => _previewStartupSessionController.MissingSignals;
        set => _previewStartupSessionController.SetMissingSignals(value);
    }

    private int PreviewStartupRecoveryAttemptCount
        => _previewStartupSessionController.RecoveryAttemptCount;

    private string? PreviewStartupLastFailureReason
        => _previewStartupSessionController.LastFailureReason;

    private bool IsPreviewFirstVisualConfirmed
        => _previewStartupSessionController.FirstVisualConfirmed;

    private bool ShouldBeginPreviewStartupAttempt
        => _previewStartupSessionController.ShouldBeginAttempt;
}
