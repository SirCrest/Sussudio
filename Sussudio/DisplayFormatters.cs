namespace Sussudio;

// UI display helpers for compact source-signal labels.
internal static class DisplayFormatters
{
    public static string FormatSourceHdr(bool? isHdr, string? colorimetry)
    {
        return isHdr switch
        {
            true when !string.IsNullOrWhiteSpace(colorimetry) => $"On ({colorimetry})",
            true => "On",
            false => "Off",
            _ => "\u2014"
        };
    }
}
