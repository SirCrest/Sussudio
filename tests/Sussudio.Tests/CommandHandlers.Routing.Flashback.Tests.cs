using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteFlashbackCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var flashbackPipeName = $"ssctl-flashback-{Guid.NewGuid():N}";
        var flashbackArguments = new List<string> { "flashback", "off" };
        var (flashbackExitCode, flashbackRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackPipeName,
                flashbackArguments)
            .ConfigureAwait(false);

        AssertEqual(0, flashbackExitCode, "flashback off exit code");
        AssertEqual(47, flashbackRequest.GetProperty("command").GetInt32(), "flashback off command id");
        AssertEqual(
            false,
            flashbackRequest.GetProperty("payload").GetProperty("enabled").GetBoolean(),
            "flashback off payload enabled");

        var flashbackExportPipeName = $"ssctl-flashback-export-{Guid.NewGuid():N}";
        var flashbackExportOutputPath = Path.Combine("temp", "ssctl flashback export", "export with spaces.mp4");
        var flashbackExportArguments = new List<string>
        {
            "flashback",
            "export",
            "--range",
            "--force",
            "2.5",
            flashbackExportOutputPath,
        };
        var (flashbackExportExitCode, flashbackExportRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackExportPipeName,
                flashbackExportArguments)
            .ConfigureAwait(false);

        AssertEqual(0, flashbackExportExitCode, "flashback export exit code");
        AssertEqual(42, flashbackExportRequest.GetProperty("command").GetInt32(), "flashback export command id");
        var flashbackExportPayload = flashbackExportRequest.GetProperty("payload");
        AssertEqual(2.5d, flashbackExportPayload.GetProperty("seconds").GetDouble(), "flashback export payload seconds");
        AssertEqual(
            flashbackExportOutputPath,
            flashbackExportPayload.GetProperty("outputPath").GetString(),
            "flashback export payload path");
        AssertEqual(
            true,
            flashbackExportPayload.GetProperty("useSelectionRange").GetBoolean(),
            "flashback export payload range");
        AssertEqual(true, flashbackExportPayload.GetProperty("force").GetBoolean(), "flashback export payload force");
        AssertEqual(
            true,
            Directory.Exists(Path.GetDirectoryName(flashbackExportOutputPath) ?? "."),
            "flashback export parent directory created");
    }
}
