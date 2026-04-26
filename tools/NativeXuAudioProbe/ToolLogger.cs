using System.Diagnostics;

namespace ElgatoCapture;

internal static class Logger
{
    public static void Log(string message)
        => Trace.TraceInformation(message);
}
