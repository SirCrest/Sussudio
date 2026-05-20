using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class McpDiagnosticSessionRunnerBehaviorContractsTests
{
    [Fact]
    public Task VerifiesFlashbackExportPlaybackCommandFlow()
        => global::Program.DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow();

    [Fact]
    public Task IgnoresTransientFlashbackWarmupWarnings()
        => global::Program.DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings();

    [Fact]
    public Task ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
        => global::Program.DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops();

    [Fact]
    public Task UnknownInitialSnapshotFailsWithoutMutatingState()
        => global::Program.DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState();

    [Fact]
    public Task RetriesSyntheticPipeConnectFailures()
        => global::Program.DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures();

    [Fact]
    public Task RejectsConcurrentInvocationOnSameOutputDirectory()
        => global::Program.DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory();
}
