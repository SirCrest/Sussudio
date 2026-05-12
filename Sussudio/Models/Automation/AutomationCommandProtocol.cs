using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sussudio.Models;

// Wire-format response status. The converter uses SnakeCaseLower so the
// on-the-wire spelling stays "ok"/"error"/"not_ready" — every consumer
// (ssctl, MCP tools, test fixtures) reads the JSON via JsonElement walking,
// so wire stability is the contract that matters.
[JsonConverter(typeof(SnakeCaseLowerStatusConverter))]
public enum AutomationResponseStatus
{
    Ok,
    Error,
    NotReady,
}

// Wire-format command lifecycle. SnakeCaseLower preserves "completed",
// "failed", "acknowledged" exactly — the values happen to also be
// snake_case-stable.
[JsonConverter(typeof(SnakeCaseLowerLifecycleConverter))]
public enum AutomationCommandLifecycle
{
    Completed,
    Failed,
    Acknowledged,
}

internal sealed class SnakeCaseLowerStatusConverter : JsonStringEnumConverter<AutomationResponseStatus>
{
    public SnakeCaseLowerStatusConverter()
        : base(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false)
    {
    }
}

internal sealed class SnakeCaseLowerLifecycleConverter : JsonStringEnumConverter<AutomationCommandLifecycle>
{
    public SnakeCaseLowerLifecycleConverter()
        : base(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false)
    {
    }
}

// Flashback actions exposed through automation. They describe timeline intent;
// the playback controller decides whether that intent is valid for the current
// Live/Scrub/Play/Pause state.
public enum AutomationFlashbackAction
{
    Play,
    Pause,
    GoLive,
    Seek,
    BeginScrub,
    UpdateScrub,
    EndScrub,
    SetInPoint,
    SetOutPoint,
    ClearInOutPoints
}

public enum AutomationWindowAction
{
    Minimize,
    Maximize,
    Restore,
    Close,
    SnapLeft,
    SnapRight,
    SnapTopLeft,
    SnapTopRight,
    SnapBottomLeft,
    SnapBottomRight,
    Center,
    Move,
    Resize
}

// Conditions that automation waits can poll against the latest snapshot. These
// names are part of the ssctl/MCP surface, so prefer adding a new condition over
// changing existing semantics.
public enum AutomationWaitCondition
{
    PreviewFramesActive,
    PreviewRendererHealthy,
    AudioSignalPresent,
    RecordingFileGrowing,
    RecordingStopped,
    VerificationReady,
    HdrModeApplied,
    PerformancePerfectionMet,
    HdrVerificationReady,
    AudioFramesFlowing,
    VideoFramesFlowing
}

// Wire-format request for the named-pipe automation server. Payload stays as a
// JsonElement so each command can validate only the fields it actually needs.
// ManifestRevision is the client's view of the AutomationCommandKind numeric
// ID table; the server rejects mismatched revisions before dispatching to keep
// stale ssctl/MCP/StreamDeck binaries from silently misrouting commands.
public sealed class AutomationCommandRequest
{
    public AutomationCommandKind Command { get; init; }
    public string? CorrelationId { get; init; }
    public string? AuthToken { get; init; }
    public int? ManifestRevision { get; init; }
    public JsonElement Payload { get; init; }
}

// Wire-format response shared by automation clients, ssctl, and MCP tools.
// Snapshot is optional so cheap acknowledgement commands do not have to force a
// full diagnostics refresh unless the caller needs it.
public sealed class AutomationCommandResponse
{
    public bool Success { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public AutomationResponseStatus Status { get; init; } = AutomationResponseStatus.Ok;
    public AutomationCommandLifecycle CommandLifecycle { get; init; } = AutomationCommandLifecycle.Completed;
    public int? RetryAfterMs { get; init; }
    public long? ElapsedMs { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public object? Data { get; init; }
    public AutomationSnapshot? Snapshot { get; init; }
}

public sealed class SnapshotAssertion
{
    public string Field { get; init; } = string.Empty;
    public string Op { get; init; } = "eq";
    public string? Value { get; init; }
}
