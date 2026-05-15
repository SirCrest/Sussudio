namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleVerifyAsync(CommandContext context)
    {
        var json = context.GlobalJson || ConsumeFlag(context.Rest, "--json");
        var verificationProfile =
            ParseOptionalStringFlag(context.Rest, "--profile") ??
            ParseOptionalStringFlag(context.Rest, "--verification-profile");
        if (context.Rest.Count > 0)
        {
            var filePath = JoinRemaining(context.Rest, 0);
            var payload = new Dictionary<string, object?> { ["filePath"] = filePath };
            if (!string.IsNullOrWhiteSpace(verificationProfile))
            {
                payload["verificationProfile"] = verificationProfile;
            }

            var response = await context.Transport.SendCommandAsync(
                "VerifyFile",
                payload,
                60000).ConfigureAwait(false);
            return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
        }
        else
        {
            // Verify last recording (existing behavior)
            var response = await context.Transport.SendCommandAsync("VerifyLastRecording").ConfigureAwait(false);
            return WriteResponse(response, json, value => Formatters.FormatResult(value, includeData: true));
        }
    }
}
