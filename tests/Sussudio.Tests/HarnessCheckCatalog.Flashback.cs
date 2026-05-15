using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackChecksAsync(List<CheckResult> results)
    {
        await AddFlashbackModelChecksAsync(results);
        await AddFlashbackPlaybackStartupChecksAsync(results);
        await AddFlashbackEncoderSinkCoreChecksAsync(results);
        await AddFlashbackPlaybackTimelineChecksAsync(results);
        await AddFlashbackDecoderChecksAsync(results);
        await AddFlashbackEncoderSinkDrainChecksAsync(results);
        await AddFlashbackExporterChecksAsync(results);
    }
}
