namespace Sussudio.Tools;

internal static class DiagnosticSessionOptionalTextFormatter
{
    internal static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}
