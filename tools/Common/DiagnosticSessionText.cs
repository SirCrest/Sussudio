namespace Sussudio.Tools;

internal static class DiagnosticSessionText
{
    internal static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}
