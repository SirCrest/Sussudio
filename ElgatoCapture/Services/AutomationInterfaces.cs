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
    IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100);
    Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default);
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
