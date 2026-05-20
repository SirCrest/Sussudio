using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class McpDiagnosticSessionResultSurfaceContractsTests
{
    [Fact]
    public Task ResultFormatterOwnsFormattedSummaryText()
        => global::Program.DiagnosticSessionResultFormatter_OwnsFormattedSummaryText();

    [Fact]
    public Task ResultBuilderOwnsSummaryConstruction()
        => global::Program.DiagnosticSessionResultBuilder_OwnsSummaryConstruction();

    [Fact]
    public Task ResultBuilderDiagnosticHealthVerdictLivesInFocusedPartial()
        => global::Program.DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesInFocusedPartial();

    [Fact]
    public Task ResultBuilderOwnsSummaryWriteFailures()
        => global::Program.DiagnosticSessionResultBuilder_OwnsSummaryWriteFailures();

    [Fact]
    public Task ResultArtifactsOwnPreSummaryWrites()
        => global::Program.DiagnosticSessionResultArtifacts_OwnPreSummaryWrites();

    [Fact]
    public Task OptionalTextFormatterOwnsSharedFormattingHelpers()
        => global::Program.DiagnosticSessionOptionalTextFormatter_OwnsSharedFormattingHelpers();
}
