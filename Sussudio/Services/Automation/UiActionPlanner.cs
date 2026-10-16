using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sussudio.Services.Automation;

/// <summary>Names of the UI Automation patterns the bridge reports, matching <c>PatternInterface</c> member names.</summary>
public static class UiPatterns
{
    public const string Invoke = "Invoke";
    public const string Toggle = "Toggle";
    public const string Value = "Value";
    public const string RangeValue = "RangeValue";
    public const string Selection = "Selection";
    public const string SelectionItem = "SelectionItem";
    public const string ExpandCollapse = "ExpandCollapse";
    public const string ScrollItem = "ScrollItem";
}

/// <summary>The actions InvokeUiElement accepts. <c>accelerator</c> is deliberately absent (see UiActionPlanner).</summary>
public static class UiActions
{
    public const string Invoke = "invoke";
    public const string Toggle = "toggle";
    public const string SetValue = "set-value";
    public const string Select = "select";
    public const string Expand = "expand";
    public const string Collapse = "collapse";
    public const string Focus = "focus";
    public const string ScrollIntoView = "scroll-into-view";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Invoke, Toggle, SetValue, Select, Expand, Collapse, Focus, ScrollIntoView,
    };
}

/// <summary>What the automation peer of the resolved element reported, as far as action planning needs it.</summary>
public sealed record UiElementFacts(
    IReadOnlyCollection<string> Patterns,
    bool ValueReadOnly,
    double? RangeMinimum,
    double? RangeMaximum,
    bool RangeReadOnly,
    string? ExpandState);

/// <summary>A validated action ready for the bridge to perform, or the reason it was rejected before touching the UI.</summary>
public sealed record UiActionPlan(
    bool Ok,
    string? Action,
    string? ErrorStatus,
    string? Error,
    double? NumericValue,
    string? TextValue,
    string? SelectionValue)
{
    internal static UiActionPlan Reject(string status, string error)
        => new(false, null, status, error, null, null, null);

    internal static UiActionPlan Accept(string action, double? numeric = null, string? text = null, string? selection = null)
        => new(true, action, null, null, numeric, text, selection);
}

