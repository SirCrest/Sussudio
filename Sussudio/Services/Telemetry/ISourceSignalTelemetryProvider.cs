using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Devices;

namespace ElgatoCapture.Services.Telemetry;

public interface ISourceSignalTelemetryProvider
{
    Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default);
}
