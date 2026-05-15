using System.IO;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class SsctlHelpWriter
{
    private static void WriteCatalogHelpLine(TextWriter writer, AutomationCommandKind kind, string? suffix = null)
    {
        var command = AutomationCommandCatalog.Get(kind).CliHelp;
        writer.WriteLine(string.IsNullOrWhiteSpace(suffix)
            ? $"  {command}"
            : $"  {command} {suffix}");
    }
}
