using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Serializable cache entry for one media mode exposed by a capture device.
// The cache avoids slow full Media Foundation enumeration on every startup,
// but DeviceService still refreshes from live devices when the cache is absent
// or stale.
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

public partial class DeviceService
{
    private static IReadOnlyList<MediaFormat> CloneFormats(IEnumerable<MediaFormat> formats)
        => formats.Select(f => new MediaFormat
        {
            Width = f.Width,
            Height = f.Height,
            FrameRate = f.FrameRate,
            FrameRateNumerator = f.FrameRateNumerator,
            FrameRateDenominator = f.FrameRateDenominator,
            PixelFormat = f.PixelFormat,
            IsHdr = f.IsHdr
        }).ToList();

    private static string GetFormatCacheDirectory()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sussudio");

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
        return Path.Combine(GetFormatCacheDirectory(), $"format_cache_{SanitizeDeviceIdForFilename(deviceId)}.json");
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in DeviceService.TryDeleteCorruptCache: {ex.Message}");
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
            Directory.CreateDirectory(GetFormatCacheDirectory());
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
                Name = deviceName,
                NativeXuInterfacePath = ResolveNativeXuInterfacePath(deviceId)
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
                    Error: null));
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
                    IsHdrCapable: false,
                    HasEnumeratedFormats: false,
                    requestId,
                    Error: ex.Message));
        }
        finally
        {
            _formatProbeGate.Release();
        }
    }

    private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)
    {
        try
        {
            var uniqueFormats = new HashSet<MediaFormat>();
            device.IsHdrCapable = false;

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

                uniqueFormats.Add(new MediaFormat
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

            var sortedFormats = uniqueFormats
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
}
