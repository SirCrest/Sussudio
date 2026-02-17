using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace ElgatoCapture.Services;

public class DeviceService
{
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

    private static readonly string[] AdditionalDeviceProperties =
    {
        "System.Devices.ContainerId",
        "System.Devices.DeviceInstanceId",
        "System.Devices.InterfaceClassGuid",
        "System.ItemNameDisplay"
    };

    public string LastDiscoverySummary { get; private set; } = "No discovery run yet";

    public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync()
    {
        var discovered = new ObservableCollection<CaptureDevice>();

        DeviceInformationCollection videoDevices;
        DeviceInformationCollection audioDevices;
        try
        {
            var videoFilter = DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass.VideoCapture);
            var audioFilter = DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass.AudioCapture);
            videoDevices = await DeviceInformation.FindAllAsync(videoFilter, AdditionalDeviceProperties);
            audioDevices = await DeviceInformation.FindAllAsync(audioFilter, AdditionalDeviceProperties);
        }
        catch (Exception ex)
        {
            LastDiscoverySummary = $"Video devices: enumeration failed ({ex.GetType().Name}: {ex.Message})";
            Logger.Log($"Device discovery failed while querying DeviceInformation: {ex}");
            return discovered;
        }

        if (videoDevices.Count == 0)
        {
            Logger.Log("Device discovery returned zero video devices. Check camera privacy permissions and elevated/runtime context.");
        }

        var evaluated = new List<DeviceCandidate>();
        foreach (var videoDevice in videoDevices)
        {
            var captureDevice = new CaptureDevice
            {
                Id = videoDevice.Id,
                Name = videoDevice.Name
            };

            var hasEnumeratedFormats = await QuerySupportedFormatsAsync(captureDevice);
            var preferredByName = PreferredDeviceNames.Any(name =>
                videoDevice.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            var likelyByName = CaptureKeywords.Any(keyword =>
                videoDevice.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);

            var include = preferredByName || likelyByName || likelyByCapability || !hasEnumeratedFormats;

            evaluated.Add(new DeviceCandidate(
                videoDevice,
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
            AttachBestAudioDevice(candidate.Source, candidate.Device, audioDevices);
            discovered.Add(candidate.Device);
        }

        var filteredOut = Math.Max(0, evaluated.Count - selected.Count);
        LastDiscoverySummary = $"Video devices: total={videoDevices.Count}, accepted={discovered.Count}, filtered={filteredOut}, audio inputs={audioDevices.Count}";
        Logger.Log($"Device discovery summary: {LastDiscoverySummary}");

        return discovered;
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
        DeviceInformation videoDevice,
        CaptureDevice captureDevice,
        IReadOnlyList<DeviceInformation> audioDevices)
    {
        var bestMatch = audioDevices
            .Select(audioDevice => new
            {
                Device = audioDevice,
                Score = ScoreAudioAssociation(videoDevice, audioDevice)
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

    private static int ScoreAudioAssociation(DeviceInformation videoDevice, DeviceInformation audioDevice)
    {
        var score = 0;

        var videoContainer = GetContainerId(videoDevice);
        var audioContainer = GetContainerId(audioDevice);
        if (!string.IsNullOrWhiteSpace(videoContainer) &&
            string.Equals(videoContainer, audioContainer, StringComparison.OrdinalIgnoreCase))
        {
            score += 200;
        }

        var videoTokens = Tokenize(videoDevice.Name);
        var audioTokens = Tokenize(audioDevice.Name);
        var overlap = videoTokens.Intersect(audioTokens).Count();
        score += overlap * 20;

        if (videoDevice.Name.Contains("Elgato", StringComparison.OrdinalIgnoreCase) &&
            audioDevice.Name.Contains("Elgato", StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        var videoModel = GetModelHint(videoDevice.Name);
        var audioModel = GetModelHint(audioDevice.Name);
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

    private static string? GetContainerId(DeviceInformation device)
    {
        if (!device.Properties.TryGetValue("System.Devices.ContainerId", out var value) || value == null)
        {
            return null;
        }

        return value.ToString();
    }

    private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)
    {
        try
        {
            var mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = device.Id,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };

            await mediaCapture.InitializeAsync(settings);

            var videoController = mediaCapture.VideoDeviceController;
            var availableMediaStreamProperties = videoController.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord);

            var uniqueFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in availableMediaStreamProperties)
            {
                if (prop is not VideoEncodingProperties videoProps)
                {
                    continue;
                }

                var numerator = videoProps.FrameRate.Numerator;
                var denominator = videoProps.FrameRate.Denominator;
                var frameRate = numerator > 0 && denominator > 0
                    ? (double)numerator / denominator
                    : 0;

                if (videoProps.Width <= 0 || videoProps.Height <= 0 || frameRate <= 0)
                {
                    continue;
                }

                var isHdr = videoProps.Subtype.Contains("P010", StringComparison.OrdinalIgnoreCase) ||
                           videoProps.Subtype.Contains("HDR", StringComparison.OrdinalIgnoreCase);

                if (isHdr)
                {
                    device.IsHdrCapable = true;
                }

                var format = new MediaFormat
                {
                    Width = videoProps.Width,
                    Height = videoProps.Height,
                    FrameRate = frameRate,
                    FrameRateNumerator = numerator,
                    FrameRateDenominator = denominator,
                    PixelFormat = videoProps.Subtype,
                    IsHdr = isHdr
                };

                var formatKey = $"{format.Width}x{format.Height}@{format.FrameRateNumerator}/{format.FrameRateDenominator}_{format.PixelFormat}_{(format.IsHdr ? "HDR" : "SDR")}";
                if (uniqueFormats.Add(formatKey))
                {
                    device.SupportedFormats.Add(format);
                }
            }

            var sortedFormats = device.SupportedFormats
                .OrderByDescending(f => (long)f.Width * f.Height)
                .ThenByDescending(f => f.FrameRate)
                .ThenBy(f => MediaFormat.GetPixelFormatPriority(f.PixelFormat))
                .ToList();

            device.SupportedFormats.Clear();
            foreach (var format in sortedFormats)
            {
                device.SupportedFormats.Add(format);
            }

            mediaCapture.Dispose();
            return sortedFormats.Count > 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"Format discovery failed for {device.Name}: {ex.Message}");
            device.SupportedFormats.Add(new MediaFormat
            {
                Width = 1920,
                Height = 1080,
                FrameRate = 60,
                FrameRateNumerator = 60,
                FrameRateDenominator = 1,
                PixelFormat = "NV12"
            });
            device.SupportedFormats.Add(new MediaFormat
            {
                Width = 1920,
                Height = 1080,
                FrameRate = 30,
                FrameRateNumerator = 30,
                FrameRateDenominator = 1,
                PixelFormat = "NV12"
            });
            return false;
        }
    }

    public async Task<List<AudioInputDevice>> EnumerateAudioCaptureDevicesAsync()
    {
        var audioFilter = DeviceInformation.GetAqsFilterFromDeviceClass(DeviceClass.AudioCapture);
        var audioDevices = await DeviceInformation.FindAllAsync(audioFilter, AdditionalDeviceProperties);
        return audioDevices.Select(d => new AudioInputDevice
        {
            Id = d.Id,
            Name = d.Name
        }).ToList();
    }

    private sealed record DeviceCandidate(
        DeviceInformation Source,
        CaptureDevice Device,
        bool HasEnumeratedFormats,
        bool Include,
        bool PreferredByName,
        bool LikelyByCapability,
        bool LikelyByName);
}
