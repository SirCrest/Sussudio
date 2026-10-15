using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Contracts;
using Sussudio.Services.Performance;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

// Performance-telemetry commands (GetPerfReport, PerfWindow, AddPerfMarker, FrameTrace, GetTuningKnobs, SetTuningKnob,
// GetLogTail). They read and control process-wide telemetry state in Sussudio.Services.Performance and never touch
// the view model, so they need no ready devices and return no automation snapshot. Contract:
// docs/architecture/performance-telemetry.md section 3.
public sealed partial class AutomationCommandDispatcher
{
    private const int DefaultLogTailEntries = 200;
    private const int MaxLogTailCountRows = 50;

    private static readonly string[] PerfWindowActions = { "begin", "read", "end", "cancel", "list" };
    private static readonly string[] FrameTraceActions = { "start", "stop", "status", "dump" };

    private AutomationCommandResponse ExecuteGetPerfReportCommand(JsonElement payload, string correlationId)
    {
        var window = GetString(payload, AutomationPayloadKeys.Window);
        var filter = GetString(payload, AutomationPayloadKeys.Filter);
        var includeIdle = GetBool(payload, AutomationPayloadKeys.IncludeIdle) ?? false;

        if (string.IsNullOrWhiteSpace(window))
        {
            var lifetime = PerfReportBuilder.BuildLifetime(filter, includeIdle);
            return CreatePerfResponse(correlationId, "Performance report retrieved (since process start).", lifetime);
        }

        var reading = PerfWindows.Set.Read(window.Trim())
            ?? throw new AutomationRequestValidationException(UnknownPerfWindowMessage(window));
        return CreatePerfResponse(
            correlationId,
            $"Performance report retrieved (window '{reading.Name}').",
            PerfReportBuilder.BuildWindow(reading, filter, includeIdle));
    }

    private AutomationCommandResponse ExecutePerfWindowCommand(JsonElement payload, string correlationId)
    {
        var action = RequireActionToken(payload, PerfWindowActions);
        if (action == "list")
        {
            var windows = PerfWindows.Set.List();
            return CreatePerfResponse(
                correlationId,
                windows.Count == 0 ? "No performance windows are open." : $"{windows.Count} performance window(s) open.",
                new PerfWindowListResponse(windows.Count, PerfWindowSet<PerfSystemSample>.MaxWindows, windows));
        }

        var name = RequireString(payload, AutomationPayloadKeys.Name).Trim();
        switch (action)
        {
            case "begin":
            {
                var result = PerfWindows.Set.Begin(name);
                switch (result.Outcome)
                {
                    case PerfWindowBeginOutcome.InvalidName:
                        throw new AutomationRequestValidationException(
                            $"Invalid performance window name. Use 1 to {PerfWindowSet<PerfSystemSample>.MaxNameLength} characters with no control characters.");
                    case PerfWindowBeginOutcome.LimitReached:
                        throw new AutomationRequestValidationException(
                            $"At most {PerfWindowSet<PerfSystemSample>.MaxWindows} performance windows can be open. End or cancel one first (perf window list).");
                }

                var restarted = result.Outcome == PerfWindowBeginOutcome.Restarted;
                return CreatePerfResponse(
                    correlationId,
                    restarted ? $"Performance window '{name}' restarted." : $"Performance window '{name}' started.",
                    new PerfWindowAckResponse(action, name, restarted ? "restarted" : "started", result.ActiveCount));
            }

            case "read":
            {
                var reading = PerfWindows.Set.Read(name)
                    ?? throw new AutomationRequestValidationException(UnknownPerfWindowMessage(name));
                return CreatePerfResponse(
                    correlationId,
                    $"Performance window '{name}' read.",
                    PerfReportBuilder.BuildWindow(reading, filter: null, includeIdle: false));
            }

            case "end":
            {
                var reading = PerfWindows.Set.End(name)
                    ?? throw new AutomationRequestValidationException(UnknownPerfWindowMessage(name));
                return CreatePerfResponse(
                    correlationId,
                    $"Performance window '{name}' ended.",
                    PerfReportBuilder.BuildWindow(reading, filter: null, includeIdle: false));
            }

            default:
            {
                if (!PerfWindows.Set.Cancel(name))
                {
                    throw new AutomationRequestValidationException(UnknownPerfWindowMessage(name));
                }

                return CreatePerfResponse(
                    correlationId,
                    $"Performance window '{name}' canceled.",
                    new PerfWindowAckResponse(action, name, "canceled", PerfWindows.Set.List().Count));
            }
        }
    }

