using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandMaps_StayAligned_ForAdvancedMcpControls()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var scriptText = ReadRepoFile("tools/send-automation-command.ps1");
        var resolveCommand = protocolType.GetMethod(
            "ResolveCommand",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.ResolveCommand not found.");

        foreach (var (name, ordinal) in new[]
        {
            ("GetCaptureOptions", 29),
            ("SetPreset", 30),
            ("SetSplitEncodeMode", 31),
            ("SetMjpegDecoderCount", 32),
            ("SetShowAllCaptureOptions", 33),
            ("SetPreviewVolume", 34),
            ("SetStatsVisible", 35)
        })
        {
            AssertEqual(ordinal, Convert.ToInt32(Enum.Parse(enumType, name)), $"AutomationCommandKind.{name}");
            AssertEqual(ordinal, Convert.ToInt32(resolveCommand.Invoke(null, new object?[] { name })), $"AutomationPipeProtocol.ResolveCommand({name})");
        }

        AssertContains(protocolText, "Enum.GetValues<AutomationCommandKind>()");

        AssertContains(scriptText, "AutomationClient\\AutomationClient.csproj");
        AssertContains(scriptText, "Get-AutomationClientInputWriteTimeUtc");
        AssertContains(scriptText, "Test-AutomationClientBuildFresh");
        AssertContains(scriptText, "AutomationClient build failed with exit code $LASTEXITCODE.");
        AssertContains(scriptText, "AutomationClient build output is stale after rebuild");
        AssertContains(scriptText, "$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"");
        AssertContains(scriptText, "\"--command\", $Command");
        AssertContains(scriptText, "$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))");
        AssertContains(scriptText, "\"--payload-base64\", $payloadBase64");
        AssertContains(scriptText, "[int]$ResponseTimeoutMs = 0");
        AssertContains(scriptText, "\"--response-timeout-ms\", $ResponseTimeoutMs");
        AssertDoesNotContain(scriptText, "function Resolve-AutomationCommand");

        return Task.CompletedTask;
    }

}
