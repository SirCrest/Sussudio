using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sussudio.Models;

namespace Sussudio.Tools;

public enum AutomationPayloadFieldType
{
    String,
    Boolean,
    Integer,
    Number,
    Object,
    Array
}

public sealed record AutomationPayloadFieldMetadata(
    string Name,
    AutomationPayloadFieldType Type,
    bool Required);

public sealed record AutomationCommandMetadata(
    AutomationCommandKind Kind,
    string Name,
    string PayloadShape,
    IReadOnlyList<AutomationPayloadFieldMetadata> PayloadFields,
    bool RequiresReadyDevices,
    int ResponseTimeoutMs,
    AutomationCommandPathPolicy PathPolicy,
    string CliHelp,
    string McpDescription);

public enum AutomationCommandPathPolicy
{
    None,
    ReadFile,
    WriteFile,
    Directory
}

public sealed record AutomationManifest(
    int SchemaVersion,
    IReadOnlyList<AutomationManifestCommand> Commands);

public sealed record AutomationManifestCommand(
    int Id,
    string Name,
    string PayloadShape,
    IReadOnlyList<AutomationManifestPayloadField> PayloadFields,
    int ResponseTimeoutMs,
    bool RequiresReadyDevices,
    string PathPolicy,
    string CliHelp,
    string McpDescription);

public sealed record AutomationManifestPayloadField(
    string Name,
    string Type,
    bool Required);

// Shared command metadata used to keep the app server, ssctl, MCP, and raw
// automation client from drifting on readiness gates, path-bearing payloads,
// and long-running command timeout policy.
public static partial class AutomationCommandCatalog
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new();

    public static IReadOnlyList<AutomationCommandMetadata> Entries { get; } =
        BuildEntries()
            .OrderBy(entry => (int)entry.Kind)
            .ToArray();

    private static IReadOnlyDictionary<AutomationCommandKind, AutomationCommandMetadata> EntriesByKind { get; } =
        Entries.ToDictionary(entry => entry.Kind);

    public static AutomationCommandMetadata Get(AutomationCommandKind kind)
    {
        if (EntriesByKind.TryGetValue(kind, out var metadata))
        {
            return metadata;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown automation command kind.");
    }

    public static bool TryGet(string commandName, out AutomationCommandMetadata metadata)
    {
        if (TryResolveKind(commandName, out var kind))
        {
            metadata = Get(kind);
            return true;
        }

        metadata = null!;
        return false;
    }

    public static AutomationManifest CreateManifest()
        => new(
            SchemaVersion: 1,
            Commands: Entries
                .Select(entry => new AutomationManifestCommand(
                    Id: (int)entry.Kind,
                    Name: entry.Name,
                    PayloadShape: entry.PayloadShape,
                    PayloadFields: entry.PayloadFields
                        .Select(field => new AutomationManifestPayloadField(
                            field.Name,
                            field.Type.ToString(),
                            field.Required))
                        .ToArray(),
                    ResponseTimeoutMs: entry.ResponseTimeoutMs,
                    RequiresReadyDevices: entry.RequiresReadyDevices,
                    PathPolicy: entry.PathPolicy.ToString(),
                    CliHelp: entry.CliHelp,
                    McpDescription: entry.McpDescription))
                .ToArray());

    public static string CreateManifestJson()
        => JsonSerializer.Serialize(CreateManifest(), ManifestJsonOptions);

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

    private static bool TryResolveKind(string commandName, out AutomationCommandKind kind)
    {
        if (int.TryParse(commandName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) &&
            Enum.IsDefined(typeof(AutomationCommandKind), numeric))
        {
            kind = (AutomationCommandKind)numeric;
            return true;
        }

        if (Enum.TryParse(commandName, ignoreCase: true, out kind) &&
            Enum.IsDefined(kind))
        {
            return true;
        }

        var normalized = Normalize(commandName);
        foreach (var candidate in Enum.GetValues<AutomationCommandKind>())
        {
            if (Normalize(candidate.ToString()).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        kind = default;
        return false;
    }

    private static AutomationCommandMetadata CreateDefault(AutomationCommandKind kind)
        => new(
            kind,
            kind.ToString(),
            "{}",
            Array.Empty<AutomationPayloadFieldMetadata>(),
            RequiresReadyDevices: false,
            ResponseTimeoutMs: DefaultTimeout,
            PathPolicy: AutomationCommandPathPolicy.None,
            CliHelp: kind.ToString(),
            McpDescription: $"Automation command {kind}.");

    private static void Set(
        Dictionary<AutomationCommandKind, AutomationCommandMetadata> entries,
        AutomationCommandKind kind,
        string payloadShape,
        bool ready,
        int timeoutMs,
        AutomationCommandPathPolicy pathPolicy,
        string cliHelp,
        string mcpDescription,
        params AutomationPayloadFieldMetadata[] payloadFields)
        => entries[kind] = new AutomationCommandMetadata(
            kind,
            kind.ToString(),
            payloadShape,
            payloadFields,
            ready,
            timeoutMs,
            pathPolicy,
            cliHelp,
            mcpDescription);

    private static int DefaultTimeout => AutomationPipeProtocol.DefaultResponseTimeoutMs;

    private static AutomationPayloadFieldMetadata Required(string name, AutomationPayloadFieldType type)
        => new(name, type, Required: true);

    private static AutomationPayloadFieldMetadata Optional(string name, AutomationPayloadFieldType type)
        => new(name, type, Required: false);

    private static string Normalize(string value)
    {
        var buffer = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(buffer);
    }
}
