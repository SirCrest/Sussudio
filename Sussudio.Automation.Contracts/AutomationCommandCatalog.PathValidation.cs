using System;
using System.IO;
using Sussudio.Models;

namespace Sussudio.Tools;

public enum AutomationCommandPathPolicy
{
    None,
    ReadFile,
    WriteFile,
    Directory
}

public static partial class AutomationCommandCatalog
{
    public static string ValidatePath(
        AutomationCommandKind kind,
        string payloadKey,
        string path)
    {
        var metadata = Get(kind);
        if (metadata.PathPolicy == AutomationCommandPathPolicy.None)
        {
            return path;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"{metadata.Name} requires non-empty path payload '{payloadKey}'.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"{metadata.Name} payload '{payloadKey}' is not a valid path: {ex.Message}", ex);
        }

        switch (metadata.PathPolicy)
        {
            case AutomationCommandPathPolicy.WriteFile:
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    throw new InvalidOperationException($"{metadata.Name} payload '{payloadKey}' must include a writable file path.");
                }

                Directory.CreateDirectory(directory);
                break;
            }
            case AutomationCommandPathPolicy.Directory:
                Directory.CreateDirectory(fullPath);
                break;
            case AutomationCommandPathPolicy.ReadFile:
                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException($"{metadata.Name} payload '{payloadKey}' must reference an existing file: '{fullPath}'.");
                }
                break;
        }

        return path;
    }
}
