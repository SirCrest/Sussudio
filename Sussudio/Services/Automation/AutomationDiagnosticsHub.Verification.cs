using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    public async Task<RecordingVerificationResult> VerifyLastRecordingAsync(CancellationToken cancellationToken = default)
    {
        await _verificationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _verificationInProgress);
        try
        {
            var runtimeSnapshot = await _snapshotQueryPort
                .GetCaptureRuntimeSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);

            var verification = await _recordingVerifier
                .VerifyAsync(runtimeSnapshot.LastOutputPath, runtimeSnapshot, cancellationToken)
                .ConfigureAwait(false);

            lock (_stateLock)
            {
                _lastVerification = verification;
            }

            var mismatchDetail = !verification.Succeeded && !string.IsNullOrWhiteSpace(verification.PrimaryMismatchCode)
                ? $" [{verification.PrimaryMismatchCode}"
                + (verification.PrimaryMismatchExpected != null ? $", expected={verification.PrimaryMismatchExpected}" : string.Empty)
                + (verification.PrimaryMismatchActual != null ? $", actual={verification.PrimaryMismatchActual}" : string.Empty)
                + "]"
                : string.Empty;

            AddEvent(
                verification.Succeeded ? DiagnosticsSeverity.Info : DiagnosticsSeverity.Error,
                DiagnosticsCategory.Verification,
                $"{verification.Message}{mismatchDetail}");

            await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return verification;
        }
        finally
        {
            Interlocked.Decrement(ref _verificationInProgress);
            _verificationGate.Release();
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub post-verification snapshot refresh: {ex.Message}");
                }
            }
        }
    }

    public async Task<RecordingVerificationResult> VerifyFileAsync(
        string filePath,
        string? verificationProfile = null,
        CancellationToken cancellationToken = default)
    {
        await _verificationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.Increment(ref _verificationInProgress);
        try
        {
            var runtimeSnapshot = await _snapshotQueryPort
                .GetCaptureRuntimeSnapshotAsync(cancellationToken)
                .ConfigureAwait(false);
            runtimeSnapshot = ApplyVerificationProfile(runtimeSnapshot, filePath, verificationProfile);

            var verification = await _recordingVerifier
                .VerifyAsync(filePath, runtimeSnapshot, cancellationToken)
                .ConfigureAwait(false);

            lock (_stateLock)
            {
                _lastVerification = verification;
            }

            AddEvent(
                verification.Succeeded ? DiagnosticsSeverity.Info : DiagnosticsSeverity.Error,
                DiagnosticsCategory.Verification,
                $"File verification ({System.IO.Path.GetFileName(filePath)}): {verification.Message}");

            await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return verification;
        }
        finally
        {
            Interlocked.Decrement(ref _verificationInProgress);
            _verificationGate.Release();
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning($"Suppressed exception in AutomationDiagnosticsHub post-verification snapshot refresh: {ex.Message}");
                }
            }
        }
    }

    private static CaptureRuntimeSnapshot ApplyVerificationProfile(
        CaptureRuntimeSnapshot runtimeSnapshot,
        string filePath,
        string? verificationProfile)
    {
        if (!string.Equals(verificationProfile, "flashback-export", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(runtimeSnapshot.FlashbackExportVerificationFormat))
        {
            return runtimeSnapshot;
        }

        return new CaptureRuntimeSnapshot
        {
            TimestampUtc = runtimeSnapshot.TimestampUtc,
            RequestedWidth = runtimeSnapshot.RequestedWidth,
            RequestedHeight = runtimeSnapshot.RequestedHeight,
            RequestedFrameRate = runtimeSnapshot.RequestedFrameRate,
            RequestedFrameRateArg = runtimeSnapshot.RequestedFrameRateArg,
            RequestedFrameRateNumerator = runtimeSnapshot.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = runtimeSnapshot.RequestedFrameRateDenominator,
            RequestedFormat = runtimeSnapshot.RequestedFormat,
            RequestedHdrEnabled = runtimeSnapshot.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = runtimeSnapshot.RequestedHdrMasteringMetadata,
            HdrOutputActive = runtimeSnapshot.HdrOutputActive,
            HdrAutoDowngraded = runtimeSnapshot.HdrAutoDowngraded,
            NegotiatedWidth = runtimeSnapshot.NegotiatedWidth,
            NegotiatedHeight = runtimeSnapshot.NegotiatedHeight,
            NegotiatedFrameRate = runtimeSnapshot.NegotiatedFrameRate,
            NegotiatedFrameRateArg = runtimeSnapshot.NegotiatedFrameRateArg,
            NegotiatedFrameRateNumerator = runtimeSnapshot.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = runtimeSnapshot.NegotiatedFrameRateDenominator,
            FlashbackExportOutputPath = filePath,
            FlashbackExportVerificationFormat = runtimeSnapshot.FlashbackExportVerificationFormat,
            FlashbackCodecDowngradeReason = runtimeSnapshot.FlashbackCodecDowngradeReason
        };
    }

    private bool ShouldAutoVerifySnapshot(AutomationSnapshot snapshot)
    {
        var verificationIdle = Volatile.Read(ref _verificationInProgress) == 0 &&
                               Volatile.Read(ref _autoVerificationScheduled) == 0;
        return !snapshot.IsRecording &&
               _wasRecording &&
               !string.IsNullOrWhiteSpace(snapshot.LastOutputPath) &&
               verificationIdle;
    }

    private RecordingVerificationResult? CaptureLastVerificationForSnapshot(bool recordingStarted)
    {
        lock (_stateLock)
        {
            if (recordingStarted)
            {
                _lastVerification = null;
            }

            return _lastVerification;
        }
    }

    private void ScheduleAutoVerificationIfNeeded(bool shouldAutoVerify)
    {
        if (!shouldAutoVerify ||
            _cts is not { IsCancellationRequested: false } cts ||
            Interlocked.CompareExchange(ref _autoVerificationScheduled, 1, 0) != 0)
        {
            return;
        }

        AddEvent(
            DiagnosticsSeverity.Info,
            DiagnosticsCategory.Verification,
            "Automatic recording verification started.");
        _autoVerificationTask = Task.Run(async () =>
        {
            try
            {
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                await VerifyLastRecordingAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown - auto-verification cancelled */
            }
            catch (Exception ex)
            {
                AddEvent(
                    DiagnosticsSeverity.Error,
                    DiagnosticsCategory.Verification,
                    $"Automatic recording verification failed: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _autoVerificationScheduled, 0);
            }
        });
    }
}
