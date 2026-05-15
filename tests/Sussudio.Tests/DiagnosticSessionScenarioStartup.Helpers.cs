static partial class Program
{
    private static string ReadDiagnosticSessionScenarioStartupSource()
        => string.Join(
                "\n",
                ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.Registrations.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.DeferredSettings.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.Playback.cs"))
            .Replace("\r\n", "\n");
}
