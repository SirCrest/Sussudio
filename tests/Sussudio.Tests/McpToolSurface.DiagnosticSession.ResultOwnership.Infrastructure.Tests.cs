using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionOptionalTextFormatter_OwnsSharedFormattingHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Preview.cs")
            .Replace("\r\n", "\n");
        var textHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionOptionalTextFormatter.cs")
            .Replace("\r\n", "\n");

        AssertContains(textHelpersText, "internal static class DiagnosticSessionOptionalTextFormatter");
        AssertContains(textHelpersText, "internal static string FormatOptional(string value)");
        AssertContains(textHelpersText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(formatterText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(artifactsText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(artifactsText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(artifactsText, "internal static object BuildFrameLedgerTrace(");
        AssertContains(artifactsText, "internal static bool TryGetSnapshot(");
        AssertContains(artifactsText, "internal static bool TryGetVerification(");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(runnerText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(runnerText, "private static bool TryGetVerification(");

        return Task.CompletedTask;
    }
}
