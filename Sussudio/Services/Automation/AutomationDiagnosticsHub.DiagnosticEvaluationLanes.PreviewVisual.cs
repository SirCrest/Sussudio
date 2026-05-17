using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildVisualLane(CaptureHealthSnapshot health)
    {
        return $"visual crop samples={health.VisualCadenceSampleCount} output={health.VisualCadenceOutputObservedFps:0.##}fps changes={health.VisualCadenceChangeObservedFps:0.##}fps repeat={health.VisualCadenceRepeatFramePercent:0.###}% repeatFrames={health.VisualCadenceRepeatFrameCount} longestRepeatRun={health.VisualCadenceLongestRepeatRun} confidence={health.VisualCadenceMotionConfidence}";
    }
}
