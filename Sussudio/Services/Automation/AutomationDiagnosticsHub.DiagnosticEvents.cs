using System;
using System.Collections.Generic;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)
    {
        if (snapshot.FlashbackExportActive ||
            snapshot.FlashbackExportId <= 0 ||
            snapshot.FlashbackExportCompletedUtcUnixMs <= 0)
        {
            return;
        }

        var previousId = Interlocked.Read(ref _lastFlashbackExportCompletionEventId);
        if (snapshot.FlashbackExportId <= previousId ||
            Interlocked.CompareExchange(
                ref _lastFlashbackExportCompletionEventId,
                snapshot.FlashbackExportId,
                previousId) != previousId)
        {
            return;
        }

        var status = string.IsNullOrWhiteSpace(snapshot.FlashbackExportStatus)
            ? "Unknown"
            : snapshot.FlashbackExportStatus;
        var severity = status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)
            ? DiagnosticsSeverity.Info
            : status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                ? DiagnosticsSeverity.Warning
                : DiagnosticsSeverity.Error;
        var message = string.IsNullOrWhiteSpace(snapshot.FlashbackExportMessage)
            ? status
            : snapshot.FlashbackExportMessage;
        var failureKind = string.IsNullOrWhiteSpace(snapshot.FlashbackExportFailureKind)
            ? "None"
            : snapshot.FlashbackExportFailureKind;

        AddEvent(
            severity,
            DiagnosticsCategory.Flashback,
            $"Flashback export completed: status={status} id={snapshot.FlashbackExportId} " +
            $"elapsed={snapshot.FlashbackExportElapsedMs}ms progress={snapshot.FlashbackExportPercent:0.##}% " +
            $"segments={snapshot.FlashbackExportSegmentsProcessed}/{snapshot.FlashbackExportTotalSegments} " +
            $"bytes={snapshot.FlashbackExportOutputBytes} kind={failureKind} path={snapshot.FlashbackExportOutputPath} message={message}");
    }

    private void AddEventThrottled(
        string key,
        DiagnosticsSeverity severity,
        DiagnosticsCategory category,
        string message,
        int throttleMs = 3000)
    {
        var nowTick = Environment.TickCount64;
        lock (_stateLock)
        {
            if (_eventThrottleTicks.TryGetValue(key, out var lastTick) && nowTick - lastTick < throttleMs)
            {
                return;
            }

            _eventThrottleTicks[key] = nowTick;
        }

        AddEvent(severity, category, message);
    }

    private void SetAlertState(
        string key,
        bool active,
        DiagnosticsSeverity activeSeverity,
        DiagnosticsCategory category,
        string activeMessage,
        string resolvedMessage,
        int throttleMs = 3000)
    {
        bool shouldEmitResolved;
        lock (_stateLock)
        {
            var wasActive = _activeAlerts.Contains(key);
            if (active)
            {
                _activeAlerts.Add(key);
                shouldEmitResolved = false;
            }
            else
            {
                shouldEmitResolved = wasActive;
                _activeAlerts.Remove(key);
                _eventThrottleTicks.Remove(key);
            }
        }

        if (active)
        {
            AddEventThrottled(key, activeSeverity, category, activeMessage, throttleMs);
            return;
        }

        if (shouldEmitResolved)
        {
            AddEvent(DiagnosticsSeverity.Info, category, resolvedMessage);
        }
    }

    private void AddEvent(DiagnosticsSeverity severity, DiagnosticsCategory category, string message, string? correlationId = null)
    {
        var evt = new DiagnosticsEvent
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Severity = severity,
            Category = category,
            Message = message,
            CorrelationId = correlationId
        };

        lock (_stateLock)
        {
            _recentEvents.Add(evt);
            if (_recentEvents.Count > MaxRecentEvents)
            {
                _recentEvents.RemoveRange(0, _recentEvents.Count - MaxRecentEvents);
            }
        }
    }

    public IReadOnlyList<DiagnosticsEvent> GetRecentEvents(int maxEvents = 100)
    {
        lock (_stateLock)
        {
            var take = Math.Clamp(maxEvents, 1, MaxRecentEvents);
            if (_recentEvents.Count <= take)
            {
                return _recentEvents.ToArray();
            }

            return _recentEvents.GetRange(_recentEvents.Count - take, take).ToArray();
        }
    }
}
