using System;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing record-button animation adapter. RecordButtonAnimationController
// owns the circle/pill width morph used when recording state changes.
public sealed partial class MainWindow
{
    private RecordButtonAnimationController _recordButtonAnimationController = null!;

    private void InitializeRecordButtonAnimationController()
    {
        _recordButtonAnimationController = new RecordButtonAnimationController(new RecordButtonAnimationControllerContext
        {
            RecordButton = RecordButton,
        });
    }

    private void AnimateRecordButtonWidth(double from, double to, Action? onCompleted = null)
        => _recordButtonAnimationController.AnimateWidth(from, to, onCompleted);
}
