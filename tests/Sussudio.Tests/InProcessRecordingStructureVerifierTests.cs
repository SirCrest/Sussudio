using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class InProcessRecordingStructureVerifierTests
    : IClassFixture<InProcessRecordingStructureVerifierTests.BundledRuntime>
{
    private readonly BundledRuntime _runtime;

    public InProcessRecordingStructureVerifierTests(BundledRuntime runtime) => _runtime = runtime;

    // Literal boundary examples protect the recording contract without reproducing
    // its tolerance calculation. All videos are 64x64; fixture durations are documented.
    private static readonly Scenario[] Scenarios =
    {
        new("valid-video", "h264-video.mp4"),
        new("valid-program", "h264-program.mp4", Audio: true, RequestedTracks: "video,device_audio", ObservedTracks: "video,device_audio"),
        new("valid-microphone", "h264-program.mp4", Microphone: true, RequestedTracks: "video,microphone", ObservedTracks: "video,microphone"),
        new("valid-dual-audio", "h264-dual.mp4", Audio: true, Microphone: true, RequestedTracks: "video,device_audio,microphone", ObservedTracks: "video,device_audio,microphone"),
        new("valid-hevc-hdr", "hevc-hdr-explicit.mp4", Format: "HevcMp4", Hdr: true),
        new("valid-hevc-sdr", "hevc-sdr.mp4", Format: "HevcMp4"),
        new("valid-av1", "av1-video.mp4", Format: "Av1Mp4"),
        new("codec-mismatch", "h264-video.mp4", "recording-video-codec-mismatch", Format: "HevcMp4"),
        new("dimension-mismatch", "h264-video.mp4", "recording-video-dimensions-mismatch", Width: 128),
        new("hdr-metadata-missing", "hevc-sdr.mp4", "recording-hdr-metadata-mismatch", Format: "HevcMp4", Hdr: true),
        new("program-missing", "h264-video.mp4", "recording-stream-topology-mismatch", Audio: true, RequestedTracks: "video,device_audio"),
        new("microphone-missing", "h264-program.mp4", "recording-stream-topology-mismatch", Audio: true, Microphone: true, RequestedTracks: "video,device_audio,microphone", ObservedTracks: "video,device_audio"),
        new("unrequested-audio", "h264-program.mp4", "recording-stream-topology-mismatch", ObservedTracks: null, ObservedTrackCount: 2),
        new("unrequested-second-audio", "h264-dual.mp4", "recording-stream-topology-mismatch", Audio: true, RequestedTracks: "video,device_audio", ObservedTracks: null, ObservedTrackCount: 3),
        new("missing-file", null, "recording-output-missing", ObservedTracks: ""),
        new("empty-file", "empty.mp4", "recording-output-empty", ObservedTracks: ""),
        new("corrupt-container", "garbage.mp4", "recording-reopen-failed", ObservedTracks: ""),
        new("metadata-without-packets", "header-without-packets.mp4", "recording-required-stream-has-no-packets"),
        new("shortfall-floor-equality", "h264-video.mp4", ExpectedSeconds: 2.25),
        new("shortfall-floor-exceeded", "h264-video.mp4", "recording-video-duration-short", ExpectedSeconds: 2.2501),
        new("shortfall-percentage-equality", "h264-percent-boundary.mp4", ExpectedSeconds: 10),
        new("shortfall-percentage-exceeded", "h264-percent-boundary.mp4", "recording-video-duration-short", ExpectedSeconds: 10.0001),
        new("shortfall-cap-equality", "h264-cap-boundary.mp4", ExpectedSeconds: 44),
        new("shortfall-cap-exceeded", "h264-cap-boundary.mp4", "recording-video-duration-short", ExpectedSeconds: 44.0001),
        new("quarter-second-interval", "h264-floor-boundary.mp4", ExpectedSeconds: 0.25),
        new("audio-short-equality", "h264-audio-short-boundary.mp4", Audio: true, RequestedTracks: "video,device_audio", ObservedTracks: "video,device_audio"),
        new("audio-short-exceeded", "h264-audio-too-short.mp4", "recording-audio-duration-mismatch", Audio: true, RequestedTracks: "video,device_audio", ObservedTracks: "video,device_audio"),
        new("audio-long-equality", "h264-audio-long-boundary.mp4", Audio: true, RequestedTracks: "video,device_audio", ObservedTracks: "video,device_audio"),
        new("audio-long-exceeded", "h264-audio-too-long.mp4", "recording-audio-duration-mismatch", Audio: true, RequestedTracks: "video,device_audio", ObservedTracks: "video,device_audio")
    };

    public static IEnumerable<object[]> ScenarioNames => Scenarios.Select(scenario => new object[] { scenario.Name });

    [Theory]
    [MemberData(nameof(ScenarioNames))]
    public void VerifiesActualMediaAndReleasesUnchangedInput(string scenarioName)
    {
        var scenario = Scenarios.Single(item => item.Name == scenarioName);
        var directory = Directory.CreateTempSubdirectory("sussudio-structure-verifier-");
        var path = Path.Combine(directory.FullName, scenario.File ?? "missing.mp4");
        try
        {
            byte[]? original = null;
            if (scenario.File is { } fileName)
            {
                original = File.ReadAllBytes(Path.Combine(_runtime.FixtureDirectory, fileName));
                Assert.Equal(_runtime.FixtureHashes[fileName], Convert.ToHexString(SHA256.HashData(original)));
                File.WriteAllBytes(path, original);
            }

            var settings = Activator.CreateInstance(_runtime.Type("Sussudio.Models.CaptureSettings"))!;
            Set(settings, "Format", Enum.Parse(_runtime.Type("Sussudio.Models.RecordingFormat"), scenario.Format));
            Set(settings, "AudioEnabled", scenario.Audio);
            Set(settings, "MicrophoneEnabled", scenario.Microphone);
            var context = Activator.CreateInstance(_runtime.Type("Sussudio.Services.Contracts.RecordingContext"))!;
            Set(context, "Settings", settings);
            Set(context, "FinalOutputPath", path);
            Set(context, "VideoOutputPath", path);
            Set(context, "EffectiveWidth", scenario.Width);
            Set(context, "EffectiveHeight", 64u);
            Set(context, "EffectiveFrameRate", 50d);
            Set(context, "HdrPipelineActive", scenario.Hdr);

            var verifierType = _runtime.Type("Sussudio.Services.Recording.InProcessRecordingStructureVerifier");
            var verifier = Activator.CreateInstance(verifierType, nonPublic: true)!;
            var result = verifierType.GetMethod("Verify")!.Invoke(verifier, new object?[]
            {
                context,
                scenario.ExpectedSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null
            })!;

            Assert.Equal(scenario.FailureCode == null, Read<bool>(result, "Succeeded"));
            Assert.Equal(scenario.FailureCode ?? "", Read<string>(result, "FailureCode"));
            Assert.False(string.IsNullOrWhiteSpace(Read<string>(result, "Detail")));
            Assert.Equal(original?.LongLength ?? 0, Read<long>(result, "OutputBytes"));
            Assert.Equal(SplitTracks(scenario.RequestedTracks), Read<IReadOnlyList<string>>(result, "RequestedTracks"));
            var observed = Read<IReadOnlyList<string>>(result, "ObservedTracks");
            if (scenario.ObservedTracks is { } expectedTracks)
            {
                Assert.Equal(SplitTracks(expectedTracks), observed);
            }
            else
            {
                // Extra audio has no requested source identity. Check its presence
                // without declaring that it came from the microphone or device.
                Assert.Equal(scenario.ObservedTrackCount, observed.Count);
                Assert.Equal("video", observed[0]);
                Assert.Equal(observed.Count, observed.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            }

            if (original == null)
            {
                Assert.False(File.Exists(path));
            }
            else
            {
                Assert.Equal(SHA256.HashData(original), SHA256.HashData(File.ReadAllBytes(path)));
                using var exclusive = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                Assert.Equal(original.LongLength, exclusive.Length);
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string[] SplitTracks(string tracks) => tracks.Split(',', StringSplitOptions.RemoveEmptyEntries);
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;

    private sealed record Scenario(
        string Name, string? File, string? FailureCode = null, string Format = "H264Mp4",
        bool Audio = false, bool Microphone = false, bool Hdr = false, uint Width = 64,
        double? ExpectedSeconds = null, string RequestedTracks = "video", string? ObservedTracks = "video",
        int ObservedTrackCount = 0);

    public sealed class BundledRuntime
    {
        private readonly Assembly _assembly = SussudioAssembly.Load();
        public string FixtureDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "Fixtures", "RecordingStructure");
        public IReadOnlyDictionary<string, string> FixtureHashes { get; }

        public BundledRuntime()
        {
            var nativeDirectory = Path.Combine(Path.GetDirectoryName(_assembly.Location)!, "ffmpeg");
            var nativeManifest = Path.Combine(nativeDirectory, "manifest.json");
            Assert.True(File.Exists(nativeManifest), $"Build the app with its bundled native libav runtime: {nativeManifest}");
            using var native = JsonDocument.Parse(File.ReadAllText(nativeManifest));
            foreach (var library in native.RootElement.GetProperty("files").EnumerateArray())
            {
                var libraryPath = Path.Combine(nativeDirectory, library.GetProperty("fileName").GetString()!);
                Assert.True(File.Exists(libraryPath), $"Bundled native libav library is missing: {libraryPath}");
            }
            // Do not skip an unavailable runtime or let a malformed-media case pass
            // before libav has actually initialized. This also checks the binding ABI.
            Type("Sussudio.Services.Recording.LibAvEncoder").GetMethod("InitializeFFmpeg")!
                .Invoke(null, new object[] { true });

            using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureDirectory, "manifest.json")));
            FixtureHashes = fixtures.RootElement.EnumerateArray().ToDictionary(
                entry => entry.GetProperty("Name").GetString()!,
                entry => entry.GetProperty("Sha256").GetString()!, StringComparer.Ordinal);
        }

        internal Type Type(string name) => _assembly.GetType(name, throwOnError: true)!;
    }
}
