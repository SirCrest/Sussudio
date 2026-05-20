using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Tools;

public static partial class AutomationCommandCatalog
{
    private static void RegisterVerificationEntries(Dictionary<AutomationCommandKind, AutomationCommandMetadata> entries)
    {
        Set(entries, AutomationCommandKind.VerifyLastRecording, "{}", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.None, "verify", "Verify the last recording.");
        Set(entries, AutomationCommandKind.VerifyFile, "{ filePath: string, verificationProfile?: string }", ready: false, timeoutMs: AutomationPipeProtocol.ExtendedResponseTimeoutMs, pathPolicy: AutomationCommandPathPolicy.ReadFile, "verify <path> [--profile NAME]", "Verify an arbitrary media file.", Required("filePath", AutomationPayloadFieldType.String), Optional("verificationProfile", AutomationPayloadFieldType.String));
    }
}
