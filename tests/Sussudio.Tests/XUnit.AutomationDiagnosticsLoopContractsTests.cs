using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationDiagnosticsLoopContractsTests
{
    public AutomationDiagnosticsLoopContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task DiagnosticsLoopDoesNotRebuildAutomationOptionsEachPoll()
        => global::Program.DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll();
}