    private AutomationCommandResponse ExecuteAddPerfMarkerCommand(JsonElement payload, string correlationId)
    {
        var text = RequireString(payload, AutomationPayloadKeys.Text).Trim();
        var truncated = text.Length > PerfMarkerLog.MaxTextLength;
        var marker = PerfMarkers.Add(text);
        return CreatePerfResponse(
            correlationId,
            "Performance marker added.",
            new PerfMarkerResponse(marker.TimestampUtc.ToString("O", CultureInfo.InvariantCulture), marker.Text, truncated));
    }

    private async Task<AutomationCommandResponse> ExecuteFrameTraceCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var action = RequireActionToken(payload, FrameTraceActions);
        var recorder = FrameTrace.Recorder;
        switch (action)
        {
            case "start":
            {
                var capacity = GetInt(payload, AutomationPayloadKeys.Capacity);
                if (capacity is < 1)
                {
                    throw new AutomationRequestValidationException("Frame trace capacity must be a positive number of events.");
                }

                var started = recorder.Start(capacity ?? 0);
                if (!started.Success)
                {
                    throw new AutomationStateConflictException(started.Error ?? "Frame trace could not be started.");
                }

                return CreatePerfResponse(
                    correlationId,
                    $"Frame trace started (capacity {started.Capacity} events).",
                    new FrameTraceResponse(action, recorder.GetStatus(), Path: null, Bytes: null));
            }

            case "stop":
            {
                var status = recorder.Stop();
                return CreatePerfResponse(
                    correlationId,
                    status.HasBuffer
                        ? $"Frame trace stopped ({status.Retained} events retained, {status.Overwritten} overwritten)."
                        : "Frame trace was not running.",
                    new FrameTraceResponse(action, status, Path: null, Bytes: null));
            }

            case "status":
                return CreatePerfResponse(
                    correlationId,
                    "Frame trace status retrieved.",
                    new FrameTraceResponse(action, recorder.GetStatus(), Path: null, Bytes: null));

            default:
            {
                var requested = ValidatePathPayload(
                    AutomationCommandKind.FrameTrace,
                    AutomationPayloadKeys.OutputPath,
                    GetString(payload, AutomationPayloadKeys.OutputPath)
                        ?? Path.Combine(
                            Path.GetTempPath(),
                            $"frame_trace_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.json"));
                var outputPath = Path.GetFullPath(requested);
                if (!recorder.GetStatus().HasBuffer)
                {
                    throw new AutomationStateConflictException("No frame trace has been recorded. Run perf trace start first.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                var (bytes, error) = await Task.Run(() => WriteFrameTrace(recorder, outputPath), CancellationToken.None).ConfigureAwait(false);
                if (error is not null)
                {
                    throw new AutomationStateConflictException(error);
                }

                return CreatePerfResponse(
                    correlationId,
                    $"Frame trace written to {outputPath} ({bytes} bytes).",
                    new FrameTraceResponse(action, recorder.GetStatus(), outputPath, bytes));
            }
        }
    }

    private AutomationCommandResponse ExecuteGetTuningKnobsCommand(JsonElement payload, string correlationId)
    {
        var filter = GetString(payload, AutomationPayloadKeys.Filter);
        var states = TuningKnobs.List(filter);
        var excluded = string.IsNullOrWhiteSpace(filter)
            ? TuningKnobCatalog.Excluded
            : TuningKnobCatalog.Excluded
                .Where(exclusion => exclusion.Name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        var knobs = states.Select(ToKnobResponse).ToList();
        return CreatePerfResponse(
            correlationId,
            $"{knobs.Count} tuning knob(s) listed.",
            new TuningKnobListResponse(knobs.Count, knobs.Count(knob => knob.Overridden), knobs, excluded));
    }

    private AutomationCommandResponse ExecuteSetTuningKnobCommand(JsonElement payload, string correlationId)
    {
        var name = RequireString(payload, AutomationPayloadKeys.Name).Trim();
        var reset = GetBool(payload, AutomationPayloadKeys.Reset) ?? false;
        var value = GetString(payload, AutomationPayloadKeys.Value);
        if (reset && !string.IsNullOrWhiteSpace(value))
        {
            throw new AutomationRequestValidationException("Specify either a value or reset, not both.");
        }

        var result = reset ? TuningKnobs.Reset(name) : TuningKnobs.Set(name, value);
        var response = new TuningKnobChangeResponse(
            result.Success,
            result.Name,
            result.Success ? null : result.Failure.ToString(),
            result.Error,
            result.Old,
            result.New,
            result.Apply,
            result.Note);
        if (!result.Success)
        {
            return CreateResponse(
                correlationId,
                result.Error ?? "Tuning knob change rejected.",
                data: response,
                errorCode: AutomationErrorCodes.InvalidRequest,
                success: false,
                status: AutomationResponseStatus.Error,
                includeSnapshot: false);
        }

        return CreatePerfResponse(
            correlationId,
            $"Tuning knob {result.Name} {(reset ? "reset" : "set")}: {result.Old ?? "<unset>"} -> {result.New ?? "<unset>"}. {result.Note}",
            response);
    }

    private AutomationCommandResponse ExecuteGetLogTailCommand(JsonElement payload, string correlationId)
    {
        var maxEntries = GetInt(payload, AutomationPayloadKeys.MaxEntries) ?? DefaultLogTailEntries;
        if (maxEntries < 1)
        {
            throw new AutomationRequestValidationException("maxEntries must be at least 1.");
        }

        var contains = GetString(payload, AutomationPayloadKeys.Contains);
        var lines = LogTap.Tail(maxEntries, contains);
        var counts = LogTap.Counts();
        return CreatePerfResponse(
            correlationId,
            $"{lines.Count} log line(s) retrieved.",
            new LogTailResponse(
                counts.TotalLines,
                lines.Count,
                lines,
                counts.ByCaller.Take(MaxLogTailCountRows).ToList(),
                counts.ByTag.Take(MaxLogTailCountRows).ToList()));
    }

    private AutomationCommandResponse CreatePerfResponse(string correlationId, string message, object data)
        => CreateResponse(correlationId, message, data: data, includeSnapshot: false);

    private static (long Bytes, string? Error) WriteFrameTrace(FrameTraceRecorder recorder, string outputPath)
    {
        try
        {
            long bytes;
            string? error;
            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var written = recorder.TryWriteChromeTrace(stream, out error);
                stream.Flush();
                bytes = stream.Length;
                if (written)
                {
                    return (bytes, null);
                }
            }

            TryDeletePartialTrace(outputPath);
            return (0, error ?? "Frame trace could not be written.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDeletePartialTrace(outputPath);
            return (0, $"Frame trace could not be written to '{outputPath}': {ex.Message}");
        }
    }

    private static void TryDeletePartialTrace(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A trace that failed to write is reported to the caller; a leftover empty file is harmless.
        }
    }

    private static string RequireActionToken(JsonElement payload, string[] allowed)
    {
        var raw = RequireString(payload, AutomationPayloadKeys.Action).Trim().ToLowerInvariant();
        if (Array.IndexOf(allowed, raw) < 0)
        {
            throw new AutomationRequestValidationException($"Invalid action '{raw}'. Expected {string.Join(", ", allowed)}.");
        }

        return raw;
    }

    private static string UnknownPerfWindowMessage(string name)
    {
        var open = PerfWindows.Set.List();
        return open.Count == 0
            ? $"Unknown performance window '{name}'. No windows are open; start one with perf window begin."
            : $"Unknown performance window '{name}'. Open windows: {string.Join(", ", open.Select(window => window.Name))}.";
    }

    private static TuningKnobInfoResponse ToKnobResponse(TuningKnobState state)
    {
        var definition = state.Definition;
        return new TuningKnobInfoResponse(
            definition.Name,
            definition.Type.ToString().ToLowerInvariant(),
            definition.Default,
            definition.Min,
            definition.Max,
            definition.Choices,
            TuningKnobApplyNames.ToWire(definition.Apply),
            definition.Owner,
            definition.Description,
            state.Value,
            state.Launch,
            state.Overridden);
    }
}

internal sealed record PerfWindowAckResponse(string Action, string Name, string Outcome, int ActiveWindows);

internal sealed record PerfWindowListResponse(int Count, int MaxWindows, IReadOnlyList<PerfWindowInfo> Windows);

internal sealed record PerfMarkerResponse(string TimestampUtc, string Text, bool Truncated);

internal sealed record FrameTraceResponse(string Action, FrameTraceStatus Status, string? Path, long? Bytes);

internal sealed record TuningKnobInfoResponse(
    string Name,
    string Type,
    string Default,
    double? Min,
    double? Max,
    IReadOnlyList<string>? Choices,
    string Apply,
    string Owner,
    string Description,
    string? Value,
    string? Launch,
    bool Overridden);

internal sealed record TuningKnobListResponse(
    int Count,
    int Overridden,
    IReadOnlyList<TuningKnobInfoResponse> Knobs,
    IReadOnlyList<TuningKnobExclusion> Excluded);

internal sealed record TuningKnobChangeResponse(
    bool Success,
    string Name,
    string? Failure,
    string? Error,
    string? Old,
    string? New,
    string? Apply,
    string? Note);

internal sealed record LogTailResponse(
    long TotalLines,
    int Returned,
    IReadOnlyList<string> Lines,
    IReadOnlyList<LogTapCount> ByCaller,
    IReadOnlyList<LogTapCount> ByTag);
