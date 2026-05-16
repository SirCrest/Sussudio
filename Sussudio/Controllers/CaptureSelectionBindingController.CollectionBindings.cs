namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public void AttachCollectionBindings()
    {
        _context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;
        _context.AudioInputComboBox.ItemsSource = _context.ViewModel.AudioInputDevices;
        _context.MicrophoneComboBox.ItemsSource = _context.ViewModel.MicrophoneDevices;
        _context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;
        _context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;
        _context.FormatComboBox.ItemsSource = _context.ViewModel.AvailableRecordingFormats;
        _context.QualityComboBox.ItemsSource = _context.ViewModel.AvailableQualities;
        _context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;
        _context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;

        AttachCollectionSync(_context.ViewModel.Devices, QueueDeviceSelectionSync);
        AttachCollectionSync(_context.ViewModel.AudioInputDevices, QueueAudioSelectionSync);
        AttachCollectionSync(_context.ViewModel.MicrophoneDevices, QueueMicrophoneSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableResolutions, QueueResolutionSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableRecordingFormats, QueueFormatSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableQualities, QueueQualitySelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailablePresets, QueuePresetSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableSplitEncodeModes, QueueSplitEncodeModeSelectionSync);
    }
}
