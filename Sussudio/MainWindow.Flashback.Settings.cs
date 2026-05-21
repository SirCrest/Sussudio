using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;

    private void InitializeFlashbackSettingsBindingController()
    {
        _flashbackSettingsBindingController = new FlashbackSettingsBindingController(new FlashbackSettingsBindingControllerContext
        {
            ViewModel = ViewModel,
            FlashbackEnabledToggle = FlashbackEnabledToggle,
            FlashbackGpuDecodeToggle = FlashbackGpuDecodeToggle,
            FlashbackBufferDurationCombo = FlashbackBufferDurationCombo,
            ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout
        });
    }

    private void ApplyInitialFlashbackSettings()
        => _flashbackSettingsBindingController.ApplyInitialSettings();

    private void AttachFlashbackSettingsBindings()
        => _flashbackSettingsBindingController.AttachBindings();

    private void SyncFlashbackGpuDecodeSetting()
        => _flashbackSettingsBindingController.SyncGpuDecodeToggle();

    private void SyncFlashbackBufferDurationSetting()
        => _flashbackSettingsBindingController.SyncBufferDurationSelection();

    private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || _flashbackSettingsBindingController == null)
        {
            return;
        }

        _flashbackSettingsBindingController.HandleBufferDurationSelectionChanged();
    }
}
