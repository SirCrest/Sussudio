using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingSettingsSelectionPolicy_LivesInFocusedHelper()
    {
        var formatSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FormatSelection.cs").Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs").Replace("\r\n", "\n");
        var automationRecordingFormatText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs").Replace("\r\n", "\n");
        var recordingSettingsPolicyText = ReadRepoFile("Sussudio/ViewModels/RecordingSettingsSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(recordingCapabilityControllerText, "private void RebuildRecordingFormatOptions()");
        AssertContains(recordingCapabilityControllerText, "=> _recordingCapabilityController.RebuildRecordingFormatOptions();");
        AssertContains(recordingCapabilityControllerText, "public void RebuildRecordingFormatOptions()");
        AssertContains(recordingCapabilityControllerText, "RecordingSettingsSelectionPolicy.Select(");
        AssertContains(recordingCapabilityControllerText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(_viewModel.SelectedRecordingFormat)");
        AssertContains(recordingCapabilityControllerText, "_viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat));");
        AssertContains(recordingCapabilityControllerText, "Logger.Log($\"Selected recording format: {_viewModel.SelectedRecordingFormat}\");");
        AssertDoesNotContain(formatSelectionText, "private void RebuildRecordingFormatOptions()");
        AssertDoesNotContain(formatSelectionText, "RecordingSettingsSelectionPolicy.Select(");
        AssertContains(automationRecordingFormatText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(matched)");
        AssertContains(automationRecordingFormatText, "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertContains(automationRecordingFormatText, "RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality)");
        AssertContains(automationRecordingFormatText, "RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps)");
        AssertContains(automationRecordingFormatText, "public async Task SetRecordingFormatAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationRecordingFormat.cs")),
            "stale recording format automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingFormatOptions.cs")),
            "stale recording format options partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingCapabilityRefresh.cs")),
            "stale recording capability refresh partial");
        AssertDoesNotContain(formatSelectionText, "private static bool IsHdrCompatibleRecordingFormat(");
        AssertContains(recordingSettingsPolicyText, "internal static class RecordingSettingsSelectionPolicy");
        AssertContains(recordingSettingsPolicyText, "internal static bool IsHdrCompatible(");
        AssertContains(recordingSettingsPolicyText, "internal static RecordingFormat ParseRecordingFormat(");
        AssertContains(recordingSettingsPolicyText, "internal static VideoQuality ParseVideoQuality(");
        AssertContains(recordingSettingsPolicyText, "internal static double ClampCustomBitrateMbps(");
        AssertContains(recordingSettingsPolicyText, "internal static RecordingFormatSelection Select(");
        AssertContains(recordingSettingsPolicyText, "internal sealed record RecordingFormatSelection(");
        AssertContains(recordingSettingsPolicyText, "Keep the last known real formats visible if capability refresh temporarily produced none.");

        return Task.CompletedTask;
    }
}
