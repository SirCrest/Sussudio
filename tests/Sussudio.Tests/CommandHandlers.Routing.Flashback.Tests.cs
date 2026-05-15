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
        AssertSsctlCommandRequest(flashbackRequest, "SetFlashbackEnabled", ("enabled", false));

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
        AssertSsctlCommandRequest(
            flashbackExportRequest,
            "FlashbackExport",
            ("seconds", 2.5d),
            ("outputPath", flashbackExportOutputPath),
            ("useSelectionRange", true),
            ("force", true));
        AssertEqual(
            true,
            Directory.Exists(Path.GetDirectoryName(flashbackExportOutputPath) ?? "."),
            "flashback export parent directory created");
    }
}
