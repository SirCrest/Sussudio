using System.Threading.Tasks;

static partial class Program
{
    internal static async Task SsctlCommandHandlers_RouteFlashbackCommands()
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

        var flashbackSeekPipeName = $"ssctl-flashback-seek-{Guid.NewGuid():N}";
        var (flashbackSeekExitCode, flashbackSeekRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackSeekPipeName,
                new List<string> { "flashback", "seek", "1234.5" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackSeekExitCode, "flashback seek exit code");
        AssertSsctlCommandRequest(
            flashbackSeekRequest,
            "FlashbackAction",
            ("action", "seek"),
            ("positionMs", 1234.5d));

        var flashbackScrubPipeName = $"ssctl-flashback-scrub-{Guid.NewGuid():N}";
        var (flashbackScrubExitCode, flashbackScrubRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackScrubPipeName,
                new List<string> { "flashback", "begin-scrub", "250" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackScrubExitCode, "flashback begin-scrub exit code");
        AssertSsctlCommandRequest(
            flashbackScrubRequest,
            "FlashbackAction",
            ("action", "begin-scrub"),
            ("positionMs", 250d));

        var flashbackClearRangePipeName = $"ssctl-flashback-clear-range-{Guid.NewGuid():N}";
        var (flashbackClearRangeExitCode, flashbackClearRangeRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackClearRangePipeName,
                new List<string> { "flashback", "clear-range" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackClearRangeExitCode, "flashback clear-range exit code");
        AssertSsctlCommandRequest(
            flashbackClearRangeRequest,
            "FlashbackAction",
            ("action", "clear-in-out-points"));
    }

    internal static async Task SsctlCommandHandlers_RouteObservabilityCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var audioRampPipeName = $"ssctl-audio-ramp-trace-{Guid.NewGuid():N}";
        var audioRampArguments = new List<string> { "audio-ramp-trace" };
        var (audioRampExitCode, audioRampRequest) = await CaptureSsctlRequestAsync(
                context,
                audioRampPipeName,
                audioRampArguments)
            .ConfigureAwait(false);

        AssertEqual(0, audioRampExitCode, "audio-ramp-trace exit code");
        AssertSsctlCommandRequestHasEmptyPayload(audioRampRequest, "GetAudioRampTrace");
    }
}
