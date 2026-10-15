using System.Linq;
using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of resolving an agent-supplied target to one live UI element
// (docs/architecture/performance-telemetry.md section 2.10). A wrong resolution clicks the wrong control in a recording
// app, so every way the lookup can be wrong has to be a visible rejection, never a guess:
//
//   1. An empty or whitespace target must be rejected before any search.
//   2. A target that matches nothing must say so, with near-miss suggestions (capped), not pick something close.
//   3. Name precedence is x:Name, then AutomationId, then AutomationProperties.Name, then visible text, and a
//      lower level must never be consulted when a higher level matched, even if that match is unusable.
//   4. x:Name and AutomationId are exact and case-sensitive; automation name and text are trimmed and case-insensitive;
//      text matches the whole string, never a substring.
//   5. Two usable elements matching at the winning level are ambiguous: the error lists them (capped, with the total)
//      and each listed path must resolve to exactly that element when passed back as the target.
//   6. A hidden twin must not block addressing the visible element, and a label inside a button must not make the
//      button ambiguous (hidden and non-interactive matches are dropped only when something better matched).
//   7. A single hidden element is rejected with the reason it is hidden, a single disabled one with the disabled
//      reason, and a hidden-and-disabled one reports both; force overrides the pre-check and says what it overrode.
//   8. Force must never pick among ambiguous elements.
public sealed class UiTargetResolverTests
{
    private static UiTargetCandidate Cand(
        string path,
        string type = "Button",
        string? name = null,
        string? id = null,
        string? automationName = null,
        string? text = null,
        bool enabled = true,
        bool visible = true,
        bool interactive = true,
        string? hiddenReason = null,
        string? disabledReason = null)
        => new(path, type, name, id, automationName, text, enabled, disabledReason, visible, hiddenReason, interactive);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_RejectsEmptyTarget(string target)
    {
        var resolution = UiTargetResolver.Resolve(target, new[] { Cand("w/Button[0]", name: "A") }, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
        Assert.Contains("target", resolution.Message, System.StringComparison.OrdinalIgnoreCase);
        Assert.Null(resolution.Element);
    }

    [Fact]
    public void Resolve_NoMatch_ReportsNotFoundWithSuggestionsAndPicksNothing()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", name: "RecordButton", text: "Record"),
            Cand("w/Button[1]", name: "ScreenshotButton", text: "Screenshot"),
        };

        var resolution = UiTargetResolver.Resolve("Recor", candidates, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
        Assert.Null(resolution.Element);
        Assert.Contains("Recor", resolution.Message);
        Assert.Contains(resolution.Candidates, candidate => candidate.Name == "RecordButton");
        Assert.DoesNotContain(resolution.Candidates, candidate => candidate.Name == "ScreenshotButton");
    }

    [Fact]
    public void Resolve_NoMatch_CapsSuggestions()
    {
        var candidates = Enumerable.Range(0, 40)
            .Select(index => Cand($"w/Button[{index}]", name: $"Item{index}"))
            .ToList();

        var resolution = UiTargetResolver.Resolve("Item", candidates, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
        Assert.Equal(UiTargetResolver.MaxListedCandidates, resolution.Candidates.Count);
        Assert.Equal(40, resolution.CandidateTotal);
    }

    [Fact]
    public void Resolve_MatchesXName()
    {
        var resolution = UiTargetResolver.Resolve("RecordButton", new[] { Cand("w/Button[0]", name: "RecordButton") }, force: false);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Equal("x:Name", resolution.MatchedBy);
        Assert.Equal("w/Button[0]", resolution.Element!.Path);
        Assert.Empty(resolution.Overridden);
    }

    [Fact]
    public void Resolve_XNameBeatsAutomationId()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", id: "Save"),
            Cand("w/Button[1]", name: "Save"),
        };

