using System.Text.Json;

namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackSessionMetrics
{
    public bool Observed { get; set; }
    public JsonElement BaselineSnapshot { get; init; }
    public JsonElement EndSnapshot { get; set; }
    public long EndSessionFrameCount { get; set; }
}
