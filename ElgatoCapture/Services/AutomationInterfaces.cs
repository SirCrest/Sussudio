using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

public interface IAutomationWindowControl
{
    Task MinimizeAsync(CancellationToken cancellationToken = default);
    Task MaximizeAsync(CancellationToken cancellationToken = default);
    Task RestoreAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
    Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default);
    Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default);
    Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default);
    Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(string outputPath, CancellationToken cancellationToken = default);
}

public interface IRecordingVerifier
{
    Task<RecordingVerificationResult> VerifyAsync(
        string? outputPath,
        CaptureRuntimeSnapshot runtimeSnapshot,
        CancellationToken cancellationToken = default);
}

public interface IAutomationDiagnosticsHub : IDisposable, IAsyncDisposable
{
    AutomationSnapshot GetLatestSnapshot();
    IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline(int maxEntries = 240);
    IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100);
    Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default);
    Task<RecordingVerificationResult> VerifyFileAsync(string filePath, CancellationToken cancellationToken = default);
    void Start();
    Task StopAsync(CancellationToken cancellationToken = default);
    event EventHandler<AutomationSnapshot>? SnapshotUpdated;
}

public interface IAutomationCommandDispatcher
{
    Task<AutomationCommandResponse> ExecuteAsync(
        AutomationCommandRequest request,
        CancellationToken cancellationToken = default);
}
