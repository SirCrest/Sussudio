using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class ArchitectureDocsReferenceIntegrityTests
{
    [Fact]
    public Task ReadRepoFileLiteralPathsResolve()
        => global::Program.ArchitectureDocs_ReadRepoFileLiteralPathsResolve();

    [Fact]
    public Task CleanupPlanFileReferencesResolve()
        => global::Program.ArchitectureCleanupPlan_FileReferencesResolve();

    [Fact]
    public Task CleanupPlanCoversArchitectureDocsTestFamily()
        => global::Program.ArchitectureCleanupPlan_CoversArchitectureDocsTestFamily();

    [Fact]
    public Task CleanupPlanDefinesSmallFileHygiene()
        => global::Program.ArchitectureCleanupPlan_DefinesSmallFileHygiene();

    [Fact]
    public Task TestMigrationPlanFileReferencesResolveAndNamesValidationCommands()
        => global::Program.TestMigrationPlan_FileReferencesResolveAndNamesValidationCommands();

    [Fact]
    public Task TestMigrationPlanCoversXUnitInventory()
        => global::Program.TestMigrationPlan_CoversXUnitInventory();
}
