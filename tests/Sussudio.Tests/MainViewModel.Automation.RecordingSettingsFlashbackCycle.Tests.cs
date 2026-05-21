using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelAutomation_RecordingSettingsRouteThroughControllerAndFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackSettings.cs"];
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackEncoderSettings.cs"];
        var automationSettingsText = viewModelFiles["MainViewModel.AutomationSettings.cs"];
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs")
            .Replace("\r\n", "\n");
        var recordingSettingsAutomationControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.Context.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackEncoderSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackEncoderSettings.cs")
            .Replace("\r\n", "\n");

        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "_suppressFlashbackFormatCycle is false");
        AssertContains(rawFlashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),\n                \"recording format\");");
        AssertContains(viewModelFlashbackStateText, "private bool _suppressFlashbackFormatCycle;");
        AssertMemberContains(automationSettingsText, "SetRecordingFormatAsync", "_recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken)");
        AssertContains(recordingSettingsAutomationControllerContextText, "namespace Sussudio.Controllers;");
        AssertContains(recordingSettingsAutomationControllerContextText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "SetSuppressFlashbackFormatCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "await _context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "_context.SetSelectedQuality(matched);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "settings.Quality,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "return BuildEncoderSettings(splitEncodeMode: _context.GetSelectedSplitEncodeMode());");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "settings.SplitEncodeMode,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "_context.SetCustomBitrateMbps(RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps));");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "settings.Bitrate,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "_context.SetSelectedPreset(matched);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "settings.Preset,");
        AssertMemberContains(flashbackEncoderSettingsText, "OnCustomBitrateMbpsChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedQualityChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedPresetChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedSplitEncodeModeChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "customBitrateMbps: CustomBitrateMbps");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "nvencPreset: SelectedPreset");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "splitEncodeMode: SelectedSplitEncodeMode");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "TrackPendingFlashbackCycleTask(task, description);");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = task;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (ReferenceEquals(_pendingFlashbackCycleTask, t))");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = null;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (t.IsFaulted)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "else if (t.IsCanceled)");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) failed");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) canceled");

        return Task.CompletedTask;
    }
}
