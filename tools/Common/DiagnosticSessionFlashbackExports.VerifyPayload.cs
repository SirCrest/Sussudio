namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExports
{
    internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath) =>
        new()
        {
            ["filePath"] = filePath,
            ["strict"] = true,
            ["verificationProfile"] = "flashback-export"
        };
}
