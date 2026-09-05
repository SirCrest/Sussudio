using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.ViewModels;
using Xunit;

namespace Sussudio.Tests;

public sealed class UiDispatchTests
{
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("window-action")]
    [InlineData("window-async")]
    [InlineData("window-value")]
    [InlineData("viewmodel-async")]
    [InlineData("viewmodel-value")]
    public async Task AlreadyCanceled_DoesNotEnqueueOrExecute(string path)
    {
        var queue = new DispatcherQueue();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;

        var invocation = Invoke(path, queue, () => calls++, cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.WaitAsync(Watchdog));
        Assert.Equal(0, calls);
        Assert.Equal(0, queue.PendingCount);
    }

    [Theory]
    [InlineData("window-action")]
    [InlineData("window-async")]
    [InlineData("window-value")]
    [InlineData("viewmodel-async")]
    [InlineData("viewmodel-value")]
    public async Task CanceledWhileQueued_CallbackDoesNotExecuteOperation(string path)
    {
        var queue = new DispatcherQueue();
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var invocation = Invoke(path, queue, () => calls++, cancellation.Token);
        Assert.Equal(1, queue.PendingCount);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.WaitAsync(Watchdog));
        queue.RunNext();

        Assert.Equal(0, calls);
        Assert.Equal(0, queue.PendingCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task StartedAsyncOperation_PreservesEachOwnersCancellationContract(
        bool useViewModel, bool failOperation)
    {
        var queue = new DispatcherQueue();
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationFinished = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("operation failure");
        var finished = false;

        async Task Operation()
        {
            try
            {
                started.SetResult(null);
                await release.Task.WaitAsync(Watchdog);
                finished = true;
                if (failOperation)
                {
                    throw failure;
                }
            }
            finally
            {
                operationFinished.TrySetResult(null);
            }
        }

        var invocation = useViewModel
            ? ViewModelController(queue).InvokeAsync(Operation, cancellation.Token)
            : WindowController(queue).InvokeAsync(Operation, cancellation.Token);
        bool completedAfterCancellation;
        try
        {
            Assert.False(invocation.IsCompleted);
            queue.RunNext();
            await started.Task.WaitAsync(Watchdog);
            cancellation.Cancel();
            completedAfterCancellation = invocation.IsCompleted;
            Assert.False(finished);
            if (!useViewModel)
            {
                // A stalled compositor operation must not prevent an automation timeout.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.WaitAsync(Watchdog));
            }
        }
        finally
        {
            // Always release the real async callback, even when a regression fails assertions.
            release.TrySetResult(null);
            await operationFinished.Task.WaitAsync(Watchdog);
        }

        if (!useViewModel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.WaitAsync(Watchdog));
        }
        else if (failOperation)
        {
            var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => invocation.WaitAsync(Watchdog));
            Assert.Same(failure, observed);
        }
        else
        {
            await invocation.WaitAsync(Watchdog);
        }

        Assert.Equal(!useViewModel, completedAfterCancellation);
        Assert.True(finished);
    }

    [Theory]
    [InlineData("window-action")]
    [InlineData("window-value")]
    [InlineData("viewmodel-value")]
    public async Task StartedSynchronousOperation_PreservesEachOwnersCancellationContract(string path)
    {
        var queue = new DispatcherQueue();
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        int Operation()
        {
            calls++;
            cancellation.Cancel();
            return 42;
        }

        var invocation = Invoke(path, queue, Operation, cancellation.Token);
        queue.RunNext();
        if (path.StartsWith("window-", StringComparison.Ordinal))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.WaitAsync(Watchdog));
        }
        else
        {
            await invocation.WaitAsync(Watchdog);
            Assert.Equal(42, await ((Task<int>)invocation).WaitAsync(Watchdog));
        }

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("window-action")]
    [InlineData("window-async")]
    [InlineData("window-value")]
    [InlineData("viewmodel-async")]
    [InlineData("viewmodel-value")]
    public async Task RejectedEnqueue_FaultsWithoutExecuting(string path)
    {
        var queue = new DispatcherQueue { AcceptEnqueue = false };
        var calls = 0;

        var invocation = Invoke(path, queue, () => calls++, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => invocation.WaitAsync(Watchdog));
        Assert.Equal(0, calls);
        Assert.Equal(0, queue.PendingCount);
    }

    [Theory]
    [InlineData("window-action")]
    [InlineData("window-async")]
    [InlineData("window-value")]
    [InlineData("viewmodel-async")]
    [InlineData("viewmodel-value")]
    public async Task OnUiThread_ExecutesDirectlyAndPreservesResult(string path)
    {
        var queue = new DispatcherQueue { HasThreadAccess = true, AcceptEnqueue = false };
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        int Operation()
        {
            calls++;
            cancellation.Cancel();
            return 42;
        }

        var invocation = Invoke(path, queue, Operation, cancellation.Token);
        await invocation.WaitAsync(Watchdog);

        Assert.Equal(1, calls);
        Assert.Equal(0, queue.PendingCount);
        if (invocation is Task<int> valueInvocation)
        {
            Assert.Equal(42, await valueInvocation.WaitAsync(Watchdog));
        }
    }

    private static Task Invoke(string path, DispatcherQueue queue, Func<int> operation, CancellationToken token)
        => path switch
        {
            "window-action" => WindowController(queue).InvokeAsync((Action)(() => { operation(); }), token),
            "window-async" => WindowController(queue).InvokeAsync(() => { operation(); return Task.CompletedTask; }, token),
            "window-value" => WindowController(queue).InvokeWithRetryAsync(operation, "Queue rejected operation.", token),
            "viewmodel-async" => ViewModelController(queue).InvokeAsync(() => { operation(); return Task.CompletedTask; }, token),
            "viewmodel-value" => ViewModelController(queue).InvokeAsync(operation, token),
            _ => throw new ArgumentOutOfRangeException(nameof(path))
        };

    private static WindowUiDispatchController WindowController(DispatcherQueue queue)
        => new(new WindowUiDispatchControllerContext
        {
            DispatcherQueue = queue,
            ViewModel = new MainViewModel()
        });

    private static MainViewModelUiDispatchController ViewModelController(DispatcherQueue queue)
        => new(new MainViewModelUiDispatchControllerContext
        {
            DispatcherQueue = queue,
            IsDisposing = () => false,
            Log = _ => { },
            LogException = _ => { },
            SetStatusText = _ => { }
        });
}
