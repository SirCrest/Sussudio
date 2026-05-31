using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static readonly Regex RootServicesUsingRegex = new(
        @"(^|\s)using\s+Sussudio\.Services\s*;",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    internal static Task ServiceNamespaces_FollowServiceFolders()
    {
        var repoRoot = GetRepoRoot();
        AssertServiceNamespaceFolderRules(repoRoot);
        AssertServiceNamespaceNativeXuProbeOwnership(repoRoot);
        AssertServiceNamespaceSourceOwnership(repoRoot);
        AssertServiceContractsBoundaryOwnership(repoRoot);

        return Task.CompletedTask;
    }

    private static void AssertServiceNamespaceFolderRules(string repoRoot)
    {
        var servicesRoot = Path.Combine(GetRepoRoot(), "Sussudio", "Services");
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

            var expectedNamespace = $"namespace Sussudio.Services.{parts[0]}";
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            if (!ContainsNamespaceDeclaration(code, expectedNamespace))
            {
                throw new InvalidOperationException($"{relative} must declare {expectedNamespace}");
            }

            AssertDoesNotContain(code, "namespace Sussudio.Services;");
        }

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "Sussudio"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            if (RootServicesUsingRegex.IsMatch(code))
            {
                throw new InvalidOperationException($"{Path.GetRelativePath(repoRoot, file)} imports the flat Services namespace.");
            }
        }
    }

    private static void AssertServiceContractsBoundaryOwnership(string repoRoot)
    {
        var serviceContractFiles = new[]
        {
            "Sussudio/Services/Contracts/ServiceContracts.cs",
            "Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs"
        };

        foreach (var relativePath in serviceContractFiles)
        {
            var source = ReadRepoFile(relativePath);
            AssertEqual(
                true,
                ContainsNamespaceDeclaration(source, "namespace Sussudio.Services.Contracts"),
                $"{relativePath} declares service contract namespace");
            AssertDoesNotContain(source, "namespace Sussudio.Tools;");
            AssertDoesNotContain(source, "Sussudio.Automation.Contracts");
        }

        var automationContractsProject = Path.Combine(
            repoRoot,
            "Sussudio.Automation.Contracts",
            "Sussudio.Automation.Contracts.csproj");
        var automationReferences = ReadProjectReferences(automationContractsProject);
        AssertEqual(
            0,
            CountProjectReference(automationReferences, @"..\Sussudio\Sussudio.csproj"),
            "automation contracts must not reference the app project");

        var automationContractSources = Directory
            .EnumerateFiles(Path.Combine(repoRoot, "Sussudio.Automation.Contracts"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText)
            .Select(StripCSharpCommentsAndLiterals);
        foreach (var source in automationContractSources)
        {
            AssertDoesNotContain(source, "Sussudio.Services.Contracts");
            AssertDoesNotContain(source, "Sussudio/Services/Contracts");
            AssertDoesNotContain(source, @"Sussudio\Services\Contracts");
        }

        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        foreach (var relativePath in serviceContractFiles)
        {
            AssertContains(agentMapText, "`" + relativePath + "`");
        }

        var serviceContractsText = ReadRepoFile("Sussudio/Services/Contracts/ServiceContracts.cs");
        AssertContains(serviceContractsText, "internal sealed class PooledVideoFrameLease : IDisposable");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "PooledVideoFrameLease.cs")),
            "pooled-frame leases live with the pooled frame owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "PooledVideoFrame.cs")),
            "pooled-frame ownership types live with ServiceContracts");

        AssertContains(serviceContractsText, "public interface IAutomationWindowControl");
        AssertContains(serviceContractsText, "internal interface IPreviewFrameSink");
        var sourceTelemetryProviderText = ReadRepoFile("Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs");
        AssertContains(sourceTelemetryProviderText, "public interface ISourceSignalTelemetryProvider");
        AssertContains(sourceTelemetryProviderText, "namespace Sussudio.Models");
        AssertContains(sourceTelemetryProviderText, "public sealed record SourceSignalTelemetrySnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Models", "Telemetry", "SourceSignalTelemetrySnapshot.cs")),
            "source telemetry DTOs live with the probe-linked telemetry provider contract");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "RecordingContracts.cs")),
            "recording service contracts live with ServiceContracts");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "AutomationInterfaces.cs")),
            "automation service interfaces live with ServiceContracts");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "IPreviewFrameSink.cs")),
            "preview sink service interface lives with ServiceContracts");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "ServiceInterfaces.cs")),
            "app service contract interfaces live with ServiceContracts");

        AssertContains(agentMapText, "separate from `Sussudio.Automation.Contracts` wire/protocol contracts");
    }

    private static bool ContainsNamespaceDeclaration(string source, string namespacePrefix)
        => source.Contains(namespacePrefix + ";", StringComparison.Ordinal) ||
           source.Contains(namespacePrefix + "\n{", StringComparison.Ordinal) ||
           source.Contains(namespacePrefix + "\r\n{", StringComparison.Ordinal);

    internal static Task AutomationContracts_SourceOwnership_IsModelAligned()
    {
        var repoRoot = GetRepoRoot();
        var automationContractsProject = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "Sussudio.Automation.Contracts.csproj");
        AssertEqual(true, File.Exists(automationContractsProject), "Automation contracts project exists");

        foreach (var contractFile in new[]
        {
            "AutomationCommandKind.cs",
            "AutomationCommandCatalog.cs",
            "AutomationPipeProtocol.cs"
        })
        {
            var contractPath = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", contractFile);
            AssertEqual(true, File.Exists(contractPath), $"{contractFile} contract source exists");
            var expectedNamespace = string.Equals(contractFile, "AutomationCommandKind.cs", StringComparison.Ordinal)
                ? "namespace Sussudio.Models;"
                : "namespace Sussudio.Tools;";
            AssertContains(File.ReadAllText(contractPath), expectedNamespace);
            AssertEqual(
                false,
                File.Exists(Path.Combine(repoRoot, "tools", "Common", contractFile)),
                $"tools/Common must not own {contractFile}");
            AssertEqual(
                false,
                File.Exists(Path.Combine(repoRoot, "tools", "Common", "AutomationPipeClient", contractFile)),
                $"tools/Common/AutomationPipeClient must not own {contractFile}");
            AssertEqual(
                false,
                File.Exists(Path.Combine(repoRoot, "Sussudio", "Models", "Automation", contractFile)),
                $"app project must not own {contractFile}");
        }

        var appIncludes = ReadCompileIncludes(Path.Combine(repoRoot, "Sussudio", "Sussudio.csproj"));
        var appReferences = ReadProjectReferences(Path.Combine(repoRoot, "Sussudio", "Sussudio.csproj"));
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationCommandKind.cs"),
            "app project must not link AutomationCommandKind from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationCommandCatalog.cs"),
            "app project must not link AutomationCommandCatalog from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationPipeProtocol.cs"),
            "app project must not link AutomationPipeProtocol from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationResponseState.cs"),
            "app project must not link AutomationResponseState from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationPipeSecurityPolicy.cs"),
            "app project must not link AutomationPipeSecurityPolicy from tools/Common");
        var protocolText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "AutomationPipeProtocol.cs"));
        AssertContains(protocolText, "public static class AutomationPipeSecurityPolicy");
        AssertContains(protocolText, "public readonly record struct AutomationPipeCommandResult");
        AssertContains(protocolText, "public static class AutomationResponseState");
        AssertContains(protocolText, "public static class AutomationSyntheticErrorResponse");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "AutomationPipeClientModels.cs")),
            "pipe client handoff/error models stay folded into AutomationPipeProtocol.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "Common", "AutomationPipeClient", "AutomationPipeClient.Models.cs")),
            "tools/Common/AutomationPipeClient must not own AutomationPipeClient.Models.cs");
        AssertEqual(
            1,
            CountProjectReference(appReferences, @"..\Sussudio.Automation.Contracts\Sussudio.Automation.Contracts.csproj"),
            "app project references automation contracts exactly once");

        foreach (var toolProject in new[]
        {
            Path.Combine(repoRoot, "tools", "AutomationClient", "AutomationClient.csproj"),
            Path.Combine(repoRoot, "tools", "ssctl", "ssctl.csproj"),
            Path.Combine(repoRoot, "tools", "McpServer", "McpServer.csproj")
        })
        {
            var includes = ReadCompileIncludes(toolProject);
            var references = ReadProjectReferences(toolProject);
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\..\Sussudio\Models\Automation\AutomationCommandKind.cs"),
                $"{Path.GetFileName(toolProject)} must not link app-owned AutomationCommandKind source");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationCommandKind.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationCommandKind from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationCommandCatalog.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationCommandCatalog from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationPipeProtocol.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationPipeProtocol from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationResponseState.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationResponseState from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationPipeSecurityPolicy.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationPipeSecurityPolicy from tools/Common");
            AssertEqual(
                1,
                CountProjectReference(references, @"..\..\Sussudio.Automation.Contracts\Sussudio.Automation.Contracts.csproj"),
                $"{Path.GetFileName(toolProject)} references automation contracts exactly once");
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

    private static string[] ReadProjectReferences(string projectPath)
        => XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => NormalizeProjectInclude(include!))
            .ToArray();

    private static int CountProjectReference(IEnumerable<string> references, string include)
        => references.Count(value => string.Equals(value, NormalizeProjectInclude(include), StringComparison.OrdinalIgnoreCase));

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

    private static readonly object RtkI2cProbeConsoleLock = new();

    internal static Task RtkI2cProbe_GuardsUnsafeNativePaths()
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine(
            "tools",
            "NativeXuAudioProbe",
            "bin",
            "Debug",
            "net8.0-windows10.0.19041.0",
            "win-x64",
            "NativeXuAudioProbe.dll"));
        var probeType = assembly.GetType("RtkI2cProbe")
            ?? throw new InvalidOperationException("RtkI2cProbe type not found.");
        var run = probeType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.Run method not found.");
        var getRtkDeviceName = probeType.GetMethod("GetRtkDeviceName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.GetRtkDeviceName method not found.");
        var rtkProbeSource = ReadRepoFile("tools/NativeXuAudioProbe/Program.cs");

        var missingPathDevice = CreateNativeXuProbeDevice(assembly, "capture-1", "Elgato 4K X (PID 0x0070)", null);
        var missingPath = CaptureConsole(() => InvokeRtkRun(run, [], missingPathDevice));
        AssertEqual(1, missingPath.ExitCode, "RtkI2cProbe missing native XU path exit code");
        AssertContains(rtkProbeSource, "requires a selected native XU interface path");

        var selectedPathDevice = CreateNativeXuProbeDevice(assembly, "capture-2", "Elgato 4K X (PID 0x0070)", @"\\?\hid#vid_0fd9&pid_0070#xu");
        var disabledSwitch = CaptureConsole(() => InvokeRtkRun(run, ["switch", "analog"], selectedPathDevice));
        AssertEqual(1, disabledSwitch.ExitCode, "RtkI2cProbe disabled switch exit code");
        AssertContains(rtkProbeSource, "RTK I2C switch is disabled");
        AssertContains(rtkProbeSource, "Use the native XU service/probe path");

        var trimmedName = getRtkDeviceName.Invoke(null, [selectedPathDevice]) as string;
        AssertEqual("Elgato 4K X", trimmedName, "RtkI2cProbe strips PID suffix for RTK device name");
        var defaultNameDevice = CreateNativeXuProbeDevice(assembly, "capture-3", string.Empty, @"\\?\hid#vid_0fd9&pid_0070#xu");
        var defaultName = getRtkDeviceName.Invoke(null, [defaultNameDevice]) as string;
        AssertEqual("Elgato 4K X", defaultName, "RtkI2cProbe default RTK device name");

        return Task.CompletedTask;
    }

    private static void AssertServiceNamespaceNativeXuProbeOwnership(string repoRoot)
    {
        var nativeXuProbeProjectText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuAudioProbe.csproj"));
        AssertDoesNotContain(nativeXuProbeProjectText, "<ProjectReference");
        AssertDoesNotContain(nativeXuProbeProjectText, "Sussudio.csproj");
        AssertContains(nativeXuProbeProjectText, "NativeXuAudioControlService.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.Transport.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.RawTransport.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAudioControlService.Profiles.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuDeviceSupport.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AnalogGain.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AtProtocol.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AudioCommands.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.AudioSwitch.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DiagnosticSummary.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DeviceCommandReads.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.DeviceCommands.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.FullSnapshot.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.InterfaceRead.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.PayloadDecoding.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.RollingPoll.cs");
        AssertContains(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.CommandResults.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.SnapshotAssembly.Timing.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.Selector4.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Build.cs");
        AssertDoesNotContain(nativeXuProbeProjectText, "NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs");
        AssertContains(File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Models", "Capture", "CaptureModels.cs")), "NativeXuInterfacePath");

        var nativeXuLocatorText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.cs"));
        AssertContains(nativeXuLocatorText, "NativeXuInterfacePath = interfacePath");
        AssertContains(nativeXuLocatorText, "matches.Length > 1");
        AssertDoesNotContain(nativeXuLocatorText, "return firstCandidate");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "NativeXuProbeDeviceLocator.cs")),
            "NativeXu probe device lookup lives with top-level probe command routing");

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
        AssertContains(probeProgramText, "Probe-local runtime shims used by linked app service sources.");
        AssertContains(probeProgramText, "NativeXuInterfacePath");
        AssertContains(probeProgramText, "EnumerateKsInterfaces(ElgatoVendorId");
        AssertContains(probeProgramText, "RTK_IO selects by name, not by native XU path");
        AssertContains(probeProgramText, "string.Equals(arg, \"--device\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(probeProgramText, "NativeXuProbeDeviceLocator.Find(null)");
        AssertContains(probeProgramText, "RtkI2cProbe.Run(rtkArgs, dev)");
        var probeDefaultExperimentText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.DefaultExperiment.cs"));
        var probeDefaultExperimentReportingText = probeDefaultExperimentText;
        var probeI2cCommandsText = File.ReadAllText(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.cs"));
        var probeI2cLegacyProbeText = probeI2cCommandsText;
        AssertDoesNotContain(probeProgramText, "sealed record GetterSpec");
        AssertDoesNotContain(probeProgramText, "sealed class ExperimentResult");
        AssertDoesNotContain(probeProgramText, "const int CmdAudioFormat");
        AssertDoesNotContain(probeProgramText, "PrintSnapshot(\"Baseline snapshot\"");
        AssertDoesNotContain(probeProgramText, "static async Task RunAnalogGainSequenceAsync");
        AssertDoesNotContain(probeProgramText, "Usage: i2c-cmd get|set|scan");
        AssertDoesNotContain(probeProgramText, "Tests whether rtk_sendI2CATCommand uses the same XU path");
        AssertDoesNotContain(probeProgramText, "I2C SET/verify via AT envelope");
        AssertDoesNotContain(probeProgramText, "static IEnumerable<SetExperiment> BuildShortExperiments");
        AssertDoesNotContain(probeProgramText, "static async Task<byte[]?> SendI2cAtGetAsync");
        AssertDoesNotContain(probeProgramText, "static byte[] BuildAtFrameWithPayload");
        AssertDoesNotContain(probeProgramText, "using static NativeXuProbeI2cTransport;");
        AssertContains(probeProgramText, "NativeXuProbeI2cCommands.RunAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeAtCommands.RunAtReadAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeAtCommands.RunAtWriteAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeAtCommands.RunAtSetInputAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeDefaultExperiment.RunAsync(device)");
        AssertContains(probeProgramText, "NativeXuProbeI2cLegacyProbe.Run()");
        AssertContains(probeProgramText, "NativeXuProbeI2cSwitch.RunAsync(args)");
        AssertContains(probeProgramText, "NativeXuProbeServiceProbe.RunServiceControlProbeAsync");
        AssertContains(probeProgramText, "NativeXuProbeServiceProbe.RunServiceSmokeAsync");
        AssertContains(probeProgramText, "static class NativeXuProbeServiceProbe");
        AssertContains(probeProgramText, "public static async Task<int> RunServiceControlProbeAsync");
        AssertContains(probeProgramText, "public static async Task<int> RunServiceSmokeAsync");
        AssertContains(probeProgramText, "ReadServiceStateAsync");
        AssertContains(probeProgramText, "Service payload snapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.Models.cs")),
            "old NativeXu probe model bucket removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.AtCommands.cs")),
            "NativeXu direct AT probe commands live with top-level probe command routing");
        AssertContains(probeProgramText, "static class NativeXuProbeAtCommands");
        AssertContains(probeProgramText, "public static async Task<int> RunAtReadAsync");
        AssertContains(probeProgramText, "public static async Task<int> RunAtWriteAsync");
        AssertContains(probeProgramText, "public static async Task<int> RunAtSetInputAsync");
        AssertContains(probeProgramText, "Usage: at-write <opcode_hex>");
        AssertContains(probeProgramText, "Before: InputSource=");
        AssertContains(probeDefaultExperimentText, "public const int CmdAudioFormat = 0x04;");
        AssertContains(probeDefaultExperimentText, "public const int CmdSetAuxOutVolume = 0x82;");
        AssertContains(probeDefaultExperimentText, "static class NativeXuProbeFormatting");
        AssertContains(probeDefaultExperimentText, "public static string FormatRaw");
        AssertContains(probeDefaultExperimentText, "static class NativeXuProbeDefaultExperiment");
        AssertDoesNotContain(probeDefaultExperimentText, "partial class NativeXuProbeDefaultExperiment");
        AssertContains(probeDefaultExperimentText, "sealed record GetterSpec");
        AssertContains(probeDefaultExperimentText, "sealed record SetterSpec");
        AssertContains(probeDefaultExperimentText, "sealed record SetExperiment");
        AssertContains(probeDefaultExperimentText, "public static async Task<int> RunAsync(CaptureDevice device)");
        AssertContains(probeDefaultExperimentText, "RunAnalogGainSequenceAsync");
        AssertContains(probeDefaultExperimentText, "private static IEnumerable<SetExperiment> BuildShortExperiments");
        AssertContains(probeDefaultExperimentText, "private static byte[] BuildPayload(int width, long value)");
        AssertContains(probeDefaultExperimentReportingText, "sealed record AtReadResult");
        AssertContains(probeDefaultExperimentReportingText, "sealed record ChangedValue");
        AssertContains(probeDefaultExperimentReportingText, "sealed class ExperimentResult");
        AssertContains(probeDefaultExperimentReportingText, "private static async Task<Dictionary<int, AtReadResult>> ReadAllAsync");
        AssertContains(probeDefaultExperimentReportingText, "private static AtReadResult Decode");
        AssertContains(probeDefaultExperimentReportingText, "private static void PrintInterestingChanges");
        AssertContains(probeDefaultExperimentReportingText, "private static void PrintSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.DefaultExperiment.Reporting.cs")),
            "NativeXu default experiment reporting folded into default experiment owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.ExperimentPayloads.cs")),
            "NativeXu probe experiment payload helpers folded into default experiment owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.Commands.cs")),
            "NativeXu probe command IDs and shared raw formatting live with default experiment support");
        AssertContains(probeI2cCommandsText, "static class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsText, "static class NativeXuProbeI2cTransport");
        AssertDoesNotContain(probeI2cCommandsText, "static partial class NativeXuProbeI2cCommands");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunAsync");
        AssertContains(probeI2cCommandsText, "Usage: i2c-cmd get|set|scan");
        AssertContains(probeI2cCommandsText, "RunVerifyAsync(dev)");
        AssertContains(probeI2cCommandsText, "I2C SET/verify via AT envelope");
        AssertContains(probeI2cCommandsText, "RunTopologyProbe(dev)");
        AssertContains(probeI2cCommandsText, "Testing with own GUID as property set");
        AssertContains(probeI2cCommandsText, "RunSelectorProbeAsync(dev)");
        AssertContains(probeI2cCommandsText, "Full Selector 3 dump");
        AssertContains(probeI2cCommandsText, "RunHighSelectorProbeAsync(dev)");
        AssertContains(probeI2cCommandsText, "Probing selectors 18-40");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunHighSelectorProbeAsync");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunSelectorProbeAsync");
        AssertContains(probeI2cCommandsText, "public static int RunTopologyProbe");
        AssertContains(probeI2cCommandsText, "public static async Task<int> RunVerifyAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.HighSelectorProbe.cs")),
            "old NativeXu i2c high-selector partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.SelectorProbe.cs")),
            "old NativeXu i2c selector partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.TopologyProbe.cs")),
            "old NativeXu i2c topology partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cCommands.Verify.cs")),
            "old NativeXu i2c verify partial removed");
        AssertContains(probeI2cLegacyProbeText, "static class NativeXuProbeI2cLegacyProbe");
        AssertContains(probeI2cLegacyProbeText, "public static int Run()");
        AssertContains(probeI2cLegacyProbeText, "Tests whether rtk_sendI2CATCommand uses the same XU path");
        AssertContains(probeI2cLegacyProbeText, "ProbeRawI2cFrames");
        AssertContains(probeI2cLegacyProbeText, "ProbeAlternateSelectors");
        AssertContains(probeI2cLegacyProbeText, "ProbeAtWrappedI2cFrames");
        AssertContains(probeI2cCommandsText, "public static async Task<byte[]?> SendI2cAtGetAsync");
        AssertContains(probeI2cCommandsText, "public static byte[] BuildAtFrameWithPayload");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cLegacyProbe.cs")),
            "NativeXu legacy i2c-probe workflow lives with the I2C command family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cSwitch.cs")),
            "NativeXu captured audio-switch replay workflow lives with top-level probe command routing");
        AssertContains(probeProgramText, "static class NativeXuProbeI2cSwitch");
        AssertContains(probeProgramText, "public static async Task<int> RunAsync");
        AssertContains(probeProgramText, "Current I2C AT state");
        AssertContains(probeProgramText, "Sending audio switch sequence");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.I2cTransport.cs")),
            "NativeXu I2C-over-AT transport helpers live with the I2C command family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "Program.ServiceProbe.cs")),
            "NativeXu service-control smoke/payload workflows live with top-level probe command routing");
        AssertContains(probeProgramText, "static class RtkI2cProbe");
        AssertContains(probeProgramText, "Run(string[] args, CaptureDevice device)");
        AssertContains(probeProgramText, "RTK I2C switch is disabled");
        AssertDoesNotContain(probeProgramText, "rtk_setCurrentDevice(\"Elgato 4K X\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "NativeXuAudioProbe", "RtkI2cProbe.cs")),
            "RTK I2C probe workflow lives with top-level probe command routing");

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "Sussudio"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsPreserveLiterals(File.ReadAllText(file));
            AssertDoesNotContain(code, "InternalsVisibleTo(\"NativeXuAudioProbe\")");
        }
    }

    private static object CreateNativeXuProbeDevice(
        Assembly assembly,
        string id,
        string name,
        string? nativeXuInterfacePath)
    {
        var deviceType = assembly.GetType("CaptureDevice")
            ?? throw new InvalidOperationException("NativeXuAudioProbe CaptureDevice type not found.");
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("Failed to create NativeXuAudioProbe CaptureDevice.");
        deviceType.GetProperty("Id")?.SetValue(device, id);
        deviceType.GetProperty("Name")?.SetValue(device, name);
        deviceType.GetProperty("NativeXuInterfacePath")?.SetValue(device, nativeXuInterfacePath);
        return device;
    }

    private static int InvokeRtkRun(MethodInfo run, string[] args, object device)
    {
        try
        {
            return (int)(run.Invoke(null, [args, device])
                         ?? throw new InvalidOperationException("RtkI2cProbe.Run returned null."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static (int ExitCode, string Output, string Error) CaptureConsole(Func<int> action)
    {
        lock (RtkI2cProbeConsoleLock)
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var output = new StringWriter();
            using var error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                var exitCode = action();
                return (exitCode, output.ToString(), error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
