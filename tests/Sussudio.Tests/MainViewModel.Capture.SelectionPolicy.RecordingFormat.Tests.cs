using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingFormatSelectionPolicy_LivesInFocusedHelper()
    {
        var formatSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FormatSelection.cs").Replace("\r\n", "\n");
        var recordingFormatOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingFormatOptions.cs").Replace("\r\n", "\n");
        var automationRecordingSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs").Replace("\r\n", "\n");
        var recordingFormatPolicyText = ReadRepoFile("Sussudio/ViewModels/RecordingFormatSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(recordingFormatOptionsText, "private void RebuildRecordingFormatOptions()");
        AssertContains(recordingFormatOptionsText, "RecordingFormatSelectionPolicy.Select(");
        AssertContains(recordingFormatOptionsText, "RecordingFormatSelectionPolicy.IsHdrCompatible(SelectedRecordingFormat)");
        AssertContains(recordingFormatOptionsText, "OnPropertyChanged(nameof(SelectedRecordingFormat));");
        AssertContains(recordingFormatOptionsText, "Logger.Log($\"Selected recording format: {SelectedRecordingFormat}\");");
        AssertDoesNotContain(formatSelectionText, "private void RebuildRecordingFormatOptions()");
        AssertDoesNotContain(formatSelectionText, "RecordingFormatSelectionPolicy.Select(");
        AssertContains(automationRecordingSettingsText, "RecordingFormatSelectionPolicy.IsHdrCompatible(matched)");
        AssertDoesNotContain(formatSelectionText, "private static bool IsHdrCompatibleRecordingFormat(");
        AssertContains(recordingFormatPolicyText, "internal static class RecordingFormatSelectionPolicy");
        AssertContains(recordingFormatPolicyText, "internal static bool IsHdrCompatible(");
        AssertContains(recordingFormatPolicyText, "internal static RecordingFormatSelection Select(");
        AssertContains(recordingFormatPolicyText, "internal sealed record RecordingFormatSelection(");
        AssertContains(recordingFormatPolicyText, "Keep the last known real formats visible if capability refresh temporarily produced none.");

        return Task.CompletedTask;
    }
}
