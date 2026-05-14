using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task InvokeInitializeAsync(object captureService, object device, object settings)
    {
        var initialize = captureService.GetType().GetMethod(
            "InitializeAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { device.GetType(), settings.GetType(), typeof(CancellationToken) },
            modifiers: null);
        if (initialize == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync method not found.");
        }

        var task = initialize.Invoke(captureService, new[] { device, settings, CancellationToken.None }) as Task;
        if (task == null)
        {
            throw new InvalidOperationException("CaptureService.InitializeAsync did not return a Task.");
        }

        await task.ConfigureAwait(false);
    }

    private static async Task DisposeAsync(object captureService)
    {
        await DisposeValueTaskAsync(captureService).ConfigureAwait(false);
    }

    private static async Task DisposeValueTaskAsync(object instance)
    {
        var disposeAsync = instance.GetType().GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance);
        if (disposeAsync == null)
        {
            return;
        }

        var valueTask = disposeAsync.Invoke(instance, null);
        if (valueTask == null)
        {
            return;
        }

        var asTaskMethod = valueTask.GetType().GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance);
        if (asTaskMethod?.Invoke(valueTask, null) is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition, string description, int timeoutMs = 2000, int pollMs = 25)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollMs).ConfigureAwait(false);
        }

        throw new InvalidOperationException($"Timed out waiting for condition: {description}");
    }
}
