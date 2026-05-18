using System.Text.RegularExpressions;

static partial class Program
{
    private static readonly Regex PresentationPreviewHarnessMethodRegex = new(
        @"(?m)^    private\s+static\s+(?:async\s+)?Task\s+([A-Za-z0-9_]+)\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly Regex PresentationPreviewCatalogRegistrationRegex = new(
        @"AddCheckAsync\s*\(\s*results\s*,\s*[^,]+,\s*([A-Za-z0-9_]+)\s*\)",
        RegexOptions.CultureInvariant);

    private static readonly string[] PresentationPreviewUiOwnershipTestFiles =
    {
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.DeviceActions.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionBindings.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionPresentation.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionPresentationPolicy.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionTooltipFormatter.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionNormalizer.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Interaction.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Layout.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Output.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Screenshot.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.Chrome.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Automation.Hdr.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Automation.Preview.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Automation.PreviewVolume.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioControls.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioDeviceSelectionPolicy.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioRuntime.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.AudioMonitoring.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.OutputPath.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Ownership.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.SessionReinit.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Signals.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.StartupStopOrdering.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Watchdog.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.Reinitialization.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.DeviceFormatProbeRetarget.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.ModeSelection.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.RecordingFormat.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.RuntimeFlags.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.VideoFormat.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SettingsProjection.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DiskSpacePresentation.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DeviceFormatProbeRetarget.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DependencyComposition.Tests.cs"
    };

    private static readonly string[] PresentationPreviewStatsContractTestFiles =
    {
        "tests/Sussudio.Tests/StatsOverlay.Lifecycle.Tests.cs",
        "tests/Sussudio.Tests/StatsDockPresentation.Tests.cs",
        "tests/Sussudio.Tests/StatsHardwareRowsController.Tests.cs",
        "tests/Sussudio.Tests/StatsPresentation.FrameTime.Tests.cs",
        "tests/Sussudio.Tests/StatsPresentation.Ownership.Tests.cs",
        "tests/Sussudio.Tests/StatsPresentation.SourceTelemetry.Tests.cs",
        "tests/Sussudio.Tests/XUnit.StatsPresentationTests.cs"
    };

    private static readonly HashSet<string> PresentationPreviewUiOwnershipCatalogExclusions = new(StringComparer.Ordinal)
    {
        "CaptureErrors_RefreshViewModelRuntimeFlags"
    };

    private static Task PresentationPreviewHarnessRegistration_CoversUiOwnershipChecks()
    {
        var expectedMethods = EnumeratePresentationPreviewUiOwnershipCheckMethods()
            .OrderBy(entry => entry.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Method, StringComparer.Ordinal)
            .ToArray();
        var registeredMethods = ReadPresentationPreviewCatalogMethodRegistrations();

        var missingMethods = expectedMethods
            .Where(entry => !registeredMethods.Contains(entry.Method))
            .Select(entry => $"{entry.File}: {entry.Method}")
            .ToArray();

        if (missingMethods.Length > 0)
        {
            throw new InvalidOperationException(
                "PresentationPreview ownership/contract checks are missing harness registrations: " +
                string.Join(", ", missingMethods));
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<(string File, string Method)> EnumeratePresentationPreviewUiOwnershipCheckMethods()
    {
        foreach (var file in PresentationPreviewUiOwnershipTestFiles.Concat(PresentationPreviewStatsContractTestFiles))
        {
            var strippedSource = StripCSharpCommentsAndLiterals(ReadRepoFile(file));
            foreach (Match match in PresentationPreviewHarnessMethodRegex.Matches(strippedSource))
            {
                var methodName = match.Groups[1].Value;
                if (PresentationPreviewUiOwnershipCatalogExclusions.Contains(methodName))
                {
                    continue;
                }

                yield return (file, methodName);
            }
        }
    }

    private static HashSet<string> ReadPresentationPreviewCatalogMethodRegistrations()
    {
        var root = Path.Combine(GetRepoRoot(), "tests", "Sussudio.Tests");
        var registeredMethods = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(root, "HarnessCheckCatalog.PresentationPreview*.cs"))
        {
            var strippedSource = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            foreach (Match match in PresentationPreviewCatalogRegistrationRegex.Matches(strippedSource))
            {
                registeredMethods.Add(match.Groups[1].Value);
            }
        }

        return registeredMethods;
    }
}
