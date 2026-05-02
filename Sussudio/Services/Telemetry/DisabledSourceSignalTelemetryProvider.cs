using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Devices;

namespace ElgatoCapture.Services.Telemetry;

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
