using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddFlashbackModelChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Flashback models preserve buffer session and export contracts",
            FlashbackModels_PreserveBufferSessionExportContracts);
        await AddCheckAsync(results,
            "Flashback buffer options max disk bytes scales with duration",
            FlashbackBufferOptions_MaxDiskBytes_ScalesWithDuration);
        await AddCheckAsync(results,
            "FlashbackPlaybackState enum has all expected states",
            FlashbackPlaybackState_HasAllExpectedStates);
    }
}
