using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

internal sealed class CachedMediaFormat
{
    public uint Width { get; set; }
    public uint Height { get; set; }
    public double FrameRate { get; set; }
    public uint FrameRateNumerator { get; set; }
    public uint FrameRateDenominator { get; set; }
    public string PixelFormat { get; set; } = string.Empty;
    public bool IsHdr { get; set; }
}

internal sealed class DeviceFormatCacheFile
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool IsHdrCapable { get; set; }
    public DateTimeOffset CachedAtUtc { get; set; }
    public int FormatCount { get; set; }
    public List<CachedMediaFormat> Formats { get; set; } = new();
}

[JsonSerializable(typeof(DeviceFormatCacheFile))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class DeviceFormatCacheJsonContext : JsonSerializerContext;

public class DeviceService
{
    private const int FormatProbeConcurrency = 2;
    private readonly SemaphoreSlim _formatProbeGate = new(FormatProbeConcurrency, FormatProbeConcurrency);

    private static readonly string[] PreferredDeviceNames =
    {
        "Game Capture Neo",
        "HD60 S+",
        "HD60 X",
        "4K60 Pro",
        "4K X",
        "4K S",
    };

    private static readonly string[] CaptureKeywords =
    {
        "elgato",
        "capture",
        "hdmi",
        "4k",
        "stream",
        "usb video"
    };

    private static readonly string[] ModelHints =
    {
        "4k x",
        "4k s",
        "4k60",
        "hd60 s+",
        "hd60",
        "neo",
        "pro"
    };

    private static readonly Regex DshowMinMaxRegex = new(
        @"(?:pixel_format|vcodec)=(?<pix>[^\s,]+).*?min s=(?<minw>\d+)x(?<minh>\d+) fps=(?<minfps>[\d\.]+).*?max s=(?<maxw>\d+)x(?<maxh>\d+) fps=(?<maxfps>[\d\.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DshowSingleRegex = new(
        @"(?:pixel_format|vcodec)=(?<pix>[^\s,]+).*?s=(?<w>\d+)x(?<h>\d+).*?fps=(?<fps>[\d\.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string LastDiscoverySummary { get; private set; } = "No discovery run yet";
    public event EventHandler<DeviceFormatProbeCompletedEventArgs>? FormatProbeCompleted;

    public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(bool waitForFormatProbes = true)
    {
        var discoveryStopwatch = Stopwatch.StartNew();
        var discovered = new ObservableCollection<CaptureDevice>();

        List<MfDeviceEnumerator.MfVideoDeviceInfo> videoDevices;
        List<AudioInputDevice> audioDevices;
        try
        {
            videoDevices = await MfDeviceEnumerator.EnumerateVideoDevicesAsync().ConfigureAwait(false);
            audioDevices = await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LastDiscoverySummary = $"Video devices: enumeration failed ({ex.GetType().Name}: {ex.Message})";
            Logger.Log($"Device discovery failed while querying MF/WASAPI enumerators: {ex}");
            return discovered;
        }

        if (videoDevices.Count == 0)
        {
            Logger.Log("Device discovery returned zero video devices from MFEnumDeviceSources.");
        }

        var evaluated = new List<DeviceCandidate>();
        foreach (var videoDevice in videoDevices)
        {
            var captureDevice = new CaptureDevice
            {
                Id = videoDevice.SymbolicLink,
                Name = videoDevice.Name
            };

            var hasEnumeratedFormats = false;
            if (waitForFormatProbes)
            {
                hasEnumeratedFormats = await QuerySupportedFormatsAsync(captureDevice);
            }
            else
            {
                TryLoadFormatCache(captureDevice);
            }

            var preferredByName = PreferredDeviceNames.Any(name =>
                captureDevice.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            var likelyByName = CaptureKeywords.Any(keyword =>
                captureDevice.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);

            var include = !waitForFormatProbes || preferredByName || likelyByName || likelyByCapability || !hasEnumeratedFormats;

            evaluated.Add(new DeviceCandidate(
                captureDevice.Name,
                captureDevice,
                hasEnumeratedFormats,
                include,
                preferredByName,
                likelyByCapability,
                likelyByName));
        }

        var selected = evaluated.Where(x => x.Include).ToList();
        if (selected.Count == 0)
        {
            Logger.Log("Capability-first filtering found no strong candidates; falling back to all video devices");
            selected = evaluated;
        }

        foreach (var candidate in selected.OrderByDescending(GetDevicePriority))
        {
            AttachBestAudioDevice(candidate.SourceName, candidate.Device, audioDevices);
            discovered.Add(candidate.Device);
        }

        var filteredOut = Math.Max(0, evaluated.Count - selected.Count);
        discoveryStopwatch.Stop();
        LastDiscoverySummary =
            $"Video devices: total={videoDevices.Count}, accepted={discovered.Count}, filtered={filteredOut}, audio inputs={audioDevices.Count}, " +
            $"first-list-ms={discoveryStopwatch.ElapsedMilliseconds}, format-probes={(waitForFormatProbes ? "inline" : "background")}";
        Logger.Log($"Device discovery summary: {LastDiscoverySummary}");

        return discovered;
    }

    public void BeginBackgroundFormatProbe(CaptureDevice device, long requestId = 0)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (string.IsNullOrWhiteSpace(device.Id) || string.IsNullOrWhiteSpace(device.Name))
        {
            return;
        }

        _ = RunBackgroundFormatProbeAsync(device.Id, device.Name, requestId);
    }

    private async Task RunBackgroundFormatProbeAsync(string deviceId, string deviceName, long requestId)
    {
        await _formatProbeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var probeDevice = new CaptureDevice
            {
                Id = deviceId,
                Name = deviceName
            };

            var hasEnumeratedFormats = await QuerySupportedFormatsAsync(probeDevice).ConfigureAwait(false);
            var snapshot = CloneFormats(probeDevice.SupportedFormats);
            if (hasEnumeratedFormats)
            {
                TrySaveFormatCache(probeDevice, snapshot);
            }

            FormatProbeCompleted?.Invoke(
                this,
                new DeviceFormatProbeCompletedEventArgs(
                    deviceId,
                    deviceName,
                    snapshot,
                    probeDevice.IsHdrCapable,
                    hasEnumeratedFormats,
                    requestId,
                    error: null));
        }
        catch (Exception ex)
        {
            Logger.Log($"Background format probe failed for {deviceName}: {ex.Message}");
            FormatProbeCompleted?.Invoke(
                this,
                new DeviceFormatProbeCompletedEventArgs(
                    deviceId,
                    deviceName,
                    Array.Empty<MediaFormat>(),
                    isHdrCapable: false,
                    hasEnumeratedFormats: false,
                    requestId,
                    error: ex.Message));
        }
        finally
        {
            _formatProbeGate.Release();
        }
    }

    private static IReadOnlyList<MediaFormat> CloneFormats(IEnumerable<MediaFormat> formats)
    {
        var clone = new List<MediaFormat>();
        foreach (var format in formats)
        {
            clone.Add(new MediaFormat
            {
                Width = format.Width,
                Height = format.Height,
                FrameRate = format.FrameRate,
                FrameRateNumerator = format.FrameRateNumerator,
                FrameRateDenominator = format.FrameRateDenominator,
                PixelFormat = format.PixelFormat,
                IsHdr = format.IsHdr
            });
        }

        return clone;
    }

    private static readonly string FormatCacheDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElgatoCapture");

    private static string SanitizeDeviceIdForFilename(string deviceId)
    {
        var sb = new StringBuilder(deviceId.Length);
        foreach (var ch in deviceId)
        {
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        }

        var sanitized = sb.ToString();
        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }

    private static string GetCacheFilePath(string deviceId)
    {
        return Path.Combine(FormatCacheDirectory, $"format_cache_{SanitizeDeviceIdForFilename(deviceId)}.json");
    }

    private static void TryDeleteFormatCache(string deviceId)
    {
        try
        {
            var path = GetCacheFilePath(deviceId);
            if (File.Exists(path))
            {
                File.Delete(path);
                Logger.Log($"FORMAT_CACHE: deleted corrupt cache for {deviceId}");
            }
        }
        catch
        {
            // Best-effort delete
        }
    }

    private static void TryLoadFormatCache(CaptureDevice device)
    {
        try
        {
            var path = GetCacheFilePath(device.Id);
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path);
            var cache = JsonSerializer.Deserialize(json, DeviceFormatCacheJsonContext.Default.DeviceFormatCacheFile);
            if (cache == null || cache.Formats.Count == 0)
            {
                TryDeleteFormatCache(device.Id);
                return;
            }

            foreach (var cached in cache.Formats)
            {
                device.SupportedFormats.Add(new MediaFormat
                {
                    Width = cached.Width,
                    Height = cached.Height,
                    FrameRate = cached.FrameRate,
                    FrameRateNumerator = cached.FrameRateNumerator,
                    FrameRateDenominator = cached.FrameRateDenominator,
                    PixelFormat = cached.PixelFormat,
                    IsHdr = cached.IsHdr
                });
            }

            device.IsHdrCapable = cache.IsHdrCapable;
            Logger.Log($"FORMAT_CACHE: warm from cache for {device.Name}: {cache.Formats.Count} formats (cached {cache.CachedAtUtc:u})");
        }
        catch (Exception ex)
        {
            Logger.Log($"FORMAT_CACHE: failed to load cache for {device.Name} ({ex.GetType().Name}: {ex.Message}), deleting");
            TryDeleteFormatCache(device.Id);
        }
    }

    private static void TrySaveFormatCache(CaptureDevice device, IReadOnlyList<MediaFormat> formats)
    {
        try
        {
            Directory.CreateDirectory(FormatCacheDirectory);
            var cache = new DeviceFormatCacheFile
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                IsHdrCapable = device.IsHdrCapable,
                CachedAtUtc = DateTimeOffset.UtcNow,
                FormatCount = formats.Count,
                Formats = new List<CachedMediaFormat>(formats.Count)
            };

            foreach (var fmt in formats)
            {
                cache.Formats.Add(new CachedMediaFormat
                {
                    Width = fmt.Width,
                    Height = fmt.Height,
                    FrameRate = fmt.FrameRate,
                    FrameRateNumerator = fmt.FrameRateNumerator,
                    FrameRateDenominator = fmt.FrameRateDenominator,
                    PixelFormat = fmt.PixelFormat,
                    IsHdr = fmt.IsHdr
                });
            }

            var json = JsonSerializer.Serialize(cache, DeviceFormatCacheJsonContext.Default.DeviceFormatCacheFile);
            var path = GetCacheFilePath(device.Id);
            File.WriteAllText(path, json);
            Logger.Log($"FORMAT_CACHE: saved {formats.Count} formats for {device.Name}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FORMAT_CACHE: failed to save cache for {device.Name} ({ex.GetType().Name}: {ex.Message})");
        }
    }

    private static int GetDevicePriority(DeviceCandidate candidate)
    {
        var maxPixelCount = candidate.Device.SupportedFormats
            .Select(f => (long)f.Width * f.Height)
            .DefaultIfEmpty(0)
            .Max();
        var maxFrameRate = candidate.Device.SupportedFormats
            .Select(f => f.FrameRate)
            .DefaultIfEmpty(0)
            .Max();

        var priority = 0;
        if (candidate.PreferredByName) priority += 400;
        if (candidate.LikelyByCapability) priority += 200;
        if (candidate.LikelyByName) priority += 100;
        if (candidate.HasEnumeratedFormats) priority += 50;
        priority += (int)Math.Min(maxFrameRate, 120);
        priority += (int)Math.Min(maxPixelCount / 500_000, 40);
        return priority;
    }

    private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)
    {
        foreach (var format in device.SupportedFormats)
        {
            if ((format.Width >= 1920 && format.FrameRate >= 50) ||
                (format.Width >= 2560 && format.FrameRate >= 30) ||
                (format.Width >= 3840 && format.FrameRate >= 24))
            {
                return true;
            }
        }

        return false;
    }

    private static void AttachBestAudioDevice(
        string videoDeviceName,
        CaptureDevice captureDevice,
        IReadOnlyList<AudioInputDevice> audioDevices)
    {
        var bestMatch = audioDevices
            .Select(audioDevice => new
            {
                Device = audioDevice,
                Score = ScoreAudioAssociation(videoDeviceName, audioDevice.Name)
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (bestMatch == null || bestMatch.Score <= 0)
        {
            Logger.Log($"No associated audio device found for {captureDevice.Name}");
            return;
        }

        captureDevice.AudioDeviceId = bestMatch.Device.Id;
        captureDevice.AudioDeviceName = bestMatch.Device.Name;
        Logger.Log($"Associated audio device for {captureDevice.Name}: {bestMatch.Device.Name} (score={bestMatch.Score})");
    }

    private static int ScoreAudioAssociation(string videoDeviceName, string audioDeviceName)
    {
        var score = 0;

        var videoTokens = Tokenize(videoDeviceName);
        var audioTokens = Tokenize(audioDeviceName);
        var overlap = videoTokens.Intersect(audioTokens).Count();
        score += overlap * 20;

        if (videoDeviceName.Contains("Elgato", StringComparison.OrdinalIgnoreCase) &&
            audioDeviceName.Contains("Elgato", StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        var videoModel = GetModelHint(videoDeviceName);
        var audioModel = GetModelHint(audioDeviceName);
        if (!string.IsNullOrEmpty(videoModel) &&
            string.Equals(videoModel, audioModel, StringComparison.OrdinalIgnoreCase))
        {
            score += 50;
        }

        return score;
    }

    private static string? GetModelHint(string deviceName)
    {
        foreach (var modelHint in ModelHints)
        {
            if (deviceName.Contains(modelHint, StringComparison.OrdinalIgnoreCase))
            {
                return modelHint;
            }
        }

        return null;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(text, "[A-Za-z0-9\\+]+"))
        {
            var token = match.Value.Trim();
            if (token.Length >= 2)
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

    private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)
    {
        try
        {
            var uniqueFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            device.IsHdrCapable = false;
            device.SupportedFormats.Clear();

            var nativeFormats = await MfDeviceEnumerator.ProbeVideoFormatsAsync(device.Id).ConfigureAwait(false);
            foreach (var nativeFormat in nativeFormats)
            {
                var width = nativeFormat.Width;
                var height = nativeFormat.Height;
                if (width == 0 || height == 0)
                {
                    continue;
                }

                var rawFps = nativeFormat.FrameRate;
                if (rawFps <= 0 &&
                    nativeFormat.FrameRateNumerator > 0 &&
                    nativeFormat.FrameRateDenominator > 0)
                {
                    rawFps = (double)nativeFormat.FrameRateNumerator / nativeFormat.FrameRateDenominator;
                }

                if (rawFps <= 0)
                {
                    continue;
                }

                var pixelFormat = NormalizePixelFormat(nativeFormat.PixelFormat);
                var (numerator, denominator, normalizedFps) = NormalizeFrameRate(rawFps);
                var isHdr = MediaFormat.IsHdrPixelFormat(pixelFormat) || MediaFormat.IsTrue10BitPixelFormat(pixelFormat);
                if (isHdr)
                {
                    device.IsHdrCapable = true;
                }

                var key = $"{width}x{height}@{numerator}/{denominator}_{pixelFormat}_{(isHdr ? "HDR" : "SDR")}";
                if (!uniqueFormats.Add(key))
                {
                    continue;
                }

                device.SupportedFormats.Add(new MediaFormat
                {
                    Width = width,
                    Height = height,
                    FrameRate = normalizedFps,
                    FrameRateNumerator = numerator,
                    FrameRateDenominator = denominator,
                    PixelFormat = pixelFormat,
                    IsHdr = isHdr
                });
            }

            var sortedFormats = device.SupportedFormats
                .OrderByDescending(f => (long)f.Width * f.Height)
                .ThenByDescending(f => f.FrameRate)
                .ThenBy(f => MediaFormat.GetPixelFormatPriority(f.PixelFormat))
                .ToList();

            if (sortedFormats.Count == 0)
            {
                Logger.Log($"MF source-reader format discovery produced no rows for {device.Name}.");
            }

            device.SupportedFormats.Clear();
            foreach (var format in sortedFormats)
            {
                device.SupportedFormats.Add(format);
            }

            return sortedFormats.Count > 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"Format discovery failed for {device.Name}: {ex.Message}");
            device.SupportedFormats.Clear();
            device.IsHdrCapable = false;
            return false;
        }
    }

    private static void ParseDshowOptions(CaptureDevice device, string output, HashSet<string> uniqueFormats)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return;
        }

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.Contains("fps=", StringComparison.OrdinalIgnoreCase) ||
                !line.Contains("s=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var minMax = DshowMinMaxRegex.Match(line);
            if (minMax.Success)
            {
                var pixelFormat = NormalizePixelFormat(minMax.Groups["pix"].Value);
                AddFormatFromMatch(device, uniqueFormats, pixelFormat, minMax.Groups["minw"].Value, minMax.Groups["minh"].Value, minMax.Groups["minfps"].Value);
                AddFormatFromMatch(device, uniqueFormats, pixelFormat, minMax.Groups["maxw"].Value, minMax.Groups["maxh"].Value, minMax.Groups["maxfps"].Value);
                continue;
            }

            var single = DshowSingleRegex.Match(line);
            if (single.Success)
            {
                var pixelFormat = NormalizePixelFormat(single.Groups["pix"].Value);
                AddFormatFromMatch(device, uniqueFormats, pixelFormat, single.Groups["w"].Value, single.Groups["h"].Value, single.Groups["fps"].Value);
            }
        }
    }

    private static void AddFormatFromMatch(
        CaptureDevice device,
        HashSet<string> uniqueFormats,
        string pixelFormat,
        string widthRaw,
        string heightRaw,
        string fpsRaw)
    {
        if (!uint.TryParse(widthRaw, out var width) ||
            !uint.TryParse(heightRaw, out var height) ||
            !double.TryParse(fpsRaw, out var fps) ||
            width == 0 ||
            height == 0 ||
            fps <= 0)
        {
            return;
        }

        var (numerator, denominator, normalizedFps) = NormalizeFrameRate(fps);
        var isHdr = MediaFormat.IsHdrPixelFormat(pixelFormat) || MediaFormat.IsTrue10BitPixelFormat(pixelFormat);
        if (isHdr)
        {
            device.IsHdrCapable = true;
        }

        var key = $"{width}x{height}@{numerator}/{denominator}_{pixelFormat}_{(isHdr ? "HDR" : "SDR")}";
        if (!uniqueFormats.Add(key))
        {
            return;
        }

        device.SupportedFormats.Add(new MediaFormat
        {
            Width = width,
            Height = height,
            FrameRate = normalizedFps,
            FrameRateNumerator = numerator,
            FrameRateDenominator = denominator,
            PixelFormat = pixelFormat,
            IsHdr = isHdr
        });
    }

    private static (uint Numerator, uint Denominator, double Fps) NormalizeFrameRate(double fps)
    {
        if (fps <= 0)
        {
            return (30, 1, 30);
        }

        var rounded = Math.Round(fps);
        if (Math.Abs(fps - rounded) <= 0.01 && rounded > 0)
        {
            return ((uint)rounded, 1, rounded);
        }

        var ntscBase = Math.Round(fps * 1001.0 / 1000.0);
        var ntscFps = ntscBase * 1000.0 / 1001.0;
        if (ntscBase > 0 && Math.Abs(fps - ntscFps) <= 0.03)
        {
            return ((uint)(ntscBase * 1000), 1001, ntscFps);
        }

        var numerator = (uint)Math.Round(fps * 1000.0);
        return (numerator, 1000, numerator / 1000.0);
    }

    private static string NormalizePixelFormat(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "UNKNOWN";
        }

        var token = raw.Trim();
        if (token.Equals("yuyv422", StringComparison.OrdinalIgnoreCase))
        {
            return "YUY2";
        }

        if (token.Equals("uyvy422", StringComparison.OrdinalIgnoreCase))
        {
            return "UYVY";
        }

        if (token.Equals("p010le", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        return token.ToUpperInvariant();
    }

    private static async Task<string> RunProbeAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to launch FFmpeg for DirectShow format discovery.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return $"{stdout}{Environment.NewLine}{stderr}";
    }

    private static string EscapeDshowDeviceName(string deviceName)
    {
        var escaped = new StringBuilder(deviceName.Length);
        foreach (var ch in deviceName)
        {
            if (ch == '"')
            {
                escaped.Append("\\\"");
                continue;
            }

            escaped.Append(ch);
        }

        return escaped.ToString();
    }

    public async Task<List<AudioInputDevice>> EnumerateAudioCaptureDevicesAsync()
    {
        return await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync().ConfigureAwait(false);
    }

    private sealed record DeviceCandidate(
        string SourceName,
        CaptureDevice Device,
        bool HasEnumeratedFormats,
        bool Include,
        bool PreferredByName,
        bool LikelyByCapability,
        bool LikelyByName);

    public sealed class DeviceFormatProbeCompletedEventArgs : EventArgs
    {
        public DeviceFormatProbeCompletedEventArgs(
            string deviceId,
            string deviceName,
            IReadOnlyList<MediaFormat> formats,
            bool isHdrCapable,
            bool hasEnumeratedFormats,
            long requestId,
            string? error)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            Formats = formats;
            IsHdrCapable = isHdrCapable;
            HasEnumeratedFormats = hasEnumeratedFormats;
            RequestId = requestId;
            Error = error;
        }

        public string DeviceId { get; }
        public string DeviceName { get; }
        public IReadOnlyList<MediaFormat> Formats { get; }
        public bool IsHdrCapable { get; }
        public bool HasEnumeratedFormats { get; }
        public long RequestId { get; }
        public string? Error { get; }
        public bool Succeeded => string.IsNullOrWhiteSpace(Error);
    }
}
