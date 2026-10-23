using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sussudio.Models;

// Wire DTOs of the UI automation commands (GetUiTree, InvokeUiElement). Dependency-free on purpose: the live WinUI walk
// lives in Sussudio.Controllers.UiAutomationBridge, and everything that can be decided without a visual tree (target
// resolution, action planning, visibility rules) is unit-tested from these plain types. Null members are omitted from the
// JSON so a few hundred nodes stay readable. Contract: docs/architecture/performance-telemetry.md section 2.10.

/// <summary>Element bounds in window DIPs (effective pixels, origin at the top-left of the window content).</summary>
public sealed record UiBounds(double X, double Y, double Width, double Height);

/// <summary>A KeyboardAccelerator declared on an element. Reported only: there is no public API to raise one.</summary>
public sealed record UiAccelerator(string Key, string Modifiers);

/// <summary>Range-value pattern state (sliders, number boxes).</summary>
public sealed record UiRange(double Value, double Minimum, double Maximum, bool IsReadOnly);

/// <summary>One element of the live visual tree with the state an agent needs to decide how to operate it.</summary>
public sealed record UiNode(
    string Path,
    string Type,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AutomationId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AutomationName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Text,
    bool IsEnabled,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DisabledReason,
    bool IsVisible,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? HiddenReason,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UiBounds? Bounds,
    bool Interactive,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Patterns,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ToggleState,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UiRange? Range,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Value,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? ValueReadOnly,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SelectedItem,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? SelectedIndex,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Items,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? ItemCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExpandState,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<UiAccelerator>? Accelerators,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<UiNode>? Children);

public sealed record UiTreeRequest(string? Root, int MaxDepth, bool InteractiveOnly);

public static class UiTreeStatus
{
    public const string Ok = "ok";
    public const string RootNotFound = "root-not-found";
    public const string RootAmbiguous = "root-ambiguous";
    public const string WindowUnavailable = "window-unavailable";
}

/// <summary>
/// Response of GetUiTree. <see cref="Truncated"/> is true when the node cap (or the walk cap) stopped the output;
/// <see cref="TruncationReason"/> says which, so a partial tree is never mistaken for the whole UI.
/// </summary>
public sealed record UiTreeResult(
    string Status,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Root,
    int MaxDepth,
    bool InteractiveOnly,
    int VisitedElements,
    int EmittedNodes,
    int MaxNodes,
    bool Truncated,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TruncationReason,
    int OpenPopups,
    double WindowWidth,
    double WindowHeight,
    IReadOnlyList<UiNode> Roots)
{
    public bool Succeeded => Status == UiTreeStatus.Ok;
}

public sealed record UiInvokeRequest(string Target, string? Action, string? Value, bool Force);

public static class UiActionStatus
{
    public const string Invoked = "invoked";
    public const string NotFound = "not-found";
    public const string Ambiguous = "ambiguous";
    public const string Hidden = "hidden";
    public const string Disabled = "disabled";
    public const string InvalidAction = "invalid-action";
    public const string ActionUnsupported = "action-unsupported";
    public const string InvalidValue = "invalid-value";
    public const string ActionFailed = "action-failed";
    public const string WindowUnavailable = "window-unavailable";
}

/// <summary>A listed element that a target matched, or nearly matched, with the path that selects exactly it.</summary>
public sealed record UiCandidateInfo(
    string Path,
    string Type,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AutomationId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? AutomationName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Text,
    bool IsVisible,
    bool IsEnabled);

/// <summary>
/// Response of InvokeUiElement. <see cref="Status"/> is <c>invoked</c> only when the automation peer accepted the action;
/// <see cref="Element"/> is the element's state re-read after the action settled (also returned for rejected hidden or
/// disabled elements so the agent can see why). <see cref="Via"/> names the exact mechanism used.
/// </summary>
public sealed record UiInvokeResult(
    string Status,
    string Message,
    string Target,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? MatchedBy,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Action,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Via,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Overridden,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] UiNode? Element,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<UiCandidateInfo>? Candidates,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? CandidateTotal)
{
    public bool Succeeded => Status == UiActionStatus.Invoked;
}
