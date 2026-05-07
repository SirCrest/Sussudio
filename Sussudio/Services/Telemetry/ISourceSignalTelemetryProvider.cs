using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Devices;

namespace Sussudio.Services.Telemetry;

// Read-only source-signal telemetry provider contract. Implementations must not
// mutate device state or block capture startup on telemetry failure.
public interface ISourceSignalTelemetryProvider
{
    Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default);
}
