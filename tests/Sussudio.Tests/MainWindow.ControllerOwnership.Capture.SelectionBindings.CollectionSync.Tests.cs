using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureSelectionBindingCollectionSync_LivesInControllerPartial()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.CaptureSelectionBindings.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs").Replace("\r\n", "\n");
        var collectionSyncText = ReadRepoFile("Sussudio/Controllers/Capture/CaptureSelectionBindingController.CollectionSync.cs").Replace("\r\n", "\n");

        AssertContains(collectionSyncText, "internal sealed partial class CaptureSelectionBindingController");
        AssertContains(collectionSyncText, "public void AttachCollectionBindings()");
        AssertContains(collectionSyncText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertContains(collectionSyncText, "private static void AttachCollectionSync(INotifyCollectionChanged collection, Action queueSync)");
        AssertContains(collectionSyncText, "private void QueueSelectionSync(int syncIndex, Action ensureMethod)");
        AssertContains(collectionSyncText, "public void HandleAvailableResolutionsPropertyChanged()");
        AssertContains(collectionSyncText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;");
        AssertContains(collectionSyncText, "EnsureResolutionSelection();");
        AssertContains(collectionSyncText, "public void HandleAvailableFrameRatesPropertyChanged()");
        AssertContains(collectionSyncText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;");
        AssertContains(collectionSyncText, "EnsureFrameRateSelection();");
        AssertContains(collectionSyncText, "public void HandleAvailablePresetsPropertyChanged()");
        AssertContains(collectionSyncText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;");
        AssertContains(collectionSyncText, "EnsurePresetSelection();");
        AssertContains(collectionSyncText, "public void HandleAvailableSplitEncodeModesPropertyChanged()");
        AssertContains(collectionSyncText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;");
        AssertContains(collectionSyncText, "EnsureSplitEncodeModeSelection();");
        AssertOccursBefore(collectionSyncText, "_context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;", "EnsureResolutionSelection();");
        AssertOccursBefore(collectionSyncText, "_context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;", "EnsureFrameRateSelection();");
        AssertOccursBefore(collectionSyncText, "_context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;", "EnsurePresetSelection();");
        AssertOccursBefore(collectionSyncText, "_context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;", "EnsureSplitEncodeModeSelection();");
        AssertContains(collectionSyncText, "_context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;");
        AssertContains(collectionSyncText, "AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);");

        AssertDoesNotContain(controllerText, "private readonly int[] _selectionSyncQueued = new int[9];");
        AssertDoesNotContain(controllerText, "private static void AttachCollectionSync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Capture", "CaptureSelectionBindingController.SelectionState.cs")),
            "empty selection-state marker partial should stay removed");
        AssertDoesNotContain(adapterText, "private void AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(adapterText, "_captureSelectionBindingController.AttachRecordingStringSelectionBindings()");
        AssertDoesNotContain(bindingsText, "private void QueueSelectionSync(");
        AssertDoesNotContain(bindingsText, "private static void AttachCollectionSync(");
        AssertDoesNotContain(bindingsText, "private void EnsureDeviceSelection()");

        return Task.CompletedTask;
    }
}
