using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class UnifiedCaptureDisposalTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopFailureRetainsItsSourceStackAfterCleanup(bool forPreviewReinitialize)
    {
        var type = SussudioAssembly.Load().GetType("Sussudio.Services.Capture.UnifiedVideoCapture", throwOnError: true)!;
        var capture = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException("Unified capture construction failed.");
        using var source = new CancellationTokenSource();
        var originalFailure = new InvalidOperationException("capture cancellation callback failed");
        using var registration = source.Token.Register(() => throw originalFailure);
        Field(type, "_readCts").SetValue(capture, source);

        var dispose = forPreviewReinitialize
            ? (type.GetMethod("DisposeForPreviewReinitAsync", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Preview reinitialization disposal was not found."))
                .CreateDelegate<Func<ValueTask>>(capture)
            : ((IAsyncDisposable)capture).DisposeAsync;

        try
        {
            var failure = await Assert.ThrowsAsync<AggregateException>(() => dispose().AsTask());
            Assert.Same(originalFailure, Assert.Single(failure.InnerExceptions));
            Assert.Contains("UnifiedVideoCapture.StopAsync", failure.StackTrace);
            Assert.True((bool)Field(type, "_disposed").GetValue(capture)!);
            Assert.Null(Field(type, "_readCts").GetValue(capture));
            Assert.Throws<ObjectDisposedException>(() => source.Token);
        }
        finally
        {
            await ((IAsyncDisposable)capture).DisposeAsync();
        }
    }

    private static FieldInfo Field(Type type, string name)
        => type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Unified capture field '{name}' was not found.");
}
