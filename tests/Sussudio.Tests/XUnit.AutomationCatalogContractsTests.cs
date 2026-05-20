using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationCatalogContractsTests
{
    [Fact]
    public Task CommandCatalogCoversCommandsAndPolicyMetadata()
        => global::Program.AutomationCommandCatalog_CoversCommandsAndPolicyMetadata();

    [Fact]
    public Task ReliabilityGatesRunToolsAndOfflineHarness()
        => global::Program.ReliabilityGates_RunToolsAndOfflineHarness();

    [Fact]
    public Task ManifestCoversCatalogMetadata()
        => global::Program.AutomationManifest_CoversCatalogMetadata();

    [Fact]
    public Task PathBearingCommandsHaveValidationCoverage()
        => global::Program.AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage();

    [Fact]
    public Task ManifestSerializationIsStable()
        => global::Program.AutomationManifest_SerializationIsStable();
}