/// <summary>
/// Validates (action, value) against what the element supports before anything runs on the UI thread, so the agent gets
/// an error that names what the element does support instead of an exception from a pattern provider. There is no
/// <c>accelerator</c> action: WinUI exposes no public way to raise a KeyboardAccelerator, so the tree lists accelerators
/// for information and the agent invokes the element that owns the accelerator instead.
/// </summary>
public static class UiActionPlanner
{
    public static UiActionPlan Plan(string? requestedAction, string? value, UiElementFacts facts)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value);
        string action;
        if (string.IsNullOrWhiteSpace(requestedAction))
        {
            var natural = NaturalAction(facts, hasValue);
            if (natural is null)
            {
                return UiActionPlan.Reject(
                    UiActionStatus.InvalidAction,
                    $"This element has no natural action; pass an action. It supports: {string.Join(", ", SupportedActions(facts))}.");
            }

            action = natural;
        }
        else
        {
            action = requestedAction.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            if (action is "accelerator" or "accelerators")
            {
                return UiActionPlan.Reject(
                    UiActionStatus.InvalidAction,
                    "Action 'accelerator' is not supported: WinUI has no public API to raise a KeyboardAccelerator. "
                    + "Invoke the element that owns the accelerator instead (ui tree lists each element's Accelerators).");
            }

            if (!UiActions.All.Contains(action))
            {
                return UiActionPlan.Reject(
                    UiActionStatus.InvalidAction,
                    $"Unknown action '{requestedAction.Trim()}'. Valid actions: {string.Join(", ", UiActions.All)}.");
            }
        }

        return action switch
        {
            UiActions.Invoke => PlanSimple(action, hasValue, facts, UiPatterns.Invoke),
            UiActions.Toggle => PlanSimple(action, hasValue, facts, UiPatterns.Toggle),
            UiActions.Expand or UiActions.Collapse => PlanExpandCollapse(action, hasValue, facts),
            UiActions.SetValue => PlanSetValue(value, hasValue, facts),
            UiActions.Select => PlanSelect(value, hasValue, facts),
            _ => PlanUnconditional(action, hasValue),
        };
    }

    /// <summary>The actions that would be accepted for the element (set-value is omitted for a read-only value).</summary>
    public static IReadOnlyList<string> SupportedActions(UiElementFacts facts)
    {
        var supported = new List<string>(UiActions.All.Count);
        if (Has(facts, UiPatterns.Invoke))
        {
            supported.Add(UiActions.Invoke);
        }

        if (Has(facts, UiPatterns.Toggle))
        {
            supported.Add(UiActions.Toggle);
        }

        if (Settable(facts))
        {
            supported.Add(UiActions.SetValue);
        }

        if (Has(facts, UiPatterns.SelectionItem) || Has(facts, UiPatterns.Selection))
        {
            supported.Add(UiActions.Select);
        }

        if (Has(facts, UiPatterns.ExpandCollapse) && !IsLeaf(facts))
        {
            supported.Add(UiActions.Expand);
            supported.Add(UiActions.Collapse);
        }

        supported.Add(UiActions.Focus);
        supported.Add(UiActions.ScrollIntoView);
        return supported;
    }

    private static string? NaturalAction(UiElementFacts facts, bool hasValue)
    {
        if (hasValue)
        {
            if (Settable(facts))
            {
                return UiActions.SetValue;
            }

            if (Has(facts, UiPatterns.Selection))
            {
                return UiActions.Select;
            }
        }

        if (Has(facts, UiPatterns.Invoke))
        {
            return UiActions.Invoke;
        }

        if (Has(facts, UiPatterns.Toggle))
        {
            return UiActions.Toggle;
        }

        if (Has(facts, UiPatterns.SelectionItem))
        {
            return UiActions.Select;
        }

        return null;
    }

    private static UiActionPlan PlanSimple(string action, bool hasValue, UiElementFacts facts, string pattern)
    {
        if (!Has(facts, pattern))
        {
            return Unsupported(action, facts);
        }

        return hasValue ? TakesNoValue(action) : UiActionPlan.Accept(action);
    }

    private static UiActionPlan PlanExpandCollapse(string action, bool hasValue, UiElementFacts facts)
    {
        if (!Has(facts, UiPatterns.ExpandCollapse))
        {
            return Unsupported(action, facts);
        }

        if (IsLeaf(facts))
        {
            return UiActionPlan.Reject(UiActionStatus.ActionUnsupported, $"Cannot {action}: the element is a leaf node (it has nothing to expand).");
        }

        return hasValue ? TakesNoValue(action) : UiActionPlan.Accept(action);
    }

    private static UiActionPlan PlanSetValue(string? value, bool hasValue, UiElementFacts facts)
    {
        var range = Has(facts, UiPatterns.RangeValue);
        if (!range && !Has(facts, UiPatterns.Value))
        {
            return Unsupported(UiActions.SetValue, facts);
        }

        if (!hasValue)
        {
            return UiActionPlan.Reject(UiActionStatus.InvalidValue, "Action 'set-value' needs a value.");
        }

        if (range)
        {
            if (facts.RangeReadOnly)
            {
                return UiActionPlan.Reject(UiActionStatus.InvalidValue, "The element's value is read-only.");
            }

            if (!double.TryParse(value!.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
            {
                return UiActionPlan.Reject(UiActionStatus.InvalidValue, $"'{value.Trim()}' is not a finite number (use a plain number such as 37.5).");
            }

            if ((facts.RangeMinimum is { } minimum && number < minimum) || (facts.RangeMaximum is { } maximum && number > maximum))
            {
                return UiActionPlan.Reject(
                    UiActionStatus.InvalidValue,
                    $"{number.ToString(CultureInfo.InvariantCulture)} is outside the element's range {FormatBound(facts.RangeMinimum)} to {FormatBound(facts.RangeMaximum)} (values are rejected, never clamped).");
            }

            return UiActionPlan.Accept(UiActions.SetValue, numeric: number);
        }

        if (facts.ValueReadOnly)
        {
            return UiActionPlan.Reject(UiActionStatus.InvalidValue, "The element's value is read-only.");
        }

        return UiActionPlan.Accept(UiActions.SetValue, text: value);
    }

    private static UiActionPlan PlanSelect(string? value, bool hasValue, UiElementFacts facts)
    {
        var isItem = Has(facts, UiPatterns.SelectionItem);
        if (!isItem && !Has(facts, UiPatterns.Selection))
        {
            return Unsupported(UiActions.Select, facts);
        }

        if (isItem)
        {
            return hasValue
                ? UiActionPlan.Reject(UiActionStatus.InvalidValue, "Action 'select' on an item takes no value (the item itself is selected).")
                : UiActionPlan.Accept(UiActions.Select);
        }

        return hasValue
            ? UiActionPlan.Accept(UiActions.Select, selection: value!.Trim())
            : UiActionPlan.Reject(UiActionStatus.InvalidValue, "Action 'select' on a list needs a value: the item text, or #N for a zero-based index.");
    }

    private static UiActionPlan PlanUnconditional(string action, bool hasValue)
        => hasValue ? TakesNoValue(action) : UiActionPlan.Accept(action);

    private static UiActionPlan TakesNoValue(string action)
        => UiActionPlan.Reject(UiActionStatus.InvalidValue, $"Action '{action}' takes no value.");

    private static UiActionPlan Unsupported(string action, UiElementFacts facts)
        => UiActionPlan.Reject(
            UiActionStatus.ActionUnsupported,
            $"The element does not support '{action}'. It supports: {string.Join(", ", SupportedActions(facts))}.");

    // A range or text value the peer accepts writes to; a read-only one (a ProgressBar, a read-only ComboBox text) is not.
    private static bool Settable(UiElementFacts facts)
        => (Has(facts, UiPatterns.RangeValue) && !facts.RangeReadOnly) || (Has(facts, UiPatterns.Value) && !facts.ValueReadOnly);

    private static bool Has(UiElementFacts facts, string pattern)
        => facts.Patterns.Contains(pattern);

    private static bool IsLeaf(UiElementFacts facts)
        => string.Equals(facts.ExpandState, "LeafNode", StringComparison.Ordinal);

    private static string FormatBound(double? bound)
        => bound?.ToString(CultureInfo.InvariantCulture) ?? "unbounded";
}
