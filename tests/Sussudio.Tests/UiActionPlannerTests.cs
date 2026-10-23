using System;
using Sussudio.Models;
using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of turning (action, value) into one automation-peer call for a resolved element
// (docs/architecture/performance-telemetry.md section 2.10). The peer call is the only thing that runs on the UI thread,
// so everything that can be rejected from the element's facts must be rejected before it, with a message that tells the
// agent what the element does support:
//
//   1. An unknown action must list the valid ones; "accelerator" must say plainly that it is not supported (there is
//      no public API to raise a KeyboardAccelerator) rather than look like a typo.
//   2. An omitted action must resolve to the element's natural action (a value implies set-value), and an element with
//      no natural action must say so instead of guessing.
//   3. An action the element has no pattern for must be rejected naming what it does support.
//   4. set-value needs a value; a range element needs a finite number inside [minimum, maximum] (rejected, never
//      clamped); a read-only element, text or range (a ProgressBar), is rejected and not offered set-value, or the
//      action fails on the UI thread instead of naming the reason.
//   5. select on an item takes no value; select on a container needs one.
//   6. expand/collapse on a leaf node must be rejected; expand on an already-expanded element is allowed (a no-op).
//   7. A value passed to an action that takes none is a mistake and must be rejected; an empty value counts as absent.
//   8. focus and scroll-into-view need no pattern and must always be planned.
public sealed class UiActionPlannerTests
{
    private static UiElementFacts Facts(
        string[] patterns,
        bool valueReadOnly = false,
        double? min = null,
        double? max = null,
        string? expandState = null,
        bool rangeReadOnly = false)
        => new(patterns, valueReadOnly, min, max, rangeReadOnly, expandState);

    private static readonly UiElementFacts Button = Facts(new[] { UiPatterns.Invoke });
    private static readonly UiElementFacts ToggleSwitch = Facts(new[] { UiPatterns.Toggle });
    private static readonly UiElementFacts Slider = Facts(new[] { UiPatterns.RangeValue }, min: 0, max: 100);
    private static readonly UiElementFacts ProgressBar = Facts(new[] { UiPatterns.RangeValue }, min: 0, max: 100, rangeReadOnly: true);
    private static readonly UiElementFacts TextBox = Facts(new[] { UiPatterns.Value });
    private static readonly UiElementFacts ComboBox = Facts(new[] { UiPatterns.Selection, UiPatterns.ExpandCollapse, UiPatterns.Value }, valueReadOnly: true, expandState: "Collapsed");
    private static readonly UiElementFacts ListItem = Facts(new[] { UiPatterns.SelectionItem });
    private static readonly UiElementFacts Expander = Facts(new[] { UiPatterns.ExpandCollapse }, expandState: "Collapsed");
    private static readonly UiElementFacts Label = Facts(Array.Empty<string>());

