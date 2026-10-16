using System.Linq;
using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the two remaining pure decisions behind the UI automation commands
// (docs/architecture/performance-telemetry.md section 2.10).
//
// Choosing an item of a ComboBox/ListView by what the agent typed:
//   1. Text matches exactly, case-insensitively and trimmed, never by substring.
//   2. "#N" is an explicit zero-based index; a bare number is text, because item labels such as "60" are common and a
//      bare number silently changing meaning would select the wrong frame rate.
//   3. Duplicate item text is ambiguous and lists the indexes; no match lists the available items (capped);
//      an out-of-range index and an empty list are errors.
//
// Whether an element is really visible (a Visibility value alone says nothing about ancestors, opacity or layout):
//   4. A collapsed element, or any collapsed ancestor, is hidden, and the reason names the root-most culprit because
//      that is what the agent must change.
//   5. Opacity at or below 0.01 on the element or any ancestor is hidden (an invisible-but-clickable control is a trap).
//   6. A zero-sized element is hidden; a zero-sized ancestor alone is not (children can overflow it).
//   7. Dim but visible elements and ordinary chains stay visible with no reason.
public sealed class UiSelectionAndVisibilityTests
{
    private static readonly string[] FrameRates = { "24", "30", "60", "120" };

    [Fact]
    public void SelectionMatch_ExactTextIsCaseInsensitiveAndTrimmed()
    {
        var match = UiSelectionMatcher.Match(new[] { "Alpha", "Beta" }, "  BETA ");

        Assert.True(match.Ok, match.Error);
        Assert.Equal(1, match.Index);
    }

    [Fact]
    public void SelectionMatch_SubstringDoesNotMatch()
    {
        var match = UiSelectionMatcher.Match(new[] { "1920x1080", "3840x2160" }, "1920");

        Assert.False(match.Ok);
        Assert.Contains("1920x1080", match.Error);
    }

    [Fact]
    public void SelectionMatch_BareNumberIsTextNotAnIndex()
    {
        var match = UiSelectionMatcher.Match(FrameRates, "60");

        Assert.True(match.Ok, match.Error);
        Assert.Equal(2, match.Index);
    }

    [Fact]
    public void SelectionMatch_BareNumberWithNoSuchTextIsNotReadAsAnIndex()
    {
        var match = UiSelectionMatcher.Match(FrameRates, "1");

        Assert.False(match.Ok);
        Assert.Contains("#", match.Error);
    }

    [Fact]
    public void SelectionMatch_HashPrefixIsAZeroBasedIndex()
    {
        var match = UiSelectionMatcher.Match(FrameRates, "#1");

        Assert.True(match.Ok, match.Error);
        Assert.Equal(1, match.Index);
    }

    [Theory]
    [InlineData("#4")]
    [InlineData("#-1")]
    [InlineData("#x")]
    public void SelectionMatch_BadIndexIsRejected(string value)
    {
        var match = UiSelectionMatcher.Match(FrameRates, value);

        Assert.False(match.Ok);
        Assert.Contains("4", match.Error);
    }

    [Fact]
    public void SelectionMatch_DuplicateTextIsAmbiguousAndListsIndexes()
    {
        var match = UiSelectionMatcher.Match(new[] { "Same", "Other", "same" }, "Same");

        Assert.False(match.Ok);
        Assert.Contains("#0", match.Error);
        Assert.Contains("#2", match.Error);
    }

    [Fact]
    public void SelectionMatch_NoMatchListsAvailableItemsCapped()
    {
        var items = Enumerable.Range(0, 50).Select(index => $"Item {index}").ToArray();

        var match = UiSelectionMatcher.Match(items, "missing");

        Assert.False(match.Ok);
        Assert.Contains("Item 0", match.Error);
        Assert.DoesNotContain("Item 49", match.Error);
        Assert.Contains("50", match.Error);
    }

    [Fact]
    public void SelectionMatch_EmptyItemListIsAnError()
    {
        var match = UiSelectionMatcher.Match(System.Array.Empty<string>(), "anything");

        Assert.False(match.Ok);
        Assert.Contains("no items", match.Error);
    }

    private static UiVisibilityLayer Layer(string label, bool collapsed = false, double opacity = 1, double width = 100, double height = 30)
        => new(label, collapsed, opacity, width, height);

    [Fact]
    public void Visibility_OrdinaryChainIsVisible()
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("Grid"), Layer("StackPanel"), Layer("Button") });

        Assert.True(visibility.IsVisible);
        Assert.Null(visibility.Reason);
    }

    [Fact]
    public void Visibility_CollapsedElementIsHidden()
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("Grid"), Layer("Button", collapsed: true) });

        Assert.False(visibility.IsVisible);
        Assert.Contains("Collapsed", visibility.Reason);
    }

    [Fact]
    public void Visibility_CollapsedAncestorHidesTheElementAndTheReasonNamesTheRootMostCulprit()
    {
        var visibility = UiVisibilityRules.Evaluate(new[]
        {
            Layer("RootGrid"),
            Layer("SettingsPanel", collapsed: true),
            Layer("InnerPanel", collapsed: true),
            Layer("Button"),
        });

        Assert.False(visibility.IsVisible);
        Assert.Contains("SettingsPanel", visibility.Reason);
        Assert.DoesNotContain("InnerPanel", visibility.Reason);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.01d)]
    public void Visibility_NearZeroOpacityOnTheElementIsHidden(double opacity)
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("Grid"), Layer("Button", opacity: opacity) });

        Assert.False(visibility.IsVisible);
        Assert.Contains("Opacity", visibility.Reason);
    }

    [Fact]
    public void Visibility_ZeroOpacityAncestorHidesTheElement()
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("FadedPanel", opacity: 0), Layer("Button") });

        Assert.False(visibility.IsVisible);
        Assert.Contains("FadedPanel", visibility.Reason);
    }

    [Fact]
    public void Visibility_DimButVisibleElementStaysVisible()
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("Grid", opacity: 0.5), Layer("Button", opacity: 0.4) });

        Assert.True(visibility.IsVisible);
    }

    [Theory]
    [InlineData(0d, 30d)]
    [InlineData(100d, 0d)]
    public void Visibility_ZeroSizedElementIsHidden(double width, double height)
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("Grid"), Layer("Button", width: width, height: height) });

        Assert.False(visibility.IsVisible);
        Assert.Contains("zero size", visibility.Reason);
    }

    [Fact]
    public void Visibility_ZeroSizedAncestorAloneDoesNotHideAChildThatHasSize()
    {
        var visibility = UiVisibilityRules.Evaluate(new[] { Layer("Canvas", width: 0, height: 0), Layer("Button") });

        Assert.True(visibility.IsVisible);
    }

    [Fact]
    public void Visibility_EmptyChainIsNotVisible()
    {
        var visibility = UiVisibilityRules.Evaluate(System.Array.Empty<UiVisibilityLayer>());

        Assert.False(visibility.IsVisible);
    }
}
