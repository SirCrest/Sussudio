using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Telemetry;

// Null-object telemetry provider used when source telemetry is unavailable or
// intentionally disabled.
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
