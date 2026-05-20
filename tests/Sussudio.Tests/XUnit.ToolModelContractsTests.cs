using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class ToolModelContractsTests
{
    [Fact]
    public async Task NvmlSnapshotComputedPropertiesConvertUnits()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.NvmlSnapshot_ComputedProperties_ConvertUnits();
    }

    [Fact]
    public async Task CaptureSessionSnapshotDefaultState()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.CaptureSessionSnapshot_DefaultState();
    }
}
