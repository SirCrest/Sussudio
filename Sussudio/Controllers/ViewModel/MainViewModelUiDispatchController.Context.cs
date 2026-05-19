using System;
using Microsoft.UI.Dispatching;

namespace Sussudio.Controllers;

internal sealed class MainViewModelUiDispatchControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<bool> IsDisposing { get; init; }
    public required Action<string> Log { get; init; }
    public required Action<Exception> LogException { get; init; }
    public required Action<string> SetStatusText { get; init; }
}
