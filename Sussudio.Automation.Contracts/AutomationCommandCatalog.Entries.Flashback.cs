using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Tools;

public static partial class AutomationCommandCatalog
{
    private static void RegisterFlashbackEntries(Dictionary<AutomationCommandKind, AutomationCommandMetadata> entries)
    {
        Set(entries, AutomationCommandKind.FlashbackAction, "{ action: string, positionMs?: double }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback <action>", "Control flashback playback and range markers.", Required("action", AutomationPayloadFieldType.String), Optional("positionMs", AutomationPayloadFieldType.Number));
        Set(entries, AutomationCommandKind.FlashbackExport, "{ seconds?: double, outputPath: string, useSelectionRange?: bool, force?: bool }", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.WriteFile, "flashback export [seconds] [path] [--range] [--force]", "Export flashback buffer to MP4. Refuses an existing destination unless force=true.", Optional("seconds", AutomationPayloadFieldType.Number), Required("outputPath", AutomationPayloadFieldType.String), Optional("useSelectionRange", AutomationPayloadFieldType.Boolean), Optional("force", AutomationPayloadFieldType.Boolean));
        Set(entries, AutomationCommandKind.FlashbackGetSegments, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "flashback segments", "List flashback buffer segments.");
        Set(entries, AutomationCommandKind.RestartFlashback, "{}", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "flashback apply", "Restart Flashback to apply deferred settings.");
        Set(entries, AutomationCommandKind.SetFlashbackEnabled, "{ enabled: bool }", ready: false, timeoutMs: AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "flashback on|off", "Enable or disable Flashback.", Required("enabled", AutomationPayloadFieldType.Boolean));
    }
}
