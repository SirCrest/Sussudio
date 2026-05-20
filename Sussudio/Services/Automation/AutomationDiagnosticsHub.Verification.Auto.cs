using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
