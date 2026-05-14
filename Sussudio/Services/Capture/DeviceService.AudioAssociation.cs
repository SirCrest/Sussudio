using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public partial class DeviceService
{
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

    private static readonly Regex TokenizeRegex = new("[A-Za-z0-9\\+]+", RegexOptions.Compiled);

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
        foreach (Match match in TokenizeRegex.Matches(text))
        {
            var token = match.Value.Trim();
            if (token.Length >= 2)
            {
                tokens.Add(token);
            }
        }

        return tokens;
    }

}
