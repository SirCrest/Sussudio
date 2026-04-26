using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ElgatoCapture.Models;

namespace ElgatoCapture.Tools;

internal static class AutomationPipeProtocol
{
    internal const string DefaultPipeName = "ElgatoCaptureAutomation";
    internal const string AutomationKeyEnvVar = "ELGATOCAPTURE_AUTOMATION_TOKEN";
    internal const int DefaultConnectTimeoutMs = 5000;
    internal const int DefaultResponseTimeoutMs = 15000;
    internal const int ExtendedResponseTimeoutMs = 60000;
    internal const int RecordingResponseTimeoutMs = 150000;
    internal const int FlashbackMutationResponseTimeoutMs = 305000;
    internal const int DefaultNotReadyRetries = 15;
    internal const int DefaultNotReadyDelayMs = 1000;

    internal static IReadOnlyDictionary<string, int> CommandMap { get; } =
        Enum.GetValues<AutomationCommandKind>()
            .ToDictionary(
                command => command.ToString(),
                command => (int)command,
                StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<int, string> CommandNamesByValue { get; } =
        CommandMap.ToDictionary(
            entry => entry.Value,
            entry => entry.Key);

    internal static string? GetConfiguredAuthToken(string? explicitAuthToken = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitAuthToken))
        {
            return explicitAuthToken;
        }

        var envToken = Environment.GetEnvironmentVariable(AutomationKeyEnvVar);
        return string.IsNullOrWhiteSpace(envToken) ? null : envToken;
    }

    internal static int GetDefaultResponseTimeout(string commandName)
    {
        commandName = ResolveCanonicalCommandName(commandName);

        var commandTimeoutMs = commandName switch
        {
            "SetRecordingEnabled" => RecordingResponseTimeoutMs,
            "RestartFlashback" or "SetFlashbackEnabled" => FlashbackMutationResponseTimeoutMs,
            "WaitForCondition" or "VerifyLastRecording" or "CapturePreviewFrame" or
            "CaptureWindowScreenshot" or "FlashbackExport" or "VerifyFile" => ExtendedResponseTimeoutMs,
            _ => DefaultResponseTimeoutMs
        };
        return commandTimeoutMs;
    }

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

    internal static bool TryGetCommandValue(string commandName, out int commandValue)
        => CommandMap.TryGetValue(commandName, out commandValue);

    internal static bool TryGetCommandName(int commandValue, out string commandName)
    {
        if (CommandNamesByValue.TryGetValue(commandValue, out var resolvedName))
        {
            commandName = resolvedName;
            return true;
        }

        commandName = string.Empty;
        return false;
    }

    internal static Dictionary<string, object?> CreateRequestEnvelope(
        int commandValue,
        object? payload = null,
        string? authToken = null)
    {
        return new Dictionary<string, object?>
        {
            ["command"] = commandValue,
            ["correlationId"] = Guid.NewGuid().ToString("N"),
            ["authToken"] = authToken ?? GetConfiguredAuthToken(),
            ["payload"] = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal)
        };
    }

    internal static int ResolveCommand(string command)
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
