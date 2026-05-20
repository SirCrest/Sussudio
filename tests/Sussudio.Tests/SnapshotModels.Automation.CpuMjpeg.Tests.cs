using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesCpuMjpegMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotCpuMjpegMetricContract(snapshotType);
    }
}
