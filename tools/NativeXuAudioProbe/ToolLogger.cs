using System.Diagnostics;

namespace Sussudio;

internal static class Logger
{
    public static void Log(string message)
        => Trace.TraceInformation(message);
}
