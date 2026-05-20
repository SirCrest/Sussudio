using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelUiDispatchController_UsesDependencyCompositionContext()
    {
        var controllerGraphUiDispatchText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.UiDispatch.cs").Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs").Replace("\r\n", "\n");
        var uiDispatchControllerContextText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.Context.cs").Replace("\r\n", "\n");

        AssertContains(controllerGraphUiDispatchText, "private sealed partial class MainViewModelControllerGraph");
        AssertContains(controllerGraphUiDispatchText, "private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)");
        AssertContains(controllerGraphUiDispatchText, "DispatcherQueue = viewModel._dispatcherQueue,");
        AssertContains(controllerGraphUiDispatchText, "IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,");
        AssertContains(controllerGraphUiDispatchText, "SetStatusText = value => viewModel.StatusText = value,");

        AssertContains(uiDispatchControllerText, "internal sealed class MainViewModelUiDispatchController");
        AssertContains(uiDispatchControllerText, "private readonly MainViewModelUiDispatchControllerContext _context;");
        AssertDoesNotContain(uiDispatchControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(uiDispatchControllerText, "_viewModel.");
        AssertContains(uiDispatchControllerContextText, "internal sealed class MainViewModelUiDispatchControllerContext");
        AssertContains(uiDispatchControllerContextText, "public required DispatcherQueue DispatcherQueue { get; init; }");
        AssertContains(uiDispatchControllerContextText, "public required Func<bool> IsDisposing { get; init; }");

        return Task.CompletedTask;
    }
}
