using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public sealed class DisabledSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    public Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-provider-disabled"));
    }
}
