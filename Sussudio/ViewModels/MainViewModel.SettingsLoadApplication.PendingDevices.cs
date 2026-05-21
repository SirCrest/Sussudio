namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void StageDeferredDeviceSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        // Defer device selection until RefreshDevicesAsync populates the device list.
        _pendingSavedDeviceId = loadPlan.PendingDeviceId;
        _pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;
        _pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;
        _pendingSavedDeviceAudioMode = loadPlan.PendingDeviceAudioMode;
        _pendingSavedAnalogAudioGainPercent = loadPlan.PendingAnalogAudioGainPercent;
    }
}
