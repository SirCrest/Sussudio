using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
}
