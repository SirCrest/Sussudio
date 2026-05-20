using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelAudioControlsContractsTests
{
    public PresentationPreviewMainViewModelAudioControlsContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AudioControlsMapAnalogGainCurveAndClampEndpoints()
        => global::Program.MainViewModelAudioControls_MapsAnalogGainCurveAndClamps();

    [Fact]
    public Task AudioMonitoringPreservesVolumePersistenceAndRampedRouting()
        => global::Program.MainViewModelAudioMonitoring_PreservesVolumePersistenceAndRampedRouting();

    [Fact]
    public Task AudioControlsPreserveMicrophoneAndDeviceGuards()
        => global::Program.MainViewModelAudioControls_PreserveMicrophoneVolumeAndDeviceGuards();

    [Fact]
    public Task DeviceAudioRequestControllerOwnsRequestLifetime()
        => global::Program.MainViewModelDeviceAudioRequestController_OwnsDeviceAudioRequestLifetime();

    [Fact]
    public Task AudioDeviceSelectionPolicyLivesInFocusedHelper()
        => global::Program.AudioDeviceSelectionPolicy_LivesInFocusedHelper();

    [Fact]
    public Task AudioDeviceSelectionPolicyStartupFiltersCaptureAudioAndUsesSavedFallbacks()
        => global::Program.AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks();

    [Fact]
    public Task AudioDeviceSelectionPolicyStartupPreservesPreviousSelections()
        => global::Program.AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections();

    [Fact]
    public Task AudioDeviceSelectionPolicyRefreshPreservesSelections()
        => global::Program.AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback();

    [Fact]
    public Task AudioDeviceSelectionPolicyHandlesEmptyLists()
        => global::Program.AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections();

    [Fact]
    public Task NativeXuAudioControlProfilesLiveInFocusedPartial()
        => global::Program.NativeXuAudioControlService_ProfilesLiveInFocusedPartial();

    [Fact]
    public Task NativeXuAudioControlTransportLivesInFocusedPartial()
        => global::Program.NativeXuAudioControlService_TransportLivesInFocusedPartial();

    [Fact]
    public Task AudioMetersOwnCallbackMeterState()
        => global::Program.MainViewModelAudioMeters_OwnCallbackMeterState();
}
