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
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionBindings.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionPresentation.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionNormalizer.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.AudioPresentation.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Interaction.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Layout.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Output.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Screenshot.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.Recording.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.ShellPreview.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.Chrome.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.Launch.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.SplashPhrase.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.NativeBootstrap.Tests.cs",
        "tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.Tests.cs",
        "tests/Sussudio.Tests/PreviewRuntimeSnapshotController.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioControls.DeviceAudio.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioControls.GainAndMonitoring.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.NativeXuAudioControlService.AudioMeters.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioDeviceSelectionPolicy.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.AudioRuntime.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.ReinitTransition.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.SessionController.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.SessionReinit.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Signals.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.StartupStopOrdering.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Watchdog.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.DeviceFormatProbeRetarget.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.Ownership.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.PolicyBehavior.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Ownership.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Behavior.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Ownership.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.VideoFormat.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.Capture.SettingsProjection.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DependencyComposition.CaptureDevice.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DependencyComposition.Presentation.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DependencyComposition.Tests.cs",
        "tests/Sussudio.Tests/MainViewModel.DependencyComposition.Runtime.Tests.cs"
    };

    private static readonly string[] PresentationPreviewStatsContractTestFiles =
    {
        "tests/Sussudio.Tests/StatsOverlay.Lifecycle.Tests.cs",
        "tests/Sussudio.Tests/StatsDockPresentation.Tests.cs",
        "tests/Sussudio.Tests/StatsPresentation.Ownership.Tests.cs",
        "tests/Sussudio.Tests/XUnit.StatsHardwareRowsTests.cs",
        "tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs"
    };

    private static readonly HashSet<string> PresentationPreviewUiOwnershipCatalogExclusions = new(StringComparer.Ordinal)
    {
        "CaptureErrors_RefreshViewModelRuntimeFlags"
    };

    internal static Task PresentationPreviewHarnessRegistration_CoversUiOwnershipChecks()
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
