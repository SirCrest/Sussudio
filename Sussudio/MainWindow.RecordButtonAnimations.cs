using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing record-button chrome adapter. RecordingButtonChromeController owns
// the demo-visible glow, pulse, spinner, content, padding, and width morph.
public sealed partial class MainWindow
{
    private RecordingButtonChromeController _recordingButtonChromeController = null!;

    private void InitializeRecordingButtonChromeController()
    {
        _recordingButtonChromeController = new RecordingButtonChromeController(new RecordingButtonChromeControllerContext
        {
            RecordingGlowBorder = RecordingGlowBorder,
            RecordingGlowPulseStoryboard = RecordingGlowPulseStoryboard,
            RecPulseStoryboard = RecPulseStoryboard,
            RecordButton = RecordButton,
            RecordButtonNormalContent = RecordButtonNormalContent,
            RecordButtonStartingContent = RecordButtonStartingContent,
            RecordButtonRecordingContent = RecordButtonRecordingContent,
        });
    }
}
