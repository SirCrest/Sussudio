using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Automation;

// Window operations that automation can request without reaching into WinUI
// implementation details.
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

// Diagnostics facade consumed by the command dispatcher and MCP/ssctl tools.
public interface IAutomationDiagnosticsHub : IDisposable, IAsyncDisposable
{
    AutomationSnapshot GetLatestSnapshot();
    Task<AutomationSnapshot> RefreshSnapshotNowAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline(int maxEntries = 240);
    IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100);
    Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default);
    Task<RecordingVerificationResult> VerifyFileAsync(
        string filePath,
        CancellationToken cancellationToken = default,
        string? verificationProfile = null);
    void Start();
    Task StopAsync(CancellationToken cancellationToken = default);
    event EventHandler<AutomationSnapshot>? SnapshotUpdated;
}

// Executes one authenticated automation request and returns the protocol DTO.
public interface IAutomationCommandDispatcher
{
    Task<AutomationCommandResponse> ExecuteAsync(
        AutomationCommandRequest request,
        CancellationToken cancellationToken = default);
}
