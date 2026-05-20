using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelOutputPathContractsTests
{
    public PresentationPreviewMainViewModelOutputPathContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task OutputPathSelectionLivesInFocusedOwner()
        => global::Program.MainViewModelOutputPathSelection_LivesInFocusedPartial();

    [Fact]
    public Task OutputDriveFreeSpacePresentationHandlesInvalidPaths()
        => global::Program.OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty();

    [Fact]
    public Task OutputDriveFreeSpacePresentationLivesInFocusedHelper()
        => global::Program.OutputDriveSpacePresentationBuilder_LivesInFocusedHelper();
}
