using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        // Coordinator must expose the audio monitoring API surface
        var setPreviewVolume = coordinatorType.GetMethod(
            "SetPreviewVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(setPreviewVolume, "CaptureSessionCoordinator.SetPreviewVolume");

        var updateAudioMonitoring = coordinatorType.GetMethod(
            "UpdateAudioMonitoringAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioMonitoring, "CaptureSessionCoordinator.UpdateAudioMonitoringAsync");

        var updateAudioInput = coordinatorType.GetMethod(
            "UpdateAudioInputAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioInput, "CaptureSessionCoordinator.UpdateAudioInputAsync");

        var startVideoPreview = coordinatorType.GetMethod(
            "StartVideoPreviewAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(startVideoPreview, "CaptureSessionCoordinator.StartVideoPreviewAsync");

        // The command kind enum must include UpdateAudioMonitoring
        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        AssertEqual(true,
            Enum.IsDefined(commandKindType, Enum.Parse(commandKindType, "SetAudioPreviewEnabled")),
            "AutomationCommandKind.SetAudioPreviewEnabled exists");

        return Task.CompletedTask;
    }

    private static Task MainViewModelCaptureSettings_OwnsSettingsProjection()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var captureSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSettings.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureSettingsText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureSettingsText, "RequestedPixelFormat = ResolveRequestedPixelFormat()");
        AssertContains(captureSettingsText, "ForceMjpegDecode = ShouldForceMjpegDecode()");
        AssertContains(captureSettingsText, "settings.UseCustomAudioInput = IsCustomAudioInputEnabled;");
        AssertContains(captureSettingsText, "settings.MicrophoneEnabled = IsMicrophoneEnabled;");
        AssertContains(captureSettingsText, "private string? ResolveRequestedPixelFormat()");
        AssertContains(captureSettingsText, "private bool ShouldForceMjpegDecode()");
        AssertDoesNotContain(captureText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureText, "await _sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken)");
        AssertContains(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");

        return Task.CompletedTask;
    }

    private static Dictionary<string, string> ReadMainViewModelCodeFiles()
    {
        return Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels"), "MainViewModel*.cs")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                path => Path.GetFileName(path),
                path => StripCSharpCommentsAndStringContents(File.ReadAllText(path).Replace("\r\n", "\n")),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ReadRepoCodeWithoutCommentsOrStrings(string relativePath)
        => StripCSharpCommentsAndStringContents(ReadRepoFile(relativePath).Replace("\r\n", "\n"));

    private static void AssertMemberContains(string source, string memberName, string token)
        => AssertContains(ExtractMemberCode(source, memberName), token);

    private static void AssertMemberDoesNotContain(string source, string memberName, string token)
        => AssertDoesNotContain(ExtractMemberCode(source, memberName), token);

    private static string ExtractMemberCode(string source, string memberName)
    {
        var match = Regex.Match(
            source,
            @"(?m)^\s*(?:(?:public|private|protected|internal|static|async|partial|override|virtual|sealed)\s+)*(?:[\w<>,\?\[\]\.]+\s+)+" +
            Regex.Escape(memberName) +
            @"\s*\(");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Member '{memberName}' was not found.");
        }

        var openBrace = source.IndexOf('{', match.Index);
        var arrow = source.IndexOf("=>", match.Index, StringComparison.Ordinal);
        var semicolon = source.IndexOf(';', match.Index);
        if (arrow >= 0 && semicolon >= 0 && (openBrace < 0 || arrow < openBrace))
        {
            return source.Substring(match.Index, semicolon - match.Index + 1);
        }

        if (openBrace < 0)
        {
            throw new InvalidOperationException($"Member '{memberName}' has no body.");
        }

        var closeBrace = FindMatchingBrace(source, openBrace);
        return source.Substring(match.Index, closeBrace - match.Index + 1);
    }

    private static string ExtractTextBetween(string source, string startToken, string endToken)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Start token '{startToken}' was not found.");
        }

        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"End token '{endToken}' was not found after '{startToken}'.");
        }

        return source.Substring(start, end - start);
    }

    private static int FindMatchingBrace(string source, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        throw new InvalidOperationException("Matching brace was not found.");
    }

    private static void AssertRegex(string value, string pattern, string fieldName)
    {
        if (!Regex.IsMatch(value, pattern, RegexOptions.Singleline))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: pattern '{pattern}' was not found.");
        }
    }

    private static void AssertNoRegex(string value, string pattern, string fieldName)
    {
        if (Regex.IsMatch(value, pattern, RegexOptions.Singleline))
        {
            throw new InvalidOperationException(
                $"Assertion failed for {fieldName}: forbidden pattern '{pattern}' was found.");
        }
    }

    private static void AssertOccursBefore(string value, string earlierToken, string laterToken)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedEarlierToken = NormalizeLineEndings(earlierToken);
        var normalizedLaterToken = NormalizeLineEndings(laterToken);
        var earlier = normalizedValue.IndexOf(normalizedEarlierToken, StringComparison.Ordinal);
        var later = normalizedValue.IndexOf(normalizedLaterToken, StringComparison.Ordinal);
        if (earlier < 0 || later < 0 || earlier >= later)
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{earlierToken}' to occur before '{laterToken}'.");
        }
    }

    private static string StripCSharpCommentsAndStringContents(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            if (current == '/' && next == '/')
            {
                builder.Append(' ');
                builder.Append(' ');
                i += 2;
                while (i < source.Length && source[i] != '\n')
                {
                    builder.Append(' ');
                    i++;
                }
                if (i < source.Length)
                {
                    builder.Append('\n');
                }
                continue;
            }

            if (current == '/' && next == '*')
            {
                builder.Append(' ');
                builder.Append(' ');
                i += 2;
                while (i < source.Length)
                {
                    if (source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/')
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i++;
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            if (current == '"')
            {
                var verbatim = i > 0 && source[i - 1] == '@';
                builder.Append('"');
                i++;
                while (i < source.Length)
                {
                    if (!verbatim && source[i] == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (source[i] == '"')
                    {
                        builder.Append('"');
                        if (verbatim && i + 1 < source.Length && source[i + 1] == '"')
                        {
                            builder.Append('"');
                            i += 2;
                            continue;
                        }
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            if (current == '\'')
            {
                builder.Append('\'');
                i++;
                while (i < source.Length)
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        builder.Append(' ');
                        builder.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (source[i] == '\'')
                    {
                        builder.Append('\'');
                        break;
                    }

                    builder.Append(source[i] == '\n' ? '\n' : ' ');
                    i++;
                }
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
