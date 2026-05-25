using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var captureCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var captureCommands = BuildCaptureCommandProjection(viewModelSnapshot);");
        AssertContains(snapshotFlatteningText, "var captureCommandFlattening = BuildCaptureCommandFlattenedProjection(captureCommands);");
        AssertContains(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommandFlattening.CommandsEnqueued,");
        AssertContains(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommandFlattening.MaxQueueLatencyMs,");
        AssertContains(snapshotFlatteningText, "CaptureCommandLastError = captureCommandFlattening.LastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandCommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandMaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = viewModelSnapshot.CaptureCommandLastError,");
        AssertDoesNotContain(snapshotFlatteningText, "CaptureCommandLastError = captureCommands.LastError,");

        AssertContains(captureCommandProjectionText, "private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = viewModelSnapshot.CaptureCommandLastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandProjection");
        AssertContains(captureCommandProjectionText, "private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = captureCommands.LastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsUserSettingsProjection_LivesWithSnapshotProjection()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var userSettingsProjectionText = snapshotProjectionText;

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(snapshotFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(snapshotFlatteningText, "SelectedDeviceId = settingsFlattening.SelectedDeviceId,");
        AssertContains(snapshotFlatteningText, "SelectedFriendlyFrameRate = settingsFlattening.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningText, "SelectedRecordingFormat = settingsFlattening.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningText, "CustomBitrateMbps = settingsFlattening.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningText, "IsStatsVisible = settingsFlattening.IsStatsVisible,");
        AssertContains(userSettingsProjectionText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = userSettings.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct SettingsFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = userSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible,");
        AssertDoesNotContain(snapshotFlatteningText, "IsStatsVisible = userSettings.IsStatsVisible,");

        AssertContains(userSettingsProjectionText, "private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(userSettingsProjectionText, "SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,");
        AssertContains(userSettingsProjectionText, "SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "IsStatsVisible = viewModelSnapshot.IsStatsVisible");
        AssertContains(userSettingsProjectionText, "private readonly record struct UserSettingsProjection");
        AssertContains(userSettingsProjectionText, "private static RecordingSettingsProjection BuildRecordingSettingsProjection(UserSettingsProjection userSettings)");
        AssertContains(userSettingsProjectionText, "SelectedRecordingFormat = userSettings.SelectedRecordingFormat,");
        AssertContains(userSettingsProjectionText, "SelectedVideoFormat = userSettings.SelectedVideoFormat,");
        AssertContains(userSettingsProjectionText, "CustomBitrateMbps = userSettings.CustomBitrateMbps");
        AssertContains(userSettingsProjectionText, "private readonly record struct RecordingSettingsProjection");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")),
            "user settings projection lives with AutomationDiagnosticsHub.SnapshotProjection.cs");

        return Task.CompletedTask;
    }
}
