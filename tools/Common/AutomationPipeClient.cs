namespace Sussudio.Tools;

// Shared pipe client used by ssctl, diagnostic sessions, and smaller smoke
// tools. It centralizes the JSON framing/auth/timeout rules so every external
// harness exercises the same protocol as MCP.
internal static partial class AutomationPipeClient
{
}
