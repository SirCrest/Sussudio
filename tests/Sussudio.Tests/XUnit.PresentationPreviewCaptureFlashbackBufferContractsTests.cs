using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewCaptureFlashbackBufferContractsTests
{
    public PresentationPreviewCaptureFlashbackBufferContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task FlashbackBufferManagerCleansStaleSessionDirectories()
        => global::Program.FlashbackBufferManager_CleansStaleSessionDirectories();

    [Fact]
    public Task FlashbackBufferManagerPreservesMarkedRecoverySessions()
        => global::Program.FlashbackBufferManager_PreservesMarkedRecoverySessions();
}
