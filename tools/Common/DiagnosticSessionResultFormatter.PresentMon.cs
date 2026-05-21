using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendPresentMon(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.PresentMon is not null)
        {
            builder.AppendLine($"PresentMon: {(result.PresentMon.Success ? "PASS" : "FAIL")} | {result.PresentMon.Message}");
        }
    }
}
