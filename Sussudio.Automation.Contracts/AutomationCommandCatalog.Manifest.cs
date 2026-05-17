using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Sussudio.Tools;

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

public static partial class AutomationCommandCatalog
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new();

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
}
