using System.Text.Json;

namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackResultMetrics
{
    public JsonElement EndSnapshot { get; init; }
}
