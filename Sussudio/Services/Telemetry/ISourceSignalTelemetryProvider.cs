using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Devices;

namespace Sussudio.Services.Telemetry;

public interface ISourceSignalTelemetryProvider
{
    Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default);
}
