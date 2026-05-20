using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationViewModelFlashbackUiContractsTests
{
    public AutomationViewModelFlashbackUiContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationPreviewVolumePersistsThroughSettingsPath()
        => global::Program.AutomationPreviewVolume_PersistsThroughSettingsPath();

    [Fact]
    public Task AutomationAudioCommandsPreserveRuntimeGuards()
        => global::Program.AutomationAudioCommands_PreserveRuntimeGuards();

    [Fact]
    public Task AutomationUiSettingsPersistThroughSettingsPath()
        => global::Program.AutomationUiSettings_PersistThroughSettingsPath();

    [Fact]
    public Task SettingsPersistenceProjectionLoadPlanPreservesSavedSemantics()
        => global::Program.SettingsPersistenceProjection_LoadPlanPreservesSavedSemantics();

    [Fact]
    public Task SettingsPersistenceProjectionSaveSettingsMapsPersistedValues()
        => global::Program.SettingsPersistenceProjection_SaveSettingsMapsPersistedValues();

    [Fact]
    public Task AutomationDeviceSelectionRoutesThroughApplyReinit()
        => global::Program.AutomationDeviceSelection_RoutesThroughApplyReinit();

    [Fact]
    public Task AutomationCaptureSettingsRouteThroughControllerAndAwaitReinitialization()
        => global::Program.AutomationCaptureModeChanges_AwaitReinitialization();

    [Fact]
    public Task AutomationRecordingTransitionsUseSharedLifecycleGate()
        => global::Program.MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate();

    [Fact]
    public Task BitrateSampleWindowPreservesBoundedAverageBehavior()
        => global::Program.BitrateSampleWindow_PreservesBoundedAverageBehavior();

    [Fact]
    public Task AutomationRecordingSettingsRouteThroughControllerAndFlashbackCycle()
        => global::Program.MainViewModelAutomation_RecordingSettingsRouteThroughControllerAndFlashbackCycle();

    [Fact]
    public Task AutomationFlashbackAndProbeCommandsUseAsyncViewModelSurface()
        => global::Program.MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface();

    [Fact]
    public Task AutomationViewModelRuntimeSnapshotLivesInFocusedPartial()
        => global::Program.MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial();

    [Fact]
    public Task MainWindowFlashbackScrubEndsOnReleaseCancelAndCaptureLost()
        => global::Program.MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost();

    [Fact]
    public Task FlashbackTimelineGeometryPreservesScrubMath()
        => global::Program.FlashbackTimelineGeometry_PreservesScrubMath();

    [Fact]
    public Task MainWindowFlashbackToggleRollsBackUiStateOnFailure()
        => global::Program.MainWindowFlashbackToggle_RollsBackUiStateOnFailure();

    [Fact]
    public Task FlashbackPollingTimersLiveInController()
        => global::Program.FlashbackPollingTimers_LiveInController();

    [Fact]
    public Task FlashbackTimelineTrackLayoutLivesInController()
        => global::Program.FlashbackTimelineTrackLayout_LivesInController();

    [Fact]
    public Task FlashbackPlayheadMotionLivesInController()
        => global::Program.FlashbackPlayheadMotion_LivesInController();

    [Fact]
    public Task FlashbackMarkerPresentationLivesInController()
        => global::Program.FlashbackMarkerPresentation_LivesInController();

    [Fact]
    public Task FlashbackPlaybackPresentationLivesInController()
        => global::Program.FlashbackPlaybackPresentation_LivesInController();

    [Fact]
    public Task FlashbackExportProgressPresentationLivesInController()
        => global::Program.FlashbackExportProgressPresentation_LivesInController();

    [Fact]
    public Task FlashbackSettingsBindingsLiveInController()
        => global::Program.FlashbackSettingsBindings_LiveInController();
}
