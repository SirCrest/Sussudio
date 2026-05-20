using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var captureCommandFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCommands.cs")
            .Replace("\r\n", "\n");
        var captureCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs")
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

        AssertContains(captureCommandFlatteningText, "private static CaptureCommandFlattenedProjection BuildCaptureCommandFlattenedProjection(");
        AssertContains(captureCommandFlatteningText, "CommandsEnqueued = captureCommands.CommandsEnqueued,");
        AssertContains(captureCommandFlatteningText, "MaxQueueLatencyMs = captureCommands.MaxQueueLatencyMs,");
        AssertContains(captureCommandFlatteningText, "LastError = captureCommands.LastError");
        AssertContains(captureCommandFlatteningText, "private readonly record struct CaptureCommandFlattenedProjection");

        AssertContains(captureCommandProjectionText, "private static CaptureCommandProjection BuildCaptureCommandProjection(ViewModelRuntimeSnapshot viewModelSnapshot)");
        AssertContains(captureCommandProjectionText, "CommandsEnqueued = viewModelSnapshot.CaptureCommandCommandsEnqueued,");
        AssertContains(captureCommandProjectionText, "MaxQueueLatencyMs = viewModelSnapshot.CaptureCommandMaxQueueLatencyMs,");
        AssertContains(captureCommandProjectionText, "LastError = viewModelSnapshot.CaptureCommandLastError");
        AssertContains(captureCommandProjectionText, "private readonly record struct CaptureCommandProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsUserSettingsProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningSettingsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Settings.cs")
            .Replace("\r\n", "\n");
        var userSettingsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var userSettings = BuildUserSettingsProjection(viewModelSnapshot);");
        AssertContains(snapshotProjectionText, "var recordingSettings = BuildRecordingSettingsProjection(userSettings);");
        AssertContains(snapshotFlatteningText, "var settingsFlattening = BuildSettingsFlattenedProjection(userSettings, recordingSettings);");
        AssertContains(snapshotFlatteningText, "SelectedDeviceId = settingsFlattening.SelectedDeviceId,");
        AssertContains(snapshotFlatteningText, "SelectedFriendlyFrameRate = settingsFlattening.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningText, "SelectedRecordingFormat = settingsFlattening.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningText, "CustomBitrateMbps = settingsFlattening.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningText, "IsStatsVisible = settingsFlattening.IsStatsVisible,");
        AssertContains(snapshotFlatteningSettingsText, "private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(");
        AssertContains(snapshotFlatteningSettingsText, "SelectedDeviceId = userSettings.SelectedDeviceId,");
        AssertContains(snapshotFlatteningSettingsText, "SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,");
        AssertContains(snapshotFlatteningSettingsText, "SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,");
        AssertContains(snapshotFlatteningSettingsText, "CustomBitrateMbps = recordingSettings.CustomBitrateMbps,");
        AssertContains(snapshotFlatteningSettingsText, "IsStatsVisible = userSettings.IsStatsVisible");
        AssertContains(snapshotFlatteningSettingsText, "private readonly record struct SettingsFlattenedProjection");
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

        return Task.CompletedTask;
    }
}
