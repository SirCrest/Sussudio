using System;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

internal sealed class CompositeSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    private readonly ISourceSignalTelemetryProvider[] _providers;

    public CompositeSourceSignalTelemetryProvider(params ISourceSignalTelemetryProvider[] providers)
    {
        _providers = providers ?? Array.Empty<ISourceSignalTelemetryProvider>();
    }

    public async Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await provider.ReadAsync(device, cancellationToken).ConfigureAwait(false);
            if (result.Availability != SourceTelemetryAvailability.Unavailable)
            {
                return result;
            }
        }

        return SourceSignalTelemetrySnapshot.CreateUnavailable("all-providers-unavailable");
    }
}
