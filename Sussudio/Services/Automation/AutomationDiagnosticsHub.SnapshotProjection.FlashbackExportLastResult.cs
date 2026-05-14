using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)
        => new()
        {
            LastExportId = health.LastExportId,
            LastExportPath = health.LastExportPath,
            LastExportSuccess = health.LastExportSuccess,
            LastExportMessage = health.LastExportMessage
        };

    private readonly record struct FlashbackExportLastResultProjection
    {
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }
}
