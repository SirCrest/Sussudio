using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace ElgatoCapture.Services;

public class DeviceService
{
    // Allowlist of supported capture devices
    private static readonly string[] AllowedDevices = new[]
    {
        "Game Capture Neo",
        "HD60 S+",
        "HD60 X",
        "4K60 Pro",
        "4K X",
        "4K S",
    };

    public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync()
    {
        var devices = new ObservableCollection<CaptureDevice>();

        var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        var audioDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);

        foreach (var videoDevice in videoDevices)
        {
            // Check if device is in allowlist
            bool isAllowed = AllowedDevices.Any(allowedName =>
                videoDevice.Name.Contains(allowedName, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                Logger.Log($"Skipping non-allowed device: {videoDevice.Name}");
                continue;
            }

            var captureDevice = new CaptureDevice
            {
                Id = videoDevice.Id,
                Name = videoDevice.Name
            };

            Logger.Log($"Found video device: {videoDevice.Name}");

            // Try to find associated audio device
            // Match patterns: "Elgato 4K X", "Elgato Game Capture Neo", "Elgato HD60 S+", etc.
            var associatedAudio = audioDevices.FirstOrDefault(a =>
            {
                // Both must contain "Elgato"
                if (!a.Name.Contains("Elgato", StringComparison.OrdinalIgnoreCase) ||
                    !videoDevice.Name.Contains("Elgato", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Try to match specific model names
                var modelNames = new[] { "4K X", "4K S", "HD60", "Neo", "HD60 S+", "HD60 Pro" };
                foreach (var model in modelNames)
                {
                    if (videoDevice.Name.Contains(model, StringComparison.OrdinalIgnoreCase) &&
                        a.Name.Contains(model, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            });

            if (associatedAudio != null)
            {
                captureDevice.AudioDeviceId = associatedAudio.Id;
                captureDevice.AudioDeviceName = associatedAudio.Name;
                Logger.Log($"  ✓ Found audio device: {associatedAudio.Name}");
            }
            else
            {
                Logger.Log($"  ✗ No audio device found for {videoDevice.Name}");
                Logger.Log($"  Available audio devices:");
                foreach (var audioDevice in audioDevices)
                {
                    Logger.Log($"    - {audioDevice.Name}");
                }
            }

            // Query supported formats
            await QuerySupportedFormatsAsync(captureDevice);

            devices.Add(captureDevice);
        }

        return devices;
    }

    private async Task QuerySupportedFormatsAsync(CaptureDevice device)
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

            var uniqueFormats = new HashSet<string>();

            foreach (var prop in availableMediaStreamProperties)
            {
                if (prop is VideoEncodingProperties videoProps)
                {
                    var frameRate = videoProps.FrameRate.Numerator > 0 && videoProps.FrameRate.Denominator > 0
                        ? (double)videoProps.FrameRate.Numerator / videoProps.FrameRate.Denominator
                        : 0;

                    if (videoProps.Width > 0 && videoProps.Height > 0 && frameRate > 0)
                    {
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
                            FrameRate = Math.Round(frameRate),
                            PixelFormat = videoProps.Subtype,
                            IsHdr = isHdr
                        };

                        var formatKey = $"{format.Width}x{format.Height}@{format.FrameRate}{(format.IsHdr ? "_HDR" : "")}";
                        if (uniqueFormats.Add(formatKey))
                        {
                            device.SupportedFormats.Add(format);
                        }
                    }
                }
            }

            // Sort formats by resolution (descending) then frame rate (descending)
            var sortedFormats = device.SupportedFormats
                .OrderByDescending(f => f.Width * f.Height)
                .ThenByDescending(f => f.FrameRate)
                .ToList();

            device.SupportedFormats.Clear();
            foreach (var format in sortedFormats)
            {
                device.SupportedFormats.Add(format);
            }

            mediaCapture.Dispose();
        }
        catch (Exception)
        {
            // Device may not be accessible; add some default formats
            device.SupportedFormats.Add(new MediaFormat { Width = 1920, Height = 1080, FrameRate = 60, PixelFormat = "NV12" });
            device.SupportedFormats.Add(new MediaFormat { Width = 1920, Height = 1080, FrameRate = 30, PixelFormat = "NV12" });
        }
    }

    public async Task<List<AudioInputDevice>> EnumerateAudioCaptureDevicesAsync()
    {
        var audioDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
        return audioDevices.Select(d => new AudioInputDevice
        {
            Id = d.Id,
            Name = d.Name
        }).ToList();
    }
}
