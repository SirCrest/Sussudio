using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class ProjectBuildContractsTests
{
    public ProjectBuildContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ProjectFilePreservesEnglishOnlyPublishLocalePolicy()
        => global::Program.ProjectFile_PreservesEnglishOnlyPublishLocalePolicy();
}
