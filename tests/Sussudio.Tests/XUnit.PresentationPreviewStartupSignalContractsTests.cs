using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewStartupSignalContractsTests
{
    public PresentationPreviewStartupSignalContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupSignalFormatterPreservesStringContracts()
        => global::Program.PreviewStartupSignalFormatter_PreservesSignalStrings();

    [Fact]
    public Task PreviewStartupReadinessSignalControllerPreservesStateContracts()
        => global::Program.PreviewStartupReadinessSignalController_PreservesSignalStateContracts();

    [Fact]
    public Task PreviewStartupFailureTextFormatterPreservesStringContracts()
        => global::Program.PreviewStartupFailureTextFormatter_PreservesFailureStrings();
}
