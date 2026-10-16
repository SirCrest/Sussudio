using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sussudio.Services.Automation;

/// <summary>
/// What the live-tree walk knows about one element, reduced to what target resolution needs. <see cref="Path"/> is
/// unique within one walk, so it is the disambiguator an ambiguity error hands back to the agent.
/// </summary>
public sealed record UiTargetCandidate(
    string Path,
    string Type,
    string? Name,
    string? AutomationId,
    string? AutomationName,
    string? Text,
    bool IsEnabled,
    string? DisabledReason,
    bool IsVisible,
    string? HiddenReason,
    bool Interactive);

public enum UiResolutionKind
{
    Matched,
    NotFound,
    Ambiguous,
    Hidden,
    Disabled,
}

public static class UiMatchKind
{
    public const string Path = "path";
    public const string Name = "x:Name";
    public const string AutomationId = "automation-id";
    public const string AutomationName = "automation-name";
    public const string Text = "text";
}

/// <summary>
/// Outcome of resolving a target. For <see cref="UiResolutionKind.Matched"/>, <see cref="Element"/> is the element and
/// <see cref="Overridden"/> lists the pre-checks <c>force</c> skipped. For Hidden and Disabled, <see cref="Element"/> is
/// the rejected element. For Ambiguous, <see cref="Candidates"/> are the (capped) contenders; for NotFound they are
/// near-miss suggestions. <see cref="CandidateTotal"/> is the uncapped count.
/// </summary>
public sealed record UiResolution(
    UiResolutionKind Kind,
    UiTargetCandidate? Element,
    string? MatchedBy,
    IReadOnlyList<string> Overridden,
    IReadOnlyList<UiTargetCandidate> Candidates,
    int CandidateTotal,
    string Message);

/// <summary>
/// Resolves an agent-supplied target to exactly one element, or says precisely why it cannot. Precedence: a path from an
/// earlier tree or ambiguity error, then x:Name, AutomationId, AutomationProperties.Name, visible text; the first level
/// that matches anything decides, and a lower level is never consulted after that. x:Name and AutomationId are exact and
/// case-sensitive; automation name and text are trimmed, whole-string and case-insensitive. Among the matches of the
/// deciding level, hidden, disabled and non-interactive elements are dropped only when something better matched (so a
/// hidden twin, or the label inside a button, never blocks the real target); more than one survivor is ambiguous and
/// <c>force</c> never picks among them.
/// </summary>
public static class UiTargetResolver
{
    public const int MaxListedCandidates = 10;

    private static readonly (string Kind, Func<UiTargetCandidate, string, bool> Matches)[] Levels =
    {
        (UiMatchKind.Path, (candidate, target) => string.Equals(candidate.Path, target, StringComparison.Ordinal)),
        (UiMatchKind.Name, (candidate, target) => string.Equals(candidate.Name, target, StringComparison.Ordinal)),
        (UiMatchKind.AutomationId, (candidate, target) => string.Equals(candidate.AutomationId, target, StringComparison.Ordinal)),
        (UiMatchKind.AutomationName, (candidate, target) => EqualsLoose(candidate.AutomationName, target)),
        (UiMatchKind.Text, (candidate, target) => EqualsLoose(candidate.Text, target)),
    };

