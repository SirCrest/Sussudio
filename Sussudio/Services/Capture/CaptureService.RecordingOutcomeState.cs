using System;
using System.Collections.Generic;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    // Recording finalization state is intentionally retained after stop so the
    // UI, automation, and verifier can explain what happened to the last file
    // even after capture resources have been torn down.
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();

    private void PublishRecordingStartedOutcome(string finalOutputPath)
    {
        _lastOutputPath = finalOutputPath;
        _lastFinalizeStatus = "Recording";
        _lastFinalizeUtc = null;
        _lastPreservedArtifacts = Array.Empty<string>();
    }

    private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)
    {
        if (updateOutputPath)
        {
            _lastOutputPath = result.OutputPath;
        }

        _lastFinalizeStatus = result.StatusMessage;
        _lastFinalizeUtc = DateTimeOffset.UtcNow;
        _lastPreservedArtifacts = result.PreservedArtifacts;
    }
}
