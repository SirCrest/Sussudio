using System.Text.Json;

namespace Sussudio.Tools.Performance;

/// <summary>A command the app refused or a response the runner cannot use. Never thrown for cancellation.</summary>
internal sealed class PerfExperimentCommandException : Exception
{
    public PerfExperimentCommandException(string message)
        : base(message)
    {
    }
}

/// <summary>One automation response. <see cref="Root"/> is the whole response; Data and Snapshot are its optional parts.</summary>
internal sealed record PerfReply(string Label, bool Success, string Message, JsonElement Root)
{
    public JsonElement? Data => PerfJson.Get(Root, "Data");

    public JsonElement? Snapshot => PerfJson.Get(Root, "Snapshot");
}

/// <summary>The parts of the app snapshot an experiment depends on.</summary>
internal readonly record struct PerfAppState(bool Previewing, bool Recording, bool FlashbackActive);

// The experiment's only view of the app: a command sender plus the three reads it needs. The sender is the same shape
// ssctl and the MCP server already use for DiagnosticSession, so both tools drive one runner and tests drive it with a fake.
internal sealed class PerfAppLink
{
    private readonly Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> _send;

    public PerfAppLink(Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> send)
    {
        _send = send ?? throw new ArgumentNullException(nameof(send));
    }

    /// <summary>
    /// Sends one command and returns the parsed reply, success or not. A canceled token throws even when the transport
    /// answers a cancellation with a synthetic error response instead of an exception, so a Ctrl-C is never mistaken
    /// for a command failure.
    /// </summary>
    public async Task<PerfReply> CallAsync(
        string command,
        Dictionary<string, object?>? payload,
        CancellationToken cancellationToken,
        string? label = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = await _send(command, payload, null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var success = PerfJson.GetBool(root, "Success") ?? false;
        var message = PerfJson.GetString(root, "Message");
        if (string.IsNullOrWhiteSpace(message))
        {
            message = root.ValueKind == JsonValueKind.Object ? "no message" : "no response";
        }

        return new PerfReply(label ?? command, success, message, root);
    }

    /// <summary>Like <see cref="CallAsync"/> but a refused command throws, naming the command and the app's reason.</summary>
    public async Task<PerfReply> RequireAsync(
        string command,
        Dictionary<string, object?>? payload,
        CancellationToken cancellationToken,
        string? label = null)
    {
        var reply = await CallAsync(command, payload, cancellationToken, label).ConfigureAwait(false);
        if (!reply.Success)
        {
            throw new PerfExperimentCommandException($"{reply.Label} failed: {reply.Message}");
        }

        return reply;
    }

    public async Task<PerfAppState> GetStateAsync(CancellationToken cancellationToken)
    {
        var reply = await RequireAsync("GetSnapshot", null, cancellationToken).ConfigureAwait(false);
        var snapshot = reply.Snapshot ?? reply.Data;
        if (snapshot is not { } state || state.ValueKind != JsonValueKind.Object)
        {
            throw new PerfExperimentCommandException("GetSnapshot failed: the response carried no app snapshot.");
        }

        return new PerfAppState(
            PerfJson.GetBool(state, "IsPreviewing") ?? false,
            PerfJson.GetBool(state, "IsRecording") ?? false,
            PerfJson.GetBool(state, "FlashbackActive") ?? false);
    }
}
