using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelDependencyCompositionContractsTests
{
    public PresentationPreviewMainViewModelDependencyCompositionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task MainViewModelUsesDependencyCompositionSeam()
        => global::Program.MainViewModel_UsesDependencyCompositionSeam();

    [Fact]
    public Task UiDispatchControllerUsesDependencyCompositionContext()
        => global::Program.MainViewModelUiDispatchController_UsesDependencyCompositionContext();

    [Fact]
    public Task PresentationControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelPresentationControllers_UseDependencyCompositionContexts();

    [Fact]
    public Task RecordingTransitionUsesDependencyCompositionContext()
        => global::Program.MainViewModelRecordingTransition_UsesDependencyCompositionContext();

    [Fact]
    public Task CaptureAndDeviceControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelCaptureDeviceControllers_UseDependencyCompositionContexts();

    [Fact]
    public Task RuntimeControllersUseDependencyCompositionContexts()
        => global::Program.MainViewModelRuntimeControllers_UseDependencyCompositionContexts();
}
