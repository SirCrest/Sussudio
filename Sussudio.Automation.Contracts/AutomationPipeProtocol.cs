using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Tools;

// Shared automation protocol constants, command names, and timeout policy used
// by ssctl, MCP, diagnostic sessions, and the generic automation client.
public static class AutomationPipeProtocol
{
    public const string DefaultPipeName = "SussudioAutomation";
    public const string AutomationKeyEnvVar = "SUSSUDIO_AUTOMATION_TOKEN";

    // Wire-format revision for the AutomationCommandKind numeric ID table.
    // Bump by +1 whenever AutomationCommandKind gains, loses, or renames a
    // member. The server uses this to reject clients that were built against
    // a different manifest revision before they can misroute a command. See
    // Sussudio.Automation.Contracts/AutomationCommandKind.cs for the
    // maintainer rules.
    public const int CommandManifestRevision = 1;

    public const int DefaultConnectTimeoutMs = 5000;
    public const int DefaultResponseTimeoutMs = 15000;
    public const int ExtendedResponseTimeoutMs = 60000;
    public const int RecordingResponseTimeoutMs = 150000;
    public const int FlashbackMutationResponseTimeoutMs = 305000;
    public const int DefaultNotReadyRetries = 15;
    public const int DefaultNotReadyDelayMs = 1000;

    public static IReadOnlyDictionary<string, int> CommandMap { get; } =
        Enum.GetValues<AutomationCommandKind>()
            .ToDictionary(
                command => command.ToString(),
                command => (int)command,
                StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<int, string> CommandNamesByValue { get; } =
        CommandMap.ToDictionary(
            entry => entry.Value,
            entry => entry.Key);

    public static string? GetConfiguredAuthToken(string? explicitAuthToken = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitAuthToken))
        {
            return explicitAuthToken;
        }

        var envToken = Environment.GetEnvironmentVariable(AutomationKeyEnvVar);
        return string.IsNullOrWhiteSpace(envToken) ? null : envToken;
    }

    public static int GetDefaultResponseTimeout(string commandName)
    {
        commandName = ResolveCanonicalCommandName(commandName);
        return AutomationCommandCatalog.TryGet(commandName, out var metadata)
            ? metadata.ResponseTimeoutMs
            : DefaultResponseTimeoutMs;
    }

    public static int GetDefaultResponseTimeout(AutomationCommandKind kind)
        => AutomationCommandCatalog.Get(kind).ResponseTimeoutMs;

    private static string ResolveCanonicalCommandName(string commandName)
    {
        if (int.TryParse(commandName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCommand) &&
            TryGetCommandName(numericCommand, out var numericCommandName))
        {
            return numericCommandName;
        }

        if (CommandMap.TryGetValue(commandName, out var directCommand) &&
            TryGetCommandName(directCommand, out var directCommandName))
        {
            return directCommandName;
        }

        var normalized = Normalize(commandName);
        foreach (var entry in CommandMap)
        {
            if (Normalize(entry.Key).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Key;
            }
        }

        return commandName;
    }

    public static bool TryGetCommandValue(string commandName, out int commandValue)
        => CommandMap.TryGetValue(commandName, out commandValue);

    public static bool TryGetCommandName(int commandValue, out string commandName)
    {
        if (CommandNamesByValue.TryGetValue(commandValue, out var resolvedName))
        {
            commandName = resolvedName;
            return true;
        }

        commandName = string.Empty;
        return false;
    }

    public static Dictionary<string, object?> CreateRequestEnvelope(
        int commandValue,
        object? payload = null,
        string? authToken = null)
    {
        return new Dictionary<string, object?>
        {
            ["command"] = commandValue,
            ["correlationId"] = Guid.NewGuid().ToString("N"),
            ["authToken"] = authToken ?? GetConfiguredAuthToken(),
            ["manifestRevision"] = CommandManifestRevision,
            ["payload"] = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal)
        };
    }

    public static int ResolveCommand(string command)
    {
        if (int.TryParse(command, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return numeric;
        }

        if (CommandMap.TryGetValue(command, out var directMatch))
        {
            return directMatch;
        }

        var normalized = Normalize(command);
        foreach (var entry in CommandMap)
        {
            if (Normalize(entry.Key).Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        throw new ArgumentException($"Unknown command '{command}'.");
    }

    private static string Normalize(string value)
    {
        var buffer = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(buffer);
    }
}

// Security fallback policy for the named-pipe server.
public static class AutomationPipeSecurityPolicy
{
    public static bool ShouldDisableDefaultSecurityFallback(
        bool isWindows,
        bool hasExplicitSecurityDescriptor,
        bool explicitSecurityFailed,
        bool authTokenRequired)
        => isWindows &&
           (!hasExplicitSecurityDescriptor || explicitSecurityFailed) &&
           !authTokenRequired;
}
