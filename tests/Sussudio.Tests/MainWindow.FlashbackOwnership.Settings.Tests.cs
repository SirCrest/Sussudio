using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackSettingsBindings_LiveInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs").Replace("\r\n", "\n");
        var commandAdapterText = ReadMainWindowFlashbackAdapterSource();
        var commandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackCommandController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackSettingsBindingController()");
        AssertContains(adapterText, "FlashbackEnabledToggle = FlashbackEnabledToggle,");
        AssertContains(adapterText, "FlashbackGpuDecodeToggle = FlashbackGpuDecodeToggle,");
        AssertContains(adapterText, "FlashbackBufferDurationCombo = FlashbackBufferDurationCombo,");
        AssertContains(adapterText, "ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout");
        AssertContains(adapterText, "private void ApplyInitialFlashbackSettings()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.ApplyInitialSettings();");
        AssertContains(adapterText, "private void AttachFlashbackSettingsBindings()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.AttachBindings();");
        AssertContains(adapterText, "private void SyncFlashbackGpuDecodeSetting()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.SyncGpuDecodeToggle();");
        AssertContains(adapterText, "private void SyncFlashbackBufferDurationSetting()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.SyncBufferDurationSelection();");
        AssertContains(adapterText, "private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(adapterText, "if (ViewModel == null || _flashbackSettingsBindingController == null)");
        AssertContains(adapterText, "_flashbackSettingsBindingController.HandleBufferDurationSelectionChanged();");
        AssertContains(mainWindowText, "InitializeFlashbackSettingsBindingController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Settings.cs")),
            "Flashback settings adapter lives in the focused Flashback settings partial");
        AssertContains(bindingsText, "ApplyInitialFlashbackSettings();");
        AssertContains(bindingsText, "AttachFlashbackSettingsBindings();");

        AssertContains(controllerText, "internal sealed class FlashbackSettingsBindingControllerContext");
        AssertContains(controllerText, "internal sealed class FlashbackSettingsBindingController");
        AssertContains(controllerText, "public void ApplyInitialSettings()");
        AssertContains(controllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;");
        AssertContains(controllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(controllerText, "SyncBufferDurationSelection();");
        AssertContains(controllerText, "public void AttachBindings()");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.Toggled +=");
        AssertContains(controllerText, "_context.ViewModel.FlashbackGpuDecode = _context.FlashbackGpuDecodeToggle.IsOn;");
        AssertContains(controllerText, "public void SyncGpuDecodeToggle()");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;");
        AssertContains(controllerText, "public void SyncBufferDurationSelection()");
        AssertContains(controllerText, "currentTag == selectedMinutes");
        AssertContains(controllerText, "_context.FlashbackBufferDurationCombo.SelectedItem = item;");
        AssertContains(controllerText, "public void HandleBufferDurationSelectionChanged()");
        AssertContains(controllerText, "int.TryParse(tag, out var minutes)");
        AssertContains(controllerText, "_context.ViewModel.FlashbackBufferMinutes = minutes;");
        AssertContains(controllerText, "FLASHBACK_UI_BUFFER_DURATION_CHANGED");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "SyncGpuDecodeSetting = SyncFlashbackGpuDecodeSetting,");
        AssertContains(flashbackPropertyChangedText, "SyncBufferDurationSetting = SyncFlashbackBufferDurationSetting");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackGpuDecode):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.SyncGpuDecodeSetting();");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferMinutes):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.SyncBufferDurationSetting();");

        AssertContains(commandAdapterText, "private FlashbackCommandController _flashbackCommandController = null!;");
        AssertContains(commandAdapterText, "private void InitializeFlashbackCommandController()");
        AssertContains(commandAdapterText, "private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)");
        AssertContains(commandAdapterText, "=> _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));");
        AssertContains(commandAdapterText, "private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(commandAdapterText, "=> _flashbackCommandController.ApplySettings(nameof(FlashbackApplyButton_Click));");
        AssertContains(commandControllerText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(commandControllerText, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.RestartFlashbackAsync(), operationName);");
        AssertContains(commandControllerText, "public bool HandleFullScreenKeyboardCommand(VirtualKey key)");
        AssertContains(commandControllerText, "NudgePlayback(TimeSpan.FromSeconds(-1), \"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\");");
        AssertContains(commandControllerText, "NudgePlayback(TimeSpan.FromSeconds(1), \"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\");");
        AssertContains(mainWindowText, "InitializeFlashbackCommandController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Commands.cs")),
            "Flashback command adapter lives in the focused Flashback command partial");
        AssertDoesNotContain(flashbackText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertDoesNotContain(bindingsText, "FlashbackEnabledToggle.IsOn = ViewModel.IsFlashbackEnabled;");
        AssertDoesNotContain(bindingsText, "FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;");
        AssertDoesNotContain(bindingsText, "FlashbackGpuDecodeToggle.Toggled +=");
        AssertDoesNotContain(bindingsText, "foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)");
        AssertDoesNotContain(flashbackText, "foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackBufferDurationCombo.SelectedItem = item;");

        return Task.CompletedTask;
    }
}
