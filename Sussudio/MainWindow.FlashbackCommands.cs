using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for Flashback command buttons and the enable toggle.
// FlashbackCommandController owns command semantics and rollback behavior.
public sealed partial class MainWindow
{
    private FlashbackCommandController _flashbackCommandController = null!;

    private void InitializeFlashbackCommandController()
    {
        _flashbackCommandController = new FlashbackCommandController(new FlashbackCommandControllerContext
        {
            ViewModel = ViewModel,
            FlashbackEnabledToggle = FlashbackEnabledToggle,
            RunUiEventHandlerAsync = RunUiEventHandlerAsync
        });
    }

    private void FlashbackInButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SetInPointAtPlayhead();

    private void FlashbackOutButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SetOutPointAtPlayhead();

    private void FlashbackClearButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ClearInOutPoints();

    private void FlashbackPlayPauseButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.TogglePlayPause();

    private void FlashbackGoLiveButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.GoLive();

    private void FlashbackExportButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.Export(nameof(FlashbackExportButton_Click));

    private void FlashbackSaveLast5mButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.SaveLast5m(nameof(FlashbackSaveLast5mButton_Click));

    private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));

    private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)
        => _flashbackCommandController.ApplySettings(nameof(FlashbackApplyButton_Click));
}
