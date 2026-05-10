using System.Diagnostics;

// Probe-local logger shim used by shared service code.
internal static class Logger
{
    public static void Log(string message)
        => Trace.TraceInformation(message);
}
