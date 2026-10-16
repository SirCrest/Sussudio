using System.Collections.Generic;
using System.Globalization;

namespace Sussudio.Services.Automation;

/// <summary>One element of the visual-tree chain from the window content down to the element being judged.</summary>
public readonly record struct UiVisibilityLayer(string Label, bool Collapsed, double Opacity, double Width, double Height);

public readonly record struct UiVisibility(bool IsVisible, string? Reason);

/// <summary>
/// Effective visibility: a <c>Visibility</c> value alone says nothing about ancestors, opacity or layout. An element is
/// hidden when it or any ancestor is Collapsed or has opacity at or below 0.01 (an invisible but clickable control is a
/// trap), or when the element itself has no layout size. The reason names the root-most cause, which is what the agent
/// must change. A zero-sized ancestor alone does not hide a child (children can overflow it).
/// </summary>
public static class UiVisibilityRules
{
    public const double MinimumVisibleOpacity = 0.01;

    /// <param name="chain">Root first, the judged element last.</param>
    public static UiVisibility Evaluate(IReadOnlyList<UiVisibilityLayer> chain)
    {
        if (chain.Count == 0)
        {
            return new UiVisibility(false, "the element is not in the visual tree");
        }

        for (var index = 0; index < chain.Count; index++)
        {
            var layer = chain[index];
            var subject = index == chain.Count - 1 ? "the element" : $"ancestor {layer.Label}";
            if (layer.Collapsed)
            {
                return new UiVisibility(false, $"{subject} has Visibility Collapsed");
            }

            if (layer.Opacity <= MinimumVisibleOpacity)
            {
                return new UiVisibility(false, $"{subject} has Opacity {layer.Opacity.ToString("0.##", CultureInfo.InvariantCulture)}");
            }
        }

        var element = chain[^1];
        if (element.Width <= 0 || element.Height <= 0)
        {
            return new UiVisibility(
                false,
                $"the element has zero size ({element.Width.ToString("0.##", CultureInfo.InvariantCulture)} x {element.Height.ToString("0.##", CultureInfo.InvariantCulture)})");
        }

        return new UiVisibility(true, null);
    }
}
