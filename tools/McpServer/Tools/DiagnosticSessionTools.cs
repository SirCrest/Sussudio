using System.ComponentModel;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class DiagnosticSessionTools
{
    [McpServerTool, Description("Run a timed capture diagnostic session, write snapshot/frame-ledger/timeline artifacts, and optionally verify recording or capture PresentMon.")]
    public static async Task<CallToolResult> run_diagnostic_session(
        PipeClient pipeClient,
        [Description("Session scenario: observe, preview-only, recording-only, flashback, flashback-playback, flashback-stress, flashback-scrub-stress, flashback-restart-cycle, flashback-encoder-cycle, flashback-export-playback, flashback-segment-playback, flashback-range-export, flashback-range-export-audio-switch, flashback-lifecycle, flashback-export-concurrent, flashback-disable-during-export, flashback-rotated-export, flashback-preview-cycle, flashback-playback-preview-cycle, flashback-recording, flashback-recording-preview-cycle, flashback-recording-settings-deferred, flashback-recording-export-rejected, flashback-export-rejected, or combined.")] string scenario = "observe",
        [Description("Session duration in seconds. Use 0 for a single snapshot sample.")] int seconds = 10,
        [Description("Snapshot sample interval in milliseconds.")] int sampleIntervalMs = 1000,
        [Description("Optional artifact output directory. Defaults to temp/diagnostic-sessions/<timestamp>.")] string? outputDirectory = null,
        [Description("Capture PresentMon during the session.")] bool presentMon = false,
        [Description("Optional PresentMon executable path.")] string? presentMonPath = null,
        [Description("Verify the last recording after the session. Recording scenarios verify automatically.")] bool verifyRecording = false,
        [Description("Leave preview/recording/flashback running after the session instead of restoring what this tool started.")] bool leaveRunning = false)
    {
        var result = await DiagnosticSessionRunner.RunAsync(
                new DiagnosticSessionOptions
                {
                    Scenario = scenario,
                    DurationSeconds = seconds,
                    SampleIntervalMs = sampleIntervalMs,
                    OutputDirectory = outputDirectory,
                    IncludePresentMon = presentMon,
                    PresentMonPath = presentMonPath,
                    VerifyRecording = verifyRecording,
                    LeaveRunning = leaveRunning
                },
                (command, payload, responseTimeoutMs) => pipeClient.SendCommandAsync(command, payload, responseTimeoutMs))
            .ConfigureAwait(false);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = DiagnosticSessionRunner.Format(result) }],
            IsError = !result.Success
        };
    }
}
