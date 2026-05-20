using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class SsctlFormatterContractsTests
{
    [Fact]
    public Task EmitsCoreSnapshotSections()
        => global::Program.SsctlFormatters_EmitCoreSnapshotSections();

    [Fact]
    public Task SnapshotSourceOwnershipIsSplit()
        => global::Program.SsctlFormatters_SnapshotSourceOwnership_IsSplit();

    [Fact]
    public Task TimelineOutputPreservesTableAndSummary()
        => global::Program.SsctlFormatters_TimelineOutputPreservesTableAndSummary();
}
