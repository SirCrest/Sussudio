using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private SnapshotStatusProjection BuildSnapshotStatusProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsInitialized = viewModelSnapshot.IsInitialized,
            IsPreviewing = viewModelSnapshot.IsPreviewing,
            IsRecording = viewModelSnapshot.IsRecording,
            VerificationInProgress = Volatile.Read(ref _verificationInProgress) != 0,
            IsAudioEnabled = viewModelSnapshot.IsAudioEnabled,
            IsAudioPreviewEnabled = viewModelSnapshot.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = viewModelSnapshot.IsCustomAudioInputEnabled,
            SessionState = captureRuntime.SessionState,
            StatusText = viewModelSnapshot.StatusText
        };

    private readonly record struct SnapshotStatusProjection
    {
        public DateTimeOffset TimestampUtc { get; init; }
        public bool IsInitialized { get; init; }
        public bool IsPreviewing { get; init; }
        public bool IsRecording { get; init; }
        public bool VerificationInProgress { get; init; }
        public bool IsAudioEnabled { get; init; }
        public bool IsAudioPreviewEnabled { get; init; }
        public bool IsCustomAudioInputEnabled { get; init; }
        public CaptureSessionState SessionState { get; init; }
        public string StatusText { get; init; }
    }

    private static SnapshotStatusFlattenedProjection BuildSnapshotStatusFlattenedProjection(
        SnapshotStatusProjection snapshotStatus)
        => new()
        {
            TimestampUtc = snapshotStatus.TimestampUtc,
            IsInitialized = snapshotStatus.IsInitialized,
            IsPreviewing = snapshotStatus.IsPreviewing,
            IsRecording = snapshotStatus.IsRecording,
            VerificationInProgress = snapshotStatus.VerificationInProgress,
            IsAudioEnabled = snapshotStatus.IsAudioEnabled,
            IsAudioPreviewEnabled = snapshotStatus.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = snapshotStatus.IsCustomAudioInputEnabled,
            SessionState = snapshotStatus.SessionState,
            StatusText = snapshotStatus.StatusText
        };

    private readonly record struct SnapshotStatusFlattenedProjection
    {
        public DateTimeOffset TimestampUtc { get; init; }
        public bool IsInitialized { get; init; }
        public bool IsPreviewing { get; init; }
        public bool IsRecording { get; init; }
        public bool VerificationInProgress { get; init; }
        public bool IsAudioEnabled { get; init; }
        public bool IsAudioPreviewEnabled { get; init; }
        public bool IsCustomAudioInputEnabled { get; init; }
        public CaptureSessionState SessionState { get; init; }
        public string StatusText { get; init; }
    }
}
