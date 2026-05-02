using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Devices;

namespace Sussudio.Services.Telemetry;

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
