using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: HalfRatePreviewAndPlaybackSuspected");
                    AssertContains(output, "SampleQuality: Ready");
                    AssertContains(output, "SourceToPreviewRatio: 0.5");
                    AssertContains(output, "SourceToPlaybackRatio: 0.5");
                    AssertContains(output, "HalfRatePreviewSuspected: true");
                    AssertContains(output, "HalfRatePlaybackSuspected: true");
                    AssertContains(output, "VisualChangeFps: 60");
                    AssertContains(output, "MjpegUniqueFps: 60");
                    AssertContains(output, "PreviewDropDelta: 4");
                    AssertContains(output, "PlaybackDropDelta: 2");
                    AssertContains(output, "PreviewPacingLikelySlowStage: VisualDuplicateOrLowMotion");
                    AssertContains(output, "PreviewPacingSlowStageConfidence: Medium");
                    AssertContains(output, "PreviewPacingSlowStageEvidence: synthetic duplicate cadence");
                    var expected = """
                                   Verdict: HalfRatePreviewAndPlaybackSuspected
                                   SampleQuality: Ready
                                   TargetFps: 120
                                   TargetFrameMs: 8.333
                                   MinSampleSeconds: 30
                                   Capture: observed=120 5pct=120 1pct=119 samples=3600 durationMs=30000 ready=true
                                   Preview: observed=60 5pct=60 1pct=58 samples=1800 durationMs=30000 ready=true
                                   Playback: observed=60 5pct=60 1pct=58 samples=1800 durationMs=30000 ready=true
                                   SourceToPreviewRatio: 0.5
                                   SourceToPlaybackRatio: 0.5
                                   HalfRatePreviewSuspected: true
                                   HalfRatePlaybackSuspected: true
                                   HiddenStutterSuspected: false
                                   VisualChangeFps: 60
                                   VisualRepeatPercent: 50
                                   VisualMotionConfidence: High
                                   MjpegInputFps: 120
                                   MjpegUniqueFps: 60
                                   MjpegDuplicatePercent: 50
                                   PreviewPacingLikelySlowStage: VisualDuplicateOrLowMotion
                                   PreviewPacingSlowStageConfidence: Medium
                                   PreviewPacingSlowStageEvidence: synthetic duplicate cadence
                                   TimelineSamples: 2
                                   DxgiMissedRefreshRecentMax: 4
                                   PreviewDropDelta: 4
                                   PlaybackDropDelta: 2
                                   Evidence: captureReady=true previewReady=true playbackReady=true previewHalfRate=true playbackHalfRate=true
                                   """;
                    AssertEqual(expected, NormalizeLineEndings(output), "frame pacing verdict text");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 3600,
                          "CaptureCadenceSampleDurationMs": 30000,
                          "PreviewCadenceObservedFps": 60,
                          "PreviewCadenceFivePercentLowFps": 60,
                          "PreviewCadenceOnePercentLowFps": 58,
                          "PreviewCadenceSampleCount": 1800,
                          "PreviewCadenceSampleDurationMs": 30000,
                          "PreviewCadenceRecentIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "FlashbackPlaybackTargetFps": 120,
                          "FlashbackPlaybackObservedFps": 60,
                          "FlashbackPlaybackFivePercentLowFps": 60,
                          "FlashbackPlaybackOnePercentLowFps": 58,
                          "FlashbackPlaybackCadenceSampleCount": 1800,
                          "FlashbackPlaybackSampleDurationMs": 30000,
                          "FlashbackPlaybackRecentFrameIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "VisualCadenceChangeObservedFps": 60,
                          "VisualCadenceRepeatFramePercent": 50,
                          "VisualCadenceMotionConfidence": "High",
                          "MjpegPacketHashInputObservedFps": 120,
                          "MjpegPacketHashUniqueObservedFps": 60,
                          "MjpegPacketHashDuplicateFramePercent": 50,
                          "PreviewPacingLikelySlowStage": "VisualDuplicateOrLowMotion",
                          "PreviewPacingSlowStageConfidence": "Medium",
                          "PreviewPacingSlowStageEvidence": "synthetic duplicate cadence"
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": [
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                            "MjpegPreviewJitterTotalDropped": 1,
                            "FlashbackPlaybackDroppedFrames": 0
                          },
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 4,
                            "MjpegPreviewJitterTotalDropped": 5,
                            "FlashbackPlaybackDroppedFrames": 2
                          }
                        ]
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }

    internal static async Task McpFramePacingVerdictTool_FlagsInsufficientSampleDuration()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing-short");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: InsufficientSample");
                    AssertContains(output, "SampleQuality: Insufficient");
                    AssertContains(output, "ready=false");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 240,
                          "CaptureCadenceSampleDurationMs": 2000,
                          "PreviewCadenceObservedFps": 120,
                          "PreviewCadenceFivePercentLowFps": 120,
                          "PreviewCadenceOnePercentLowFps": 119,
                          "PreviewCadenceSampleCount": 240,
                          "PreviewCadenceSampleDurationMs": 2000
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }
}
