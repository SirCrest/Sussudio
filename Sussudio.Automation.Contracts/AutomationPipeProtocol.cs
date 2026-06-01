using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    // the AutomationCommandKind section in
    // Sussudio.Automation.Contracts/AutomationCommandCatalog.cs for the
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

public readonly record struct AutomationPipeCommandResult(
    string ResponseJson,
    bool StateRead,
    bool Success,
    string? Status,
    int? RetryAfterMs,
    JsonElement? ResponseElement);

public class AutomationPipeException : Exception
{
    public AutomationPipeException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AutomationPipeConnectException : AutomationPipeException
{
    public AutomationPipeConnectException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

public sealed class AutomationPipeResponseTimeoutException : AutomationPipeException
{
    public AutomationPipeResponseTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class AutomationPipeProtocolException : AutomationPipeException
{
    public AutomationPipeProtocolException(string message)
        : base(message)
    {
    }
}

public enum AutomationUnknownCommandHandling
{
    ReturnSyntheticError,
    ThrowArgumentException
}

// Tolerant success/status/retry reader for automation response envelopes.
public static class AutomationResponseState
{
    public static bool TryRead(
        JsonElement response,
        out bool success,
        out string? status,
        out int? retryAfterMs)
    {
        success = false;
        status = null;
        retryAfterMs = null;

        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Success", out var successProperty) &&
            successProperty.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            success = successProperty.GetBoolean();
        }

        if (response.TryGetProperty("Status", out var statusProperty) &&
            statusProperty.ValueKind == JsonValueKind.String)
        {
            status = statusProperty.GetString();
        }

        if (response.TryGetProperty("RetryAfterMs", out var retryAfterProperty))
        {
            if (retryAfterProperty.ValueKind == JsonValueKind.Number &&
                retryAfterProperty.TryGetInt32(out var numeric))
            {
                retryAfterMs = numeric;
            }
            else if (retryAfterProperty.ValueKind == JsonValueKind.String &&
                     int.TryParse(
                         retryAfterProperty.GetString(),
                         NumberStyles.Integer,
                         CultureInfo.InvariantCulture,
                         out var parsed))
            {
                retryAfterMs = parsed;
            }
        }

        return true;
    }
}

// Shared named-pipe client and transport helpers used by ssctl, MCP, diagnostic sessions, and AutomationClient.
internal static class AutomationPipeClient
{
    internal static async Task<string> SendCommandAsync(
        string pipeName,
        string commandName,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithResultAsync(
                pipeName,
                commandName,
                payload,
                connectTimeoutMs,
                responseTimeoutMs,
                authToken,
                includeResponseElement: false,
                cancellationToken)
            .ConfigureAwait(false);
        return result.ResponseJson;
    }

    internal static async Task<string> SendCommandAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithResultAsync(
                pipeName,
                kind,
                payload,
                connectTimeoutMs,
                responseTimeoutMs,
                authToken,
                includeResponseElement: false,
                cancellationToken)
            .ConfigureAwait(false);
        return result.ResponseJson;
    }

    internal static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(
        string pipeName,
        string commandName,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        bool includeResponseElement = false,
        CancellationToken cancellationToken = default)
    {
        var commandValue = AutomationPipeProtocol.ResolveCommand(commandName);

        return await SendCommandWithResultAsync(
                pipeName,
                commandValue,
                payload,
                connectTimeoutMs,
                responseTimeoutMs,
                authToken,
                includeResponseElement,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal static Task<AutomationPipeCommandResult> SendCommandWithResultAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        bool includeResponseElement = false,
        CancellationToken cancellationToken = default)
        => SendCommandWithResultAsync(
            pipeName,
            (int)kind,
            payload,
            connectTimeoutMs,
            responseTimeoutMs,
            authToken,
            includeResponseElement,
            cancellationToken);

    private static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(
        string pipeName,
        int commandValue,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken,
        bool includeResponseElement,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = AutomationPipeProtocol.CreateRequestEnvelope(
                commandValue,
                payload,
                authToken: authToken);

            var responseLine = await SendRequestAsync(
                    pipeName,
                    JsonSerializer.Serialize(request),
                    connectTimeoutMs,
                    responseTimeoutMs,
                    cancellationToken)
                .ConfigureAwait(false);

            var stateRead = TryReadResponseState(
                responseLine,
                includeResponseElement,
                out var success,
                out var status,
                out var retryAfterMs,
                out var responseElement);

            if (!stateRead ||
                success ||
                !string.Equals(status, "not_ready", StringComparison.OrdinalIgnoreCase) ||
                attempt >= AutomationPipeProtocol.DefaultNotReadyRetries)
            {
                return new AutomationPipeCommandResult(
                    responseLine,
                    stateRead,
                    success,
                    status,
                    retryAfterMs,
                    responseElement);
            }

            var delayMs = Math.Clamp(
                retryAfterMs ?? AutomationPipeProtocol.DefaultNotReadyDelayMs,
                100,
                30000);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static bool TryReadResponseState(
        string responseJson,
        out bool success,
        out string? status,
        out int? retryAfterMs)
        => TryReadResponseState(
            responseJson,
            includeResponseElement: false,
            out success,
            out status,
            out retryAfterMs,
            out _);

    internal static Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs)
        => SendRequestAsync(
            pipeName,
            requestJson,
            connectTimeoutMs,
            responseTimeoutMs,
            CancellationToken.None);

    internal static async Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs,
        CancellationToken cancellationToken)
    {
        using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await ConnectWithClassifiedErrorsAsync(
                client,
                pipeName,
                connectTimeoutMs,
                cancellationToken)
            .ConfigureAwait(false);

        using var writer = new StreamWriter(
            client,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        using var reader = new StreamReader(
            client,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        await writer.WriteLineAsync(requestJson).WaitAsync(cancellationToken).ConfigureAwait(false);
        string? responseLine;
        try
        {
            responseLine = await reader.ReadLineAsync()
                .WaitAsync(TimeSpan.FromMilliseconds(responseTimeoutMs), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeResponseTimeoutException(
                $"Timed out waiting for automation response after {responseTimeoutMs} ms.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new AutomationPipeProtocolException("No response received from automation pipe.");
        }

        return responseLine;
    }

    private static bool TryReadResponseState(
        string responseJson,
        bool includeResponseElement,
        out bool success,
        out string? status,
        out int? retryAfterMs,
        out JsonElement? responseElement)
    {
        success = false;
        status = null;
        retryAfterMs = null;
        responseElement = null;

        try
        {
            using var responseDocument = JsonDocument.Parse(responseJson);
            var response = responseDocument.RootElement;
            var stateRead = AutomationResponseState.TryRead(
                response,
                out success,
                out status,
                out retryAfterMs);
            if (includeResponseElement)
            {
                responseElement = response.Clone();
            }

            return stateRead;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task ConnectWithClassifiedErrorsAsync(
        NamedPipeClientStream client,
        string pipeName,
        int connectTimeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeConnectException(
                $"Timed out connecting to automation pipe '{pipeName}' after {connectTimeoutMs} ms.",
                "pipe-connect-timeout",
                ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AutomationPipeConnectException(
                $"Access denied connecting to automation pipe '{pipeName}'. The app is running, but this process is not allowed by the pipe security descriptor. Run the client from the same Windows user/elevation/session as the app, or restart the app with {AutomationPipeProtocol.AutomationKeyEnvVar} configured for token-gated fallback security.",
                "pipe-access-denied",
                ex);
        }
        catch (Exception ex)
        {
            throw new AutomationPipeConnectException(
                $"Failed to connect to automation pipe '{pipeName}': {ex.Message}",
                "pipe-connect-failed",
                ex);
        }
    }
}

internal static class AutomationCommandTransport
{
    public static async Task<JsonElement> SendCommandAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload = null,
        int? responseTimeoutOverrideMs = null,
        int? responseTimeoutMs = null,
        AutomationUnknownCommandHandling unknownCommandHandling = AutomationUnknownCommandHandling.ReturnSyntheticError)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs
            ?? responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(kind);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    pipeName,
                    kind,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (ArgumentException ex) when (unknownCommandHandling == AutomationUnknownCommandHandling.ReturnSyntheticError)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }

    public static async Task<JsonElement> SendCommandAsync(
        string pipeName,
        string commandName,
        object? payload = null,
        int? responseTimeoutOverrideMs = null,
        int? responseTimeoutMs = null,
        AutomationUnknownCommandHandling unknownCommandHandling = AutomationUnknownCommandHandling.ReturnSyntheticError)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs
            ?? responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    pipeName,
                    commandName,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (ArgumentException ex) when (unknownCommandHandling == AutomationUnknownCommandHandling.ReturnSyntheticError)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }
}

public static class AutomationSyntheticErrorResponse
{
    public static JsonElement Create(string message, string errorCode)
    {
        var response = new Dictionary<string, object?>
        {
            ["Success"] = false,
            ["CorrelationId"] = Guid.NewGuid().ToString("N"),
            ["TimestampUtc"] = DateTimeOffset.UtcNow,
            ["Status"] = "error",
            ["CommandLifecycle"] = "failed",
            ["RetryAfterMs"] = null,
            ["ElapsedMs"] = null,
            ["Message"] = string.IsNullOrWhiteSpace(message) ? "Unknown pipe client error." : message,
            ["ErrorCode"] = errorCode,
            ["Data"] = null,
            ["Snapshot"] = null
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response));
        return document.RootElement.Clone();
    }

    public static bool CanCreateFromException(Exception exception)
        => exception is AutomationPipeConnectException ||
            exception is AutomationPipeResponseTimeoutException ||
            exception is AutomationPipeProtocolException ||
            exception is JsonException ||
            exception is IOException ||
            exception is OperationCanceledException;

    public static JsonElement Create(Exception exception)
        => exception switch
        {
            AutomationPipeConnectException ex => Create(ex.Message, ex.ErrorCode),
            AutomationPipeResponseTimeoutException ex => Create(ex.Message, "pipe-response-timeout"),
            AutomationPipeProtocolException ex => Create(ex.Message, "pipe-protocol-error"),
            JsonException ex => Create(
                $"Automation pipe returned invalid JSON: {ex.Message}",
                "pipe-invalid-json"),
            IOException ex => Create(
                $"Automation pipe I/O failed ({ex.GetType().Name}): {ex.Message}",
                "pipe-io-error"),
            OperationCanceledException ex => Create(
                $"Automation pipe request canceled: {ex.Message}",
                "pipe-canceled"),
            _ => throw new ArgumentException(
                $"Exception type '{exception.GetType().FullName}' cannot be converted to a synthetic automation error response.",
                nameof(exception))
        };
}
