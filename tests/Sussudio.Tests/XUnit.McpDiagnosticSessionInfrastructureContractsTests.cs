using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class McpDiagnosticSessionInfrastructureContractsTests
{
    [Fact]
    public Task RunnerWritesTerminalArtifactsOnFinalSnapshotFailure()
        => global::Program.DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts();

    [Fact]
    public Task ModelsAreSplitFromRunnerBehavior()
        => global::Program.DiagnosticSessionModels_AreSplitFromRunnerBehavior();

    [Fact]
    public Task InitialSnapshotOwnsBaselineCapture()
        => global::Program.DiagnosticSessionInitialSnapshot_OwnsBaselineCapture();

    [Fact]
    public Task RunnerOwnsCompatibilitySurface()
        => global::Program.DiagnosticSessionRunner_OwnsCompatibilitySurface();
}
