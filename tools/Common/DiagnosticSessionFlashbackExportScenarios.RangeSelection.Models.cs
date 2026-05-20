using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private readonly record struct FlashbackSelectionRange(
        JsonElement BaselineSnapshot,
        int RangeStartMs,
        int RangeEndMs);
}