    [Fact]
    public void Plan_UnknownActionListsTheValidOnes()
    {
        var plan = UiActionPlanner.Plan("smash", null, Button);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidAction, plan.ErrorStatus);
        foreach (var action in UiActions.All)
        {
            Assert.Contains(action, plan.Error);
        }
    }

    [Theory]
    [InlineData("accelerator")]
    [InlineData("Accelerators")]
    public void Plan_AcceleratorIsExplicitlyUnsupported(string action)
    {
        var plan = UiActionPlanner.Plan(action, null, Button);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidAction, plan.ErrorStatus);
        Assert.Contains("KeyboardAccelerator", plan.Error);
        Assert.Contains("not supported", plan.Error);
    }

    [Theory]
    [InlineData("INVOKE")]
    [InlineData(" invoke ")]
    public void Plan_NormalizesCaseAndWhitespace(string action)
    {
        var plan = UiActionPlanner.Plan(action, null, Button);

        Assert.True(plan.Ok);
        Assert.Equal(UiActions.Invoke, plan.Action);
    }

    [Theory]
    [InlineData("set_value")]
    [InlineData("Set-Value")]
    [InlineData("scroll_into_view")]
    public void Plan_AcceptsUnderscoreSpellings(string action)
    {
        var plan = UiActionPlanner.Plan(action, action.StartsWith("scroll", StringComparison.OrdinalIgnoreCase) ? null : "5", Slider);

        Assert.True(plan.Ok, plan.Error);
    }

    [Fact]
    public void Plan_OmittedActionUsesTheNaturalAction()
    {
        Assert.Equal(UiActions.Invoke, UiActionPlanner.Plan(null, null, Button).Action);
        Assert.Equal(UiActions.Toggle, UiActionPlanner.Plan("", null, ToggleSwitch).Action);
        Assert.Equal(UiActions.Select, UiActionPlanner.Plan(null, null, ListItem).Action);
    }

    [Fact]
    public void Plan_ValueWithoutActionImpliesSetValueOrSelect()
    {
        Assert.Equal(UiActions.SetValue, UiActionPlanner.Plan(null, "40", Slider).Action);
        Assert.Equal(UiActions.SetValue, UiActionPlanner.Plan(null, "hello", TextBox).Action);
        Assert.Equal(UiActions.Select, UiActionPlanner.Plan(null, "Option A", ComboBox).Action);
    }

    [Fact]
    public void Plan_ElementWithOnlyExpandCollapseNeedsAnExplicitAction()
    {
        var plan = UiActionPlanner.Plan(null, null, Expander);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidAction, plan.ErrorStatus);
        Assert.Contains("expand", plan.Error);
        Assert.Contains("collapse", plan.Error);
    }

    [Fact]
    public void Plan_ElementWithNoPatternsHasNoNaturalActionButNamesFocus()
    {
        var plan = UiActionPlanner.Plan(null, null, Label);

        Assert.False(plan.Ok);
        Assert.Contains("focus", plan.Error);
    }

    [Fact]
    public void Plan_ActionTheElementDoesNotSupportNamesWhatItDoes()
    {
        var plan = UiActionPlanner.Plan("toggle", null, Button);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.ActionUnsupported, plan.ErrorStatus);
        Assert.Contains("invoke", plan.Error);
        Assert.Contains("focus", plan.Error);
        Assert.DoesNotContain("toggle,", plan.Error);
    }

    [Theory]
    [InlineData("invoke")]
    [InlineData("select")]
    [InlineData("expand")]
    [InlineData("collapse")]
    [InlineData("set-value")]
    public void Plan_PatternActionsAreUnsupportedOnAPlainElement(string action)
    {
        var plan = UiActionPlanner.Plan(action, action == "set-value" || action == "select" ? "x" : null, Label);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.ActionUnsupported, plan.ErrorStatus);
    }

    [Fact]
    public void Plan_FocusAndScrollIntoViewNeedNoPattern()
    {
        Assert.True(UiActionPlanner.Plan("focus", null, Label).Ok);
        Assert.True(UiActionPlanner.Plan("scroll-into-view", null, Label).Ok);
    }

    [Fact]
    public void Plan_SetValueWithoutAValueIsRejected()
    {
        var plan = UiActionPlanner.Plan("set-value", null, Slider);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void Plan_RangeValueMustBeAFiniteNumber(string value)
    {
        var plan = UiActionPlanner.Plan("set-value", value, Slider);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("100.5")]
    public void Plan_RangeValueOutsideTheRangeIsRejectedNotClamped(string value)
    {
        var plan = UiActionPlanner.Plan("set-value", value, Slider);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
        Assert.Contains("0", plan.Error);
        Assert.Contains("100", plan.Error);
    }

    [Theory]
    [InlineData("0", 0d)]
    [InlineData("100", 100d)]
    [InlineData("37.5", 37.5d)]
    public void Plan_RangeValueInsideTheRangeIsPlannedWithTheParsedNumber(string value, double expected)
    {
        var plan = UiActionPlanner.Plan("set-value", value, Slider);

        Assert.True(plan.Ok, plan.Error);
        Assert.Equal(expected, plan.NumericValue);
        Assert.Null(plan.TextValue);
    }

    [Fact]
    public void Plan_RangeValueParsingIsCultureInvariant()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var plan = UiActionPlanner.Plan("set-value", "37.5", Slider);

            Assert.True(plan.Ok, plan.Error);
            Assert.Equal(37.5, plan.NumericValue);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Plan_ReadOnlyTextElementRejectsSetValue()
    {
        var readOnly = Facts(new[] { UiPatterns.Value }, valueReadOnly: true);

        var plan = UiActionPlanner.Plan("set-value", "hello", readOnly);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
        Assert.Contains("read-only", plan.Error);
    }

    [Fact]
    public void Plan_ReadOnlyRangeRejectsSetValueAndHasNoNaturalSetValue()
    {
        var plan = UiActionPlanner.Plan("set-value", "50", ProgressBar);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
        Assert.Contains("read-only", plan.Error);
        Assert.False(UiActionPlanner.Plan(null, "50", ProgressBar).Ok);
    }

    [Fact]
    public void Plan_TextElementPlansTheRawString()
    {
        var plan = UiActionPlanner.Plan("set-value", "  hello  ", TextBox);

        Assert.True(plan.Ok, plan.Error);
        Assert.Equal("  hello  ", plan.TextValue);
        Assert.Null(plan.NumericValue);
    }

    [Fact]
    public void Plan_SelectOnAnItemTakesNoValue()
    {
        var plan = UiActionPlanner.Plan("select", "Option A", ListItem);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
    }

    [Fact]
    public void Plan_SelectOnAContainerNeedsAValue()
    {
        var plan = UiActionPlanner.Plan("select", null, ComboBox);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
    }

    [Fact]
    public void Plan_SelectOnAContainerCarriesTheSelectionValue()
    {
        var plan = UiActionPlanner.Plan("select", "Option A", ComboBox);

        Assert.True(plan.Ok, plan.Error);
        Assert.Equal("Option A", plan.SelectionValue);
    }

    [Fact]
    public void Plan_ExpandOnALeafNodeIsRejected()
    {
        var leaf = Facts(new[] { UiPatterns.ExpandCollapse }, expandState: "LeafNode");

        var plan = UiActionPlanner.Plan("expand", null, leaf);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.ActionUnsupported, plan.ErrorStatus);
        Assert.Contains("leaf", plan.Error);
    }

    [Fact]
    public void Plan_ExpandOnAnAlreadyExpandedElementIsAllowed()
    {
        var expanded = Facts(new[] { UiPatterns.ExpandCollapse }, expandState: "Expanded");

        Assert.True(UiActionPlanner.Plan("expand", null, expanded).Ok);
        Assert.True(UiActionPlanner.Plan("collapse", null, expanded).Ok);
    }

    [Fact]
    public void Plan_ValueOnAnActionThatTakesNoneIsRejected()
    {
        var plan = UiActionPlanner.Plan("invoke", "oops", Button);

        Assert.False(plan.Ok);
        Assert.Equal(UiActionStatus.InvalidValue, plan.ErrorStatus);
        Assert.Contains("takes no value", plan.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Plan_EmptyValueCountsAsAbsent(string value)
    {
        Assert.True(UiActionPlanner.Plan("invoke", value, Button).Ok);
        Assert.True(UiActionPlanner.Plan("toggle", value, ToggleSwitch).Ok);
        Assert.Equal(UiActionStatus.InvalidValue, UiActionPlanner.Plan("set-value", value, Slider).ErrorStatus);
    }

    [Fact]
    public void SupportedActions_ReflectsTheElementPatterns()
    {
        Assert.Equal(
            new[] { UiActions.Invoke, UiActions.Focus, UiActions.ScrollIntoView },
            UiActionPlanner.SupportedActions(Button));
        Assert.Equal(
            new[] { UiActions.SetValue, UiActions.Focus, UiActions.ScrollIntoView },
            UiActionPlanner.SupportedActions(Slider));
        // The ComboBox's Value pattern is read-only, so set-value would be rejected and must not be advertised.
        Assert.Equal(
            new[] { UiActions.Select, UiActions.Expand, UiActions.Collapse, UiActions.Focus, UiActions.ScrollIntoView },
            UiActionPlanner.SupportedActions(ComboBox));
        // The same for a read-only range.
        Assert.Equal(
            new[] { UiActions.Focus, UiActions.ScrollIntoView },
            UiActionPlanner.SupportedActions(ProgressBar));
    }
}
