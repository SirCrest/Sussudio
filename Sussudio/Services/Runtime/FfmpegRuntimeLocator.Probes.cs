using System;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Runtime;

internal static partial class FfmpegRuntimeLocator
{
    private static Task<EncoderSupport>? _encoderProbeTask;
    private static readonly object EncoderProbeLock = new();
    private static Task<SplitEncodeSupport>? _splitEncodeSupportTask;
    private static readonly object SplitEncodeSupportLock = new();
    private static readonly Lazy<string> CachedFfmpegPath = new(() => FindToolPath("ffmpeg.exe"));
    private const int ProbeTimeoutMs = 10_000;

    private readonly record struct ProbeCommandResult(
        string Output,
        int ExitCode,
        bool Started,
        bool TimedOut);

    public static Task<EncoderSupport> GetEncoderSupportAsync()
    {
        lock (EncoderProbeLock)
        {
            _encoderProbeTask ??= ProbeEncoderSupportAsync();
            return _encoderProbeTask;
        }
    }

    public static Task<SplitEncodeSupport> GetSplitEncodeSupportAsync()
    {
        lock (SplitEncodeSupportLock)
        {
            _splitEncodeSupportTask ??= ProbeSplitEncodeSupportAsync();
            return _splitEncodeSupportTask;
        }
    }

    private static async Task<EncoderSupport> ProbeEncoderSupportAsync()
    {
        var ffmpegPath = CachedFfmpegPath.Value;
        try
        {
            var result = await RunProbeCommandAsync(ffmpegPath, "-hide_banner -encoders");
            if (!result.Started || result.TimedOut || result.ExitCode != 0)
            {
                Logger.Log(
                    "FFmpeg encoder probe did not complete successfully: " +
                    $"started={result.Started} timedOut={result.TimedOut} exitCode={result.ExitCode}");
                return EncoderSupport.Empty;
            }

            var output = result.Output;
            var support = new EncoderSupport
            {
                HasH264Nvenc = output.Contains("h264_nvenc"),
                HasHevcNvenc = output.Contains("hevc_nvenc"),
                HasAv1Nvenc = output.Contains("av1_nvenc"),
                HasLibX264 = output.Contains("libx264"),
                HasLibX265 = output.Contains("libx265"),
                HasLibSvtAv1 = output.Contains("libsvtav1"),
                HasLibAomAv1 = output.Contains("libaom-av1")
            };
            Logger.Log(
                $"Encoder support: H.264={support.HasH264} (nvenc={support.HasH264Nvenc}, x264={support.HasLibX264}), " +
                $"HEVC={support.HasHevc} (nvenc={support.HasHevcNvenc}, x265={support.HasLibX265}), " +
                $"AV1={support.HasAv1} (nvenc={support.HasAv1Nvenc}, svt={support.HasLibSvtAv1}, aom={support.HasLibAomAv1})");
            return support;
        }
        catch (Exception ex)
        {
            Logger.Log($"FFmpeg encoder probe failed: {ex.Message}");
            return EncoderSupport.Empty;
        }
    }

    private static async Task<SplitEncodeSupport> ProbeSplitEncodeSupportAsync()
    {
        var ffmpegPath = CachedFfmpegPath.Value;
        try
        {
            var twoWayTask = TestSplitEncodeModeAsync(ffmpegPath, 2);
            var threeWayTask = TestSplitEncodeModeAsync(ffmpegPath, 3);
            await Task.WhenAll(twoWayTask, threeWayTask).ConfigureAwait(false);
            var support = new SplitEncodeSupport(twoWayTask.Result, threeWayTask.Result);
            Logger.Log($"Split encode support: 2-way={support.Supports2Way}, 3-way={support.Supports3Way}");
            return support;
        }
        catch (Exception ex)
        {
            Logger.Log($"Split encode probe failed: {ex.Message}");
            return SplitEncodeSupport.NvencUnavailable;
        }
    }

    private static async Task<bool> TestSplitEncodeModeAsync(string ffmpegPath, int mode)
    {
        var probeArgs =
            "-hide_banner -loglevel error " +
            "-f lavfi -i color=size=16x16:rate=1:color=black:duration=1 " +
            "-c:v hevc_nvenc " +
            $"-split_encode_mode {mode} " +
            "-frames:v 1 -an -f null NUL";
        var result = await RunProbeCommandAsync(ffmpegPath, probeArgs).ConfigureAwait(false);
        return result.Started && !result.TimedOut && result.ExitCode == 0;
    }

    private static async Task<ProbeCommandResult> RunProbeCommandAsync(string fileName, string arguments)
    {
        var result = await new ProcessSupervisor().RunAsync(new ProcessSpec
        {
            FileName = fileName,
            Arguments = arguments,
            TimeoutMs = ProbeTimeoutMs
        }).ConfigureAwait(false);

        if (!result.Started)
        {
            return new ProbeCommandResult(string.Empty, -1, Started: false, TimedOut: false);
        }

        var output = result.StdOut + Environment.NewLine + result.StdErr;
        return new ProbeCommandResult(output, result.ExitCode ?? -1, Started: true, TimedOut: result.TimedOut);
    }
}
