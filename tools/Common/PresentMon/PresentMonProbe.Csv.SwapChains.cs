using System.Globalization;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static string? SelectPrimarySwapChain(IReadOnlyList<PresentMonRow> rows, string? expectedSwapChainAddress)
    {
        if (!string.IsNullOrWhiteSpace(expectedSwapChainAddress))
        {
            return rows.Any(row => string.Equals(row.SwapChainAddress, expectedSwapChainAddress, StringComparison.OrdinalIgnoreCase))
                ? expectedSwapChainAddress
                : null;
        }

        var selected = rows
            .Where(row => !IsArtifactSwapChain(row.SwapChainAddress))
            .GroupBy(row => row.SwapChainAddress, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        return selected?.Key;
    }

    private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(
        IReadOnlyList<PresentMonRow> rows,
        string? selectedSwapChain)
    {
        return rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.SwapChainAddress) ? "(missing)" : row.SwapChainAddress, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var groupRows = group.ToArray();
                return new PresentMonSwapChainSummary
                {
                    Address = group.Key,
                    SampleCount = groupRows.Length,
                    Selected = string.Equals(group.Key, selectedSwapChain, StringComparison.OrdinalIgnoreCase),
                    Artifact = IsArtifactSwapChain(group.Key),
                    BetweenPresentsMs = Summarize(groupRows.Select(row => row.BetweenPresentsMs)),
                    BetweenDisplayChangeMs = Summarize(groupRows.Select(row => row.BetweenDisplayChangeMs)),
                    UntilDisplayedMs = Summarize(groupRows.Select(row => row.UntilDisplayedMs)),
                    PresentModes = CountValues(groupRows.Select(row => row.PresentMode))
                };
            })
            .OrderByDescending(item => item.Selected)
            .ThenByDescending(item => item.SampleCount)
            .ToArray();
    }

    private static bool IsArtifactSwapChain(string? swapChainAddress)
        => string.IsNullOrWhiteSpace(swapChainAddress) ||
           string.Equals(swapChainAddress.Trim(), "0x0", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeSwapChainAddress(string? swapChainAddress)
    {
        if (string.IsNullOrWhiteSpace(swapChainAddress))
        {
            return null;
        }

        var value = swapChainAddress.Trim();
        if (value.Equals("0x0", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var digits = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : value;
        if (ulong.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric == 0 ? null : $"0x{numeric:X}";
        }

        return value.ToUpperInvariant();
    }
}
