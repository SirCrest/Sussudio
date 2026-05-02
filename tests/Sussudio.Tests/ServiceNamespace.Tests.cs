using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

static partial class Program
{
    private static readonly Regex RootServicesUsingRegex = new(
        @"(^|\s)using\s+ElgatoCapture\.Services\s*;",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static Task ServiceNamespaces_FollowServiceFolders()
    {
        var repoRoot = GetRepoRoot();
        var servicesRoot = Path.Combine(GetRepoRoot(), "ElgatoCapture", "Services");
        var rootFiles = EnumerateSourceFiles(servicesRoot, SearchOption.TopDirectoryOnly).ToArray();
        AssertEqual(0, rootFiles.Length, "Services root C# file count");

        foreach (var file in EnumerateSourceFiles(servicesRoot, SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(servicesRoot, file);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length < 2)
            {
                throw new InvalidOperationException($"Service file must live in a domain folder: {relative}");
            }

            var expectedNamespace = $"namespace ElgatoCapture.Services.{parts[0]};";
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            if (!code.Contains(expectedNamespace, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{relative} must declare {expectedNamespace}");
            }

            AssertDoesNotContain(code, "namespace ElgatoCapture.Services;");
        }

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "ElgatoCapture"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            if (RootServicesUsingRegex.IsMatch(code))
            {
                throw new InvalidOperationException($"{Path.GetRelativePath(repoRoot, file)} imports the flat Services namespace.");
            }
        }

        var nativeXuProbeProjectText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuAudioProbe.csproj"));
        AssertDoesNotContain(nativeXuProbeProjectText, "<ProjectReference");
        AssertDoesNotContain(nativeXuProbeProjectText, "ElgatoCapture.csproj");
        AssertContains(nativeXuProbeProjectText, "NativeXuAudioControlService.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.cs");
        AssertContains(File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Models", "CaptureDevice.cs")), "NativeXuInterfacePath");
        AssertContains(File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "ToolCaptureDevice.cs")), "NativeXuInterfacePath");
        var deviceServiceText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Services", "Capture", "DeviceService.cs"));
        AssertContains(deviceServiceText, "NativeXuInterfacePath = ResolveNativeXuInterfacePath(videoDevice.SymbolicLink)");
        AssertContains(deviceServiceText, "Native XU interface resolution found no matching interface");
        AssertDoesNotContain(deviceServiceText, "SelectOnlyUnambiguousDeviceGroup");

        var nativeXuLocatorText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuProbeDeviceLocator.cs"));
        AssertContains(nativeXuLocatorText, "NativeXuInterfacePath = interfacePath");
        AssertContains(nativeXuLocatorText, "matches.Length > 1");
        AssertDoesNotContain(nativeXuLocatorText, "return firstCandidate");
        AssertDoesNotContain(
            File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.cs")),
            "KsExtensionUnitNative.EnumerateKsInterfaces(");

        var nativeXuAtProviderText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Services", "Telemetry", "NativeXuAtCommandProvider.cs"));
        AssertContains(nativeXuAtProviderText, "device?.NativeXuInterfacePath");
        AssertContains(nativeXuAtProviderText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(nativeXuAtProviderText, "return Array.Empty<KsExtensionUnitNative.KsInterfacePath>()");
        AssertContains(nativeXuAtProviderText, "nativexu-interface-ambiguous");
        AssertContains(nativeXuAtProviderText, "missing_selected_interface");
        AssertContains(nativeXuAtProviderText, "_rollingInterfacePath");
        AssertContains(nativeXuAtProviderText, "cancellationToken.ThrowIfCancellationRequested()");
        AssertContains(nativeXuAtProviderText, "EnumerateKsInterfaces(vendorId, productId, device)");

        var nativeXuAudioServiceText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Services", "Audio", "NativeXuAudioControlService.cs"));
        AssertContains(nativeXuAudioServiceText, "device?.NativeXuInterfacePath");
        AssertContains(nativeXuAudioServiceText, "missing-selected-interface");
        AssertContains(nativeXuAudioServiceText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(nativeXuAudioServiceText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(nativeXuAudioServiceText, "EnumerateCandidates(vendorId, productId, device?.NativeXuInterfacePath)");

        var captureServiceText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Services", "Capture", "CaptureService.cs"));
        AssertContains(captureServiceText, "pollGeneration != Volatile.Read(ref _telemetryPollGeneration)");
        AssertContains(captureServiceText, "_telemetryPollSync");
        AssertContains(captureServiceText, "lock (_telemetryPollSync)");
        AssertContains(captureServiceText, "StartTelemetryPollCoreLocked");
        AssertContains(captureServiceText, "StartTelemetryPollCore");
        AssertContains(captureServiceText, "Telemetry poll start deferred until canceled poll exits");

        var audioControlsText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "ViewModels", "MainViewModel.AudioControls.cs"));
        AssertContains(audioControlsText, "RefreshDeviceAudioControlsAsync(");
        AssertContains(audioControlsText, "ReadStateAsync(device, cancellationToken)");
        AssertContains(audioControlsText, "Device audio mode failure readback ignored");
        AssertContains(audioControlsText, "failureState.Mode");
        AssertContains(audioControlsText, "failureState.AnalogGainPercent");
        AssertContains(audioControlsText, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(audioControlsText, "CaptureDevice? targetDevice = null");
        AssertContains(audioControlsText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertContains(audioControlsText, "IsCurrentSelectedDevice(device)");

        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "ViewModels", "MainViewModel.cs"));
        AssertContains(mainViewModelText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelText, "allowDuringDispose: true");
        AssertContains(mainViewModelText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing");
        AssertContains(mainViewModelText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing_after_enqueue");
        AssertContains(mainViewModelText, "UI_OPERATION_ENQUEUE_FAILED op='{operationName}'");
        AssertContains(mainViewModelText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=async");
        AssertContains(mainViewModelText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=value");
        AssertContains(mainViewModelText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        AssertContains(mainViewModelText, "CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        var deviceManagementText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "ViewModels", "MainViewModel.DeviceManagement.cs"));
        AssertContains(deviceManagementText, "CancelPendingAudioControlWork");
        AssertContains(deviceManagementText, "_deviceAudioModeCts");
        AssertContains(deviceManagementText, "_deviceAudioRefreshCts");
        AssertContains(deviceManagementText, "AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        AssertContains(deviceManagementText, "FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "ViewModels", "MainViewModel.Telemetry.cs")),
            "SOURCE_TELEMETRY_UI_ENQUEUE_FAILED");
        var settingsText = File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "ViewModels", "MainViewModel.Settings.cs"));
        AssertContains(settingsText, "RECORDING_FORMATS_UI_ENQUEUE_FAILED");
        AssertContains(settingsText, "SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Services", "Preview", "D3D11PreviewRenderer.Rendering.cs")),
            "D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "ElgatoCapture", "Services", "Preview", "D3D11PreviewRenderer.Rendering.cs")),
            "D3D11 preview swap chain unbind enqueue failed during cleanup.");

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            AssertDoesNotContain(code, "Activator.CreateInstance");
            AssertDoesNotContain(code, "BindingFlags");
            AssertDoesNotContain(code, "GetMethod(");
            AssertDoesNotContain(code, "GetProperty(");
            AssertDoesNotContain(code, "ReadPreferredPayloadAsync");
            AssertDoesNotContain(code, "typeof(NativeXuAudioControlService)");
        }
        var probeProgramText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.cs"));
        AssertDoesNotContain(probeProgramText, "KsExtensionUnitNative.EnumerateKsInterfaces(");
        AssertContains(probeProgramText, "RTK_IO selects by name, not by native XU path");
        AssertContains(probeProgramText, "string.Equals(arg, \"--device\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(probeProgramText, "NativeXuProbeDeviceLocator.Find(null)");
        AssertContains(probeProgramText, "RtkI2cProbe.Run(rtkArgs, dev)");
        var rtkProbeText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "RtkI2cProbe.cs"));
        AssertContains(rtkProbeText, "Run(string[] args, CaptureDevice device)");
        AssertContains(rtkProbeText, "RTK I2C switch is disabled");
        AssertDoesNotContain(rtkProbeText, "rtk_setCurrentDevice(\"Elgato 4K X\"");

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "ElgatoCapture"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsPreserveLiterals(File.ReadAllText(file));
            AssertDoesNotContain(code, "InternalsVisibleTo(\"NativeXuAudioProbe\")");
        }

        return Task.CompletedTask;
    }

    private static Task AutomationCommandKind_SourceOwnership_IsModelAligned()
    {
        var repoRoot = GetRepoRoot();
        var automationKindPath = Path.Combine(repoRoot, "ElgatoCapture", "Models", "AutomationCommandKind.cs");
        AssertEqual(true, File.Exists(automationKindPath), "AutomationCommandKind model source exists");
        AssertContains(File.ReadAllText(automationKindPath), "namespace ElgatoCapture.Models;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "Common", "AutomationCommandKind.cs")),
            "tools/Common no longer owns AutomationCommandKind");

        var appIncludes = ReadCompileIncludes(Path.Combine(repoRoot, "ElgatoCapture", "ElgatoCapture.csproj"));
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationCommandKind.cs"),
            "app project must not link AutomationCommandKind from tools/Common");

        foreach (var toolProject in new[]
        {
            Path.Combine(repoRoot, "tools", "AutomationClient", "AutomationClient.csproj"),
            Path.Combine(repoRoot, "tools", "ecctl", "ecctl.csproj"),
            Path.Combine(repoRoot, "tools", "McpServer", "McpServer.csproj")
        })
        {
            var includes = ReadCompileIncludes(toolProject);
            AssertEqual(
                1,
                CountCompileInclude(includes, @"..\..\ElgatoCapture\Models\AutomationCommandKind.cs"),
                $"{Path.GetFileName(toolProject)} links app-owned AutomationCommandKind source exactly once");
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root, SearchOption searchOption)
        => Directory.EnumerateFiles(root, "*.cs", searchOption)
            .Where(file => !HasIgnoredPathSegment(root, file));

    private static string[] ReadCompileIncludes(string projectPath)
        => XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => NormalizeProjectInclude(include!))
            .ToArray();

    private static int CountCompileInclude(IEnumerable<string> includes, string include)
        => includes.Count(value => string.Equals(value, NormalizeProjectInclude(include), StringComparison.OrdinalIgnoreCase));

    private static string NormalizeProjectInclude(string include)
        => include.Trim().Replace('\\', '/');

    private static bool HasIgnoredPathSegment(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        return segments.Any(segment =>
            string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(segment, "Generated Files", StringComparison.OrdinalIgnoreCase));
    }

    private static string StripCSharpCommentsAndLiterals(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length;)
        {
            var current = text[index];
            if (current == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                index = StripLineComment(text, index, builder);
                continue;
            }

            if (current == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index = StripBlockComment(text, index, builder);
                continue;
            }

            if (current == '\'')
            {
                index = StripCharacterLiteral(text, index, builder);
                continue;
            }

            if (current == '"')
            {
                var quoteCount = CountQuoteRun(text, index);
                index = quoteCount >= 3
                    ? StripRawStringLiteral(text, index, quoteCount, builder)
                    : StripStringLiteral(text, index, IsVerbatimStringQuote(text, index), builder);
                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static int StripLineComment(string text, int start, StringBuilder builder)
    {
        var index = start;
        while (index < text.Length && text[index] != '\r' && text[index] != '\n')
        {
            builder.Append(' ');
            index++;
        }

        return index;
    }

    private static int StripBlockComment(string text, int start, StringBuilder builder)
    {
        var index = start;
        while (index < text.Length)
        {
            if (index + 1 < text.Length && text[index] == '*' && text[index + 1] == '/')
            {
                builder.Append(' ');
                builder.Append(' ');
                return index + 2;
            }

            AppendSpaceOrNewline(builder, text[index]);
            index++;
        }

        return index;
    }

    private static int StripStringLiteral(string text, int start, bool verbatim, StringBuilder builder)
    {
        AppendSpaceOrNewline(builder, text[start]);
        var index = start + 1;
        while (index < text.Length)
        {
            var current = text[index];
            AppendSpaceOrNewline(builder, current);

            if (verbatim)
            {
                if (current == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        AppendSpaceOrNewline(builder, text[index + 1]);
                        index += 2;
                        continue;
                    }

                    return index + 1;
                }

                index++;
                continue;
            }

            if (current == '\\' && index + 1 < text.Length)
            {
                AppendSpaceOrNewline(builder, text[index + 1]);
                index += 2;
                continue;
            }

            if (current == '"')
            {
                return index + 1;
            }

            index++;
        }

        return index;
    }

    private static int StripRawStringLiteral(string text, int start, int quoteCount, StringBuilder builder)
    {
        for (var quoteIndex = 0; quoteIndex < quoteCount; quoteIndex++)
        {
            builder.Append(' ');
        }

        var index = start + quoteCount;
        while (index < text.Length)
        {
            if (CountQuoteRun(text, index) >= quoteCount)
            {
                for (var quoteIndex = 0; quoteIndex < quoteCount; quoteIndex++)
                {
                    builder.Append(' ');
                }

                return index + quoteCount;
            }

            AppendSpaceOrNewline(builder, text[index]);
            index++;
        }

        return index;
    }

    private static int StripCharacterLiteral(string text, int start, StringBuilder builder)
    {
        AppendSpaceOrNewline(builder, text[start]);
        var index = start + 1;
        while (index < text.Length)
        {
            var current = text[index];
            AppendSpaceOrNewline(builder, current);

            if (current == '\\' && index + 1 < text.Length)
            {
                AppendSpaceOrNewline(builder, text[index + 1]);
                index += 2;
                continue;
            }

            if (current == '\'')
            {
                return index + 1;
            }

            index++;
        }

        return index;
    }

    private static bool IsVerbatimStringQuote(string text, int quoteIndex)
    {
        var index = quoteIndex - 1;
        while (index >= 0 && text[index] == '$')
        {
            index--;
        }

        return index >= 0 && text[index] == '@';
    }

    private static int CountQuoteRun(string text, int start)
    {
        var count = 0;
        while (start + count < text.Length && text[start + count] == '"')
        {
            count++;
        }

        return count;
    }

    private static void AppendSpaceOrNewline(StringBuilder builder, char value)
        => builder.Append(value is '\r' or '\n' ? value : ' ');

    private static string StripCSharpCommentsPreserveLiterals(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length;)
        {
            var current = text[index];
            if (current == '/' && index + 1 < text.Length && text[index + 1] == '/')
            {
                index = StripLineComment(text, index, builder);
                continue;
            }

            if (current == '/' && index + 1 < text.Length && text[index + 1] == '*')
            {
                index = StripBlockComment(text, index, builder);
                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }
}
