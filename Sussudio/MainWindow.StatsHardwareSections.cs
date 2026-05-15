namespace Sussudio;

// Hardware-oriented stats rows. The dynamic row element pools live in
// StatsDiagnosticRowsController; this partial only keeps the XAML-facing hooks.
public sealed partial class MainWindow
{
    private void UpdateDecodeSection()
        => _statsHardwareRowsController.UpdateDecodeSection();

    private void UpdateGpuSection()
        => _statsHardwareRowsController.UpdateGpuSection();
}
