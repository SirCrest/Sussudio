using System;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.Controllers;

internal sealed class MainViewModelDisposalControllerContext
{
    public required Func<bool> TryBeginDispose { get; init; }
    public required Action CancelActiveFlashbackExport { get; init; }
    public required Action CancelPendingAudioControlWork { get; init; }
    public required Action StopRuntimeForDispose { get; init; }
    public required Func<Task> CleanupSessionCoordinatorAsync { get; init; }
    public required Func<Task> DisposeSessionCoordinatorAsync { get; init; }
    public required Func<Task> DisposeCaptureServiceAsync { get; init; }
    public required Action DisposeCaptureService { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
}

/// <summary>
/// Owns bounded teardown policy for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDisposalController
{
    private const int DefaultDisposeTimeoutMs = 30000;

    private readonly MainViewModelDisposalControllerContext _context;

    public MainViewModelDisposalController(MainViewModelDisposalControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Dispose()
    {
        var disposeTimeoutMs = GetDisposeTimeoutMs();
        var disposeTask = Task.Run(DisposeCoreAsync);
        var completed = Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).GetAwaiter().GetResult();
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            disposeTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeTimeoutMs = GetDisposeTimeoutMs();
        var disposeTask = DisposeCoreAsync();
        var completed = await Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).ConfigureAwait(false);
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel async dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            await disposeTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel async dispose failed: {ex.Message}");
        }
    }

    private async Task DisposeCoreAsync()
    {
        if (!_context.TryBeginDispose())
        {
            return;
        }

        _context.CancelActiveFlashbackExport();
        _context.CancelPendingAudioControlWork();
        _context.StopRuntimeForDispose();

        var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

        await RunDisposeStepAsync(
            _context.CleanupSessionCoordinatorAsync(),
            stepTimeoutMs,
            "Coordinator cleanup",
            "ViewModel cleanup during dispose failed").ConfigureAwait(false);
        await RunDisposeStepAsync(
            _context.DisposeSessionCoordinatorAsync(),
            stepTimeoutMs,
            "Coordinator dispose",
            "Coordinator dispose failed").ConfigureAwait(false);

        try
        {
            await _context.AwaitWithTimeoutAsync(
                _context.DisposeCaptureServiceAsync(),
                stepTimeoutMs,
                "Capture service dispose").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Capture service async dispose failed: {ex.Message}");
            _context.DisposeCaptureService();
        }
    }

    private static int GetDisposeTimeoutMs()
        => EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

    private async Task RunDisposeStepAsync(
        Task task,
        int timeoutMs,
        string operationName,
        string failureLogPrefix)
    {
        try
        {
            await _context.AwaitWithTimeoutAsync(task, timeoutMs, operationName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"{failureLogPrefix}: {ex.Message}");
        }
    }
}
