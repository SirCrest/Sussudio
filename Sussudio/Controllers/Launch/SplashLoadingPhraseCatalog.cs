using System;
using System.Collections.Generic;
using System.IO;

namespace Sussudio.Controllers;

internal static class SplashLoadingPhraseCatalog
{
    private static readonly string[] DefaultSplashLoadingPhrases =
    {
        "Reticulating splines",
        "Re-rounding corners",
        "Warming the silicon",
        "Calibrating HDR",
        "Summoning Phil",
    };

    private static string[]? _cachedSplashPhrases;

    public static string[] Load()
    {
        if (_cachedSplashPhrases is not null) return _cachedSplashPhrases;

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "SplashPhrases.md");
            if (File.Exists(path))
            {
                var phrases = new List<string>();
                var inPhraseSection = false;
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("##"))
                    {
                        inPhraseSection = true;
                        continue;
                    }
                    if (line.StartsWith('#')) continue;
                    if (!inPhraseSection) continue;
                    if (line.StartsWith("<!--")) continue;
                    if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
                    {
                        line = line[2..].Trim();
                    }
                    if (line.Length == 0) continue;

                    while (line.EndsWith('.'))
                    {
                        line = line[..^1].TrimEnd();
                    }

                    if (line.Length == 0) continue;
                    phrases.Add(line);
                }
                if (phrases.Count > 0)
                {
                    _cachedSplashPhrases = phrases.ToArray();
                    return _cachedSplashPhrases;
                }
            }
        }
        catch
        {
            // Splash copy must never block startup.
        }

        _cachedSplashPhrases = DefaultSplashLoadingPhrases;
        return _cachedSplashPhrases;
    }
}
