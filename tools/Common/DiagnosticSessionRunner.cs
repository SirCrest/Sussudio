using System.Text.Json;

namespace Sussudio.Tools;

public static class DiagnosticSessionRunner
{
    // Public compatibility wrapper. DiagnosticSessionRunExecution owns the
    // mutable phase plan; the scenario partial owns setup, sampling, and drains.
    public static Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        return DiagnosticSessionRunExecution.RunAsync(options, sendCommandAsync, cancellationToken);
    }

    public static string Format(DiagnosticSessionResult result)
    {
        return DiagnosticSessionResultFormatter.Format(result);
    }

}
