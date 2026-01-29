using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ElgatoCapture;

public static class Logger
{
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ElgatoCapture_Debug.log");

    private static readonly object _lockObject = new();

    static Logger()
    {
        // Clear log on startup
        try
        {
            File.WriteAllText(LogFilePath, $"=== ElgatoCapture Debug Log ===\n");
            File.AppendAllText(LogFilePath, $"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n");
        }
        catch { }
    }

    public static void Log(string message, [CallerMemberName] string caller = "")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{caller}] {message}\n";

        // Write to debug output
        System.Diagnostics.Debug.WriteLine(logMessage.TrimEnd());

        // Write to file
        try
        {
            lock (_lockObject)
            {
                File.AppendAllText(LogFilePath, logMessage);
            }
        }
        catch { }
    }

    public static void LogException(Exception ex, [CallerMemberName] string caller = "")
    {
        Log($"EXCEPTION: {ex.GetType().Name}", caller);
        Log($"  Message: {ex.Message}", caller);
        Log($"  StackTrace: {ex.StackTrace}", caller);
    }

    public static string GetLogFilePath() => LogFilePath;
}