    public static UiResolution Resolve(string target, IReadOnlyList<UiTargetCandidate> candidates, bool force)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return NotFound("Target is empty. Pass an x:Name, AutomationId, automation name, visible text or a path from ui tree.", Array.Empty<UiTargetCandidate>(), 0);
        }

        var trimmed = target.Trim();
        foreach (var (kind, matches) in Levels)
        {
            var found = candidates.Where(candidate => matches(candidate, trimmed)).ToList();
            if (found.Count > 0)
            {
                return Decide(trimmed, kind, found, force);
            }
        }

        var suggestions = candidates
            .Where(candidate => Contains(candidate.Name, trimmed)
                || Contains(candidate.AutomationId, trimmed)
                || Contains(candidate.AutomationName, trimmed)
                || Contains(candidate.Text, trimmed))
            .OrderByDescending(candidate => candidate.IsVisible)
            .ToList();
        var message = new StringBuilder()
            .Append("No element matches '").Append(trimmed)
            .Append("' (looked at path, x:Name, AutomationId, AutomationProperties.Name and visible text).");
        if (suggestions.Count > 0)
        {
            message.Append(" Near matches: ").Append(Describe(suggestions.Take(MaxListedCandidates))).Append('.');
            if (suggestions.Count > MaxListedCandidates)
            {
                message.Append(" (").Append(suggestions.Count).Append(" in total.)");
            }
        }

        return NotFound(message.ToString(), suggestions.Take(MaxListedCandidates).ToList(), suggestions.Count);
    }

    private static UiResolution Decide(string target, string matchedBy, List<UiTargetCandidate> matches, bool force)
    {
        var narrowed = Narrow(matches, candidate => candidate.IsVisible);
        narrowed = Narrow(narrowed, candidate => candidate.IsEnabled);
        narrowed = Narrow(narrowed, candidate => candidate.Interactive);

        if (narrowed.Count > 1)
        {
            var listed = narrowed.Take(MaxListedCandidates).ToList();
            var message = new StringBuilder()
                .Append("Target '").Append(target).Append("' is ambiguous: ").Append(narrowed.Count)
                .Append(" elements match by ").Append(matchedBy).Append(". Pass one of these paths as the target: ")
                .Append(Describe(listed)).Append('.');
            if (narrowed.Count > listed.Count)
            {
                message.Append(" (Showing ").Append(listed.Count).Append(" of ").Append(narrowed.Count).Append(".)");
            }

            return new UiResolution(UiResolutionKind.Ambiguous, null, matchedBy, Array.Empty<string>(), listed, narrowed.Count, message.ToString());
        }

        var element = narrowed[0];
        var problems = new List<string>(2);
        if (!element.IsVisible)
        {
            problems.Add("hidden: " + (element.HiddenReason ?? "not visible"));
        }

        if (!element.IsEnabled)
        {
            problems.Add("disabled: " + (element.DisabledReason ?? "IsEnabled is false"));
        }

        if (problems.Count == 0)
        {
            return new UiResolution(UiResolutionKind.Matched, element, matchedBy, Array.Empty<string>(), Array.Empty<UiTargetCandidate>(), 1, string.Empty);
        }

        if (force)
        {
            return new UiResolution(UiResolutionKind.Matched, element, matchedBy, problems, Array.Empty<UiTargetCandidate>(), 1, string.Empty);
        }

        var kind = element.IsVisible ? UiResolutionKind.Disabled : UiResolutionKind.Hidden;
        var rejection = $"Element '{target}' ({element.Path}) is {string.Join(" and ", problems)}. Use force to attempt the action anyway.";
        return new UiResolution(kind, element, matchedBy, Array.Empty<string>(), Array.Empty<UiTargetCandidate>(), 1, rejection);
    }

    // Keeps only the matches satisfying the predicate, but only when that leaves at least one: the predicate is a
    // preference, not a filter, so a lone hidden or disabled match still gets reported as such.
    private static List<UiTargetCandidate> Narrow(List<UiTargetCandidate> matches, Func<UiTargetCandidate, bool> prefer)
    {
        if (matches.Count < 2)
        {
            return matches;
        }

        var preferred = matches.Where(prefer).ToList();
        return preferred.Count > 0 ? preferred : matches;
    }

    private static UiResolution NotFound(string message, IReadOnlyList<UiTargetCandidate> suggestions, int total)
        => new(UiResolutionKind.NotFound, null, null, Array.Empty<string>(), suggestions, total, message);

    private static bool EqualsLoose(string? field, string target)
        => !string.IsNullOrWhiteSpace(field)
            && string.Equals(field.Trim(), target, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string? field, string target)
        => !string.IsNullOrWhiteSpace(field)
            && field.Contains(target, StringComparison.OrdinalIgnoreCase);

    private static string Describe(IEnumerable<UiTargetCandidate> candidates)
        => string.Join("; ", candidates.Select(candidate =>
        {
            var label = new StringBuilder(candidate.Type);
            if (!string.IsNullOrEmpty(candidate.Name))
            {
                label.Append(" #").Append(candidate.Name);
            }

            if (!string.IsNullOrWhiteSpace(candidate.Text))
            {
                label.Append(" \"").Append(Shorten(candidate.Text.Trim(), 40)).Append('"');
            }

            return $"{candidate.Path} [{label}]";
        }));

    private static string Shorten(string value, int max)
        => value.Length <= max ? value : value.Substring(0, max - 3) + "...";
}