        var resolution = UiTargetResolver.Resolve("Save", candidates, force: false);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Equal("w/Button[1]", resolution.Element!.Path);
        Assert.Equal("x:Name", resolution.MatchedBy);
    }

    [Fact]
    public void Resolve_AutomationIdBeatsAutomationName()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", automationName: "Save"),
            Cand("w/Button[1]", id: "Save"),
        };

        var resolution = UiTargetResolver.Resolve("Save", candidates, force: false);

        Assert.Equal("w/Button[1]", resolution.Element!.Path);
        Assert.Equal("automation-id", resolution.MatchedBy);
    }

    [Fact]
    public void Resolve_AutomationNameBeatsVisibleText()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Save"),
            Cand("w/Button[1]", automationName: "Save"),
        };

        var resolution = UiTargetResolver.Resolve("Save", candidates, force: false);

        Assert.Equal("w/Button[1]", resolution.Element!.Path);
        Assert.Equal("automation-name", resolution.MatchedBy);
    }

    [Fact]
    public void Resolve_FallsBackToVisibleTextWhenNothingNamedMatches()
    {
        var resolution = UiTargetResolver.Resolve("Open folder", new[] { Cand("w/Button[0]", text: "Open folder") }, force: false);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Equal("text", resolution.MatchedBy);
    }

    [Fact]
    public void Resolve_UnusableHigherPrecedenceMatchDoesNotFallThroughToLowerLevel()
    {
        // The x:Name match is hidden. Quietly clicking the visible control that merely has the same text would operate
        // a different element than the one the agent named.
        var candidates = new[]
        {
            Cand("w/Button[0]", name: "Go", visible: false, hiddenReason: "ancestor Panel is Collapsed"),
            Cand("w/Button[1]", text: "Go"),
        };

        var resolution = UiTargetResolver.Resolve("Go", candidates, force: false);

        Assert.Equal(UiResolutionKind.Hidden, resolution.Kind);
        Assert.Equal("w/Button[0]", resolution.Element!.Path);
    }

    [Fact]
    public void Resolve_XNameIsCaseSensitive()
    {
        var resolution = UiTargetResolver.Resolve("recordbutton", new[] { Cand("w/Button[0]", name: "RecordButton") }, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
    }

    [Fact]
    public void Resolve_AutomationIdIsCaseSensitive()
    {
        var resolution = UiTargetResolver.Resolve("recordbutton", new[] { Cand("w/Button[0]", id: "RecordButton") }, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
    }

    [Fact]
    public void Resolve_AutomationNameAndTextAreTrimmedAndCaseInsensitive()
    {
        var byAutomationName = UiTargetResolver.Resolve("  start RECORDING ", new[] { Cand("w/Button[0]", automationName: "Start recording") }, force: false);
        var byText = UiTargetResolver.Resolve("open folder", new[] { Cand("w/Button[0]", text: " Open Folder ") }, force: false);

        Assert.Equal(UiResolutionKind.Matched, byAutomationName.Kind);
        Assert.Equal(UiResolutionKind.Matched, byText.Kind);
    }

    [Fact]
    public void Resolve_TextMatchesWholeStringNotSubstring()
    {
        var candidates = new[] { Cand("w/Button[0]", text: "Record audio") };

        var resolution = UiTargetResolver.Resolve("Record", candidates, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
        Assert.Contains(resolution.Candidates, candidate => candidate.Path == "w/Button[0]");
    }

    [Fact]
    public void Resolve_WhitespaceOnlyTextNeverMatches()
    {
        var resolution = UiTargetResolver.Resolve(" ", new[] { Cand("w/TextBlock[0]", type: "TextBlock", text: " ", interactive: false) }, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
    }

    [Fact]
    public void Resolve_TwoUsableMatches_AreAmbiguousAndListPaths()
    {
        var candidates = new[]
        {
            Cand("w/Grid[0]/Button[0]", text: "Close"),
            Cand("popup[0]/Button[0]", text: "Close"),
        };

        var resolution = UiTargetResolver.Resolve("Close", candidates, force: false);

        Assert.Equal(UiResolutionKind.Ambiguous, resolution.Kind);
        Assert.Null(resolution.Element);
        Assert.Equal(2, resolution.CandidateTotal);
        Assert.Contains("w/Grid[0]/Button[0]", resolution.Message);
        Assert.Contains("popup[0]/Button[0]", resolution.Message);
    }

    [Fact]
    public void Resolve_AmbiguousPathsRoundTripToExactlyThatElement()
    {
        var candidates = new[]
        {
            Cand("w/Grid[0]/Button[0]", text: "Close"),
            Cand("popup[0]/Button[0]", text: "Close"),
        };

        var ambiguous = UiTargetResolver.Resolve("Close", candidates, force: false);
        foreach (var listed in ambiguous.Candidates)
        {
            var resolution = UiTargetResolver.Resolve(listed.Path, candidates, force: false);

            Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
            Assert.Equal("path", resolution.MatchedBy);
            Assert.Equal(listed.Path, resolution.Element!.Path);
        }
    }

    [Fact]
    public void Resolve_PathThatMatchesNothingIsNotFound()
    {
        var resolution = UiTargetResolver.Resolve("w/Grid[9]/Button[9]", new[] { Cand("w/Grid[0]/Button[0]", text: "Close") }, force: false);

        Assert.Equal(UiResolutionKind.NotFound, resolution.Kind);
    }

    [Fact]
    public void Resolve_AmbiguousListIsCappedButReportsTheTotal()
    {
        var candidates = Enumerable.Range(0, 25)
            .Select(index => Cand($"w/Button[{index}]", text: "Same"))
            .ToList();

        var resolution = UiTargetResolver.Resolve("Same", candidates, force: false);

        Assert.Equal(UiResolutionKind.Ambiguous, resolution.Kind);
        Assert.Equal(UiTargetResolver.MaxListedCandidates, resolution.Candidates.Count);
        Assert.Equal(25, resolution.CandidateTotal);
        Assert.Contains("25", resolution.Message);
    }

    [Fact]
    public void Resolve_HiddenTwinDoesNotBlockTheVisibleElement()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Start", visible: false, hiddenReason: "Visibility is Collapsed"),
            Cand("w/Button[1]", text: "Start"),
        };

        var resolution = UiTargetResolver.Resolve("Start", candidates, force: false);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Equal("w/Button[1]", resolution.Element!.Path);
    }

    [Fact]
    public void Resolve_DisabledTwinDoesNotBlockTheEnabledElement()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Start", enabled: false, disabledReason: "IsEnabled is false"),
            Cand("w/Button[1]", text: "Start"),
        };

        var resolution = UiTargetResolver.Resolve("Start", candidates, force: false);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Equal("w/Button[1]", resolution.Element!.Path);
    }

    [Fact]
    public void Resolve_LabelInsideButtonDoesNotMakeTheButtonAmbiguous()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Record"),
            Cand("w/Button[0]/TextBlock[0]", type: "TextBlock", text: "Record", interactive: false),
        };

        var resolution = UiTargetResolver.Resolve("Record", candidates, force: false);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Equal("w/Button[0]", resolution.Element!.Path);
    }

    [Fact]
    public void Resolve_TwoInteractiveMatchesStayAmbiguousEvenWithALabelPresent()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Record"),
            Cand("w/Button[1]", text: "Record"),
            Cand("w/TextBlock[0]", type: "TextBlock", text: "Record", interactive: false),
        };

        var resolution = UiTargetResolver.Resolve("Record", candidates, force: false);

        Assert.Equal(UiResolutionKind.Ambiguous, resolution.Kind);
        Assert.Equal(2, resolution.CandidateTotal);
    }

    [Fact]
    public void Resolve_SingleHiddenElementIsRejectedWithItsReason()
    {
        var candidates = new[] { Cand("w/Button[0]", name: "Go", visible: false, hiddenReason: "ancestor SettingsPanel is Collapsed") };

        var resolution = UiTargetResolver.Resolve("Go", candidates, force: false);

        Assert.Equal(UiResolutionKind.Hidden, resolution.Kind);
        Assert.Contains("SettingsPanel", resolution.Message);
        Assert.Contains("force", resolution.Message);
    }

    [Fact]
    public void Resolve_SingleDisabledElementIsRejectedWithItsReason()
    {
        var candidates = new[] { Cand("w/Button[0]", name: "Go", enabled: false, disabledReason: "IsEnabled is false") };

        var resolution = UiTargetResolver.Resolve("Go", candidates, force: false);

        Assert.Equal(UiResolutionKind.Disabled, resolution.Kind);
        Assert.Contains("IsEnabled is false", resolution.Message);
        Assert.Contains("force", resolution.Message);
    }

    [Fact]
    public void Resolve_HiddenAndDisabledReportsBothAndForceOverridesBoth()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", name: "Go", visible: false, hiddenReason: "Opacity is 0", enabled: false, disabledReason: "ancestor Panel is disabled"),
        };

        var rejected = UiTargetResolver.Resolve("Go", candidates, force: false);
        var forced = UiTargetResolver.Resolve("Go", candidates, force: true);

        Assert.Equal(UiResolutionKind.Hidden, rejected.Kind);
        Assert.Contains("Opacity is 0", rejected.Message);
        Assert.Contains("ancestor Panel is disabled", rejected.Message);
        Assert.Equal(UiResolutionKind.Matched, forced.Kind);
        Assert.Equal(2, forced.Overridden.Count);
        Assert.Contains(forced.Overridden, item => item.Contains("Opacity is 0"));
        Assert.Contains(forced.Overridden, item => item.Contains("ancestor Panel is disabled"));
    }

    [Fact]
    public void Resolve_ForceOnAUsableElementOverridesNothing()
    {
        var resolution = UiTargetResolver.Resolve("Go", new[] { Cand("w/Button[0]", name: "Go") }, force: true);

        Assert.Equal(UiResolutionKind.Matched, resolution.Kind);
        Assert.Empty(resolution.Overridden);
    }

    [Fact]
    public void Resolve_ForceNeverPicksAmongAmbiguousElements()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Close"),
            Cand("w/Button[1]", text: "Close"),
        };

        var resolution = UiTargetResolver.Resolve("Close", candidates, force: true);

        Assert.Equal(UiResolutionKind.Ambiguous, resolution.Kind);
        Assert.Null(resolution.Element);
    }

    [Fact]
    public void Resolve_AllMatchesHiddenAndSeveral_IsAmbiguousNotHidden()
    {
        var candidates = new[]
        {
            Cand("w/Button[0]", text: "Close", visible: false, hiddenReason: "Visibility is Collapsed"),
            Cand("w/Button[1]", text: "Close", visible: false, hiddenReason: "Visibility is Collapsed"),
        };

        var resolution = UiTargetResolver.Resolve("Close", candidates, force: true);

        Assert.Equal(UiResolutionKind.Ambiguous, resolution.Kind);
    }
}
