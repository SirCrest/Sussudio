using System.Collections.Generic;

namespace Sussudio.Services.Performance;

/// <summary>
/// Process-wide log tap fed by the logger's writer task: the last 4,096 lines plus per-caller and per-tag line counts.
/// See <see cref="LogTapBuffer"/> for the behavior; this class is only the shared instance.
/// </summary>
public static class LogTap
{
    private static readonly LogTapBuffer Buffer = new();

    /// <summary>Called by the logger writer for each entry. Never throws.</summary>
    public static void Add(string entry) => Buffer.Add(entry);

    public static IReadOnlyList<string> Tail(int max, string? contains) => Buffer.Tail(max, contains);

    public static LogTapCounts Counts() => Buffer.Counts();
}
