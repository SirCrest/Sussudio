using System;
using System.Diagnostics;
using System.IO;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private LastOutputProbe ProbeLastOutput(string? lastOutputPath, bool isRecording)
    {
        if (string.IsNullOrWhiteSpace(lastOutputPath))
        {
            _cachedFinalOutputSize = null;
            _cachedFinalOutputPath = null;
            return LastOutputProbe.Empty;
        }

        // While recording, the file is still growing, so re-stat each poll.
        // Once recording stops, the size is final and cached until the path changes.
        var isFinalAndCached = !isRecording &&
                               _cachedFinalOutputSize.HasValue &&
                               string.Equals(_cachedFinalOutputPath, lastOutputPath, StringComparison.Ordinal);
        if (isFinalAndCached)
        {
            return new LastOutputProbe(true, _cachedFinalOutputSize);
        }

        try
        {
            var size = new FileInfo(lastOutputPath).Length;
            if (!isRecording)
            {
                _cachedFinalOutputSize = size;
                _cachedFinalOutputPath = lastOutputPath;
            }

            return new LastOutputProbe(true, size);
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub output file probe: {ex.Message}");
            return LastOutputProbe.Empty;
        }
    }

    private readonly record struct LastOutputProbe(bool Exists, long? SizeBytes)
    {
        public static LastOutputProbe Empty { get; } = new(false, null);
    }
}
