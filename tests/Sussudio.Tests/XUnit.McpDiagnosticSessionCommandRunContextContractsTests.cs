using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class McpDiagnosticSessionCommandRunContextContractsTests
{
    [Fact]
    public Task PipeRetryPolicyOwnsConnectRetryClassification()
        => global::Program.DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification();

    [Fact]
    public Task CommandChannelOwnsSerializedCommandSending()
        => global::Program.DiagnosticSessionCommandChannel_OwnsSerializedCommandSending();

    [Fact]
    public Task JsonArtifactsOwnJsonWritingAndResponseExtractionSplit()
        => global::Program.DiagnosticSessionJsonArtifacts_OwnsJsonWritingAndResponseExtractionSplit();

    [Fact]
    public Task RunStateOwnsTerminalState()
        => global::Program.DiagnosticSessionRunState_OwnsTerminalState();

    [Fact]
    public Task LiveStateWriterOwnsBreadcrumbFile()
        => global::Program.DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile();

    [Fact]
    public Task RunContextOwnsMutableRunInfrastructure()
        => global::Program.DiagnosticSessionRunContext_OwnsMutableRunInfrastructure();

    [Fact]
    public Task RunBootstrapOwnsNormalizedSessionIdentity()
        => global::Program.DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity();

    [Fact]
    public Task OutputLockOwnsExclusiveOutputDirectoryLock()
        => global::Program.DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock();
}
