using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Tools;

public static partial class AutomationCommandCatalog
{
    private static void RegisterCoreEntries(Dictionary<AutomationCommandKind, AutomationCommandMetadata> entries)
    {
        Set(entries, AutomationCommandKind.Authenticate, "{ authToken?: string }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "auth", "Validate automation authentication token.", Optional("authToken", AutomationPayloadFieldType.String));
        Set(entries, AutomationCommandKind.GetSnapshot, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "state", "Get full application state snapshot.");
        Set(entries, AutomationCommandKind.GetDiagnostics, "{ maxEvents?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "diagnostics [--max N]", "Get recent diagnostic events.", Optional("maxEvents", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.WaitForCondition, "{ condition?: string, timeoutMs?: int, pollMs?: int }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "wait <condition>", "Wait for an automation condition.", Optional("condition", AutomationPayloadFieldType.String), Optional("timeoutMs", AutomationPayloadFieldType.Integer), Optional("pollMs", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.AssertSnapshot, "{ assertions: array }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "assert <json>", "Assert fields against the current snapshot.", Required("assertions", AutomationPayloadFieldType.Array));
        Set(entries, AutomationCommandKind.GetPerformanceTimeline, "{ maxEntries?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "timeline [--max N]", "Get performance timeline samples.", Optional("maxEntries", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.GetAudioRampTrace, "{ maxEntries?: int }", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "audio-ramp-trace", "Get audio ramp trace diagnostics.", Optional("maxEntries", AutomationPayloadFieldType.Integer));
        Set(entries, AutomationCommandKind.GetAutomationManifest, "{}", ready: false, timeoutMs: DefaultTimeout, pathPolicy: AutomationCommandPathPolicy.None, "manifest", "Get automation command manifest.");
    }
}
