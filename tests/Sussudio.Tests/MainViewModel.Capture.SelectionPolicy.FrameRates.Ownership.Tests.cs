using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task SourceFilteredFrameRatesAreAlwaysUnlocked()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/FrameRateTimingPolicy.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildFrameRateOptions();");
        AssertContains(frameRateRebuildControllerText, "FrameRateSourceFilterPolicy.Apply(");
        AssertContains(frameRateRebuildControllerText, "true);");
        AssertContains(mainViewModelText, "RebuildFrameRateOptions();");
        AssertContains(sourceFilterPolicyText, "showAllCaptureOptions");
        AssertContains(sourceFilterPolicyText, "!IsSourceFilteredFrameRateDisableReason(option.DisableReason)");
        AssertContains(sourceFilterPolicyText, "CloneOption(option, isEnabled: true, disableReason: string.Empty)");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsSourceFilteredFrameRateDisableReason(");
        AssertDoesNotContain(captureModeTransactionsText, "higher capture fps duplicates frames");

        return Task.CompletedTask;
    }

    internal static Task FrameRateSourceFilterPolicy_LivesInFocusedHelper()
    {
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = captureModeOptionsControllerText;
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/FrameRateTimingPolicy.cs").Replace("\r\n", "\n");
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs").Replace("\r\n", "\n");
        var repoRoot = GetRepoRoot();

        AssertContains(frameRateOptionsText, "/// Capture-device, resolution, and frame-rate selection reactions.");
        AssertContains(frameRateOptionsText, "private void SelectAutoFrameRate(bool rebuildOptions)");
        AssertContains(frameRateOptionsText, "private void RebuildFrameRateOptions()");
        AssertContains(frameRateOptionsText, "=> _captureModeOptionRebuildController.RebuildFrameRateOptions();");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(captureModeTransactionsText, "private void RebuildFrameRateOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildFrameRateOptions();");
        AssertContains(captureModeOptionsControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureModeOptionsControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(captureModeOptionsControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(frameRateRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(frameRateRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(frameRateRebuildControllerText, "public void RebuildFrameRateOptions()");
        AssertContains(frameRateRebuildControllerText, "var sourceRate = _frameRateTimingResolver.ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);");
        AssertContains(frameRateRebuildControllerText, "_context.AvailableFrameRates.Clear();");
        AssertContains(frameRateRebuildControllerText, "_context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertDoesNotContain(captureModeOptionsControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(frameRateRebuildControllerText, "_viewModel.");
        AssertContains(modeSelectionText, "private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)");
        AssertContains(sourceFilterPolicyText, "internal static class FrameRateSourceFilterPolicy");
        AssertContains(sourceFilterPolicyText, "internal static FrameRateSourceFilterResult Apply(");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.FrameRateOptions.cs")), "old frame-rate options partial folded into capture selection owner");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.FrameRateSourceFilterPolicy.cs")), "old nested frame-rate source-filter partial removed");
        AssertContains(sourceFilterPolicyText, "IReadOnlyCollection<FrameRateTimingVariant> resolutionTimingVariants");
        AssertContains(sourceFilterPolicyText, "option.FriendlyValue > sourceFriendlyRate.Value + 0.01");
        AssertContains(sourceFilterPolicyText, "option.Value > sourceRate.Value + 0.03");
        AssertContains(sourceFilterPolicyText, "higher capture fps duplicates frames");
        AssertContains(sourceFilterPolicyText, "duplicate variant is hidden");
        AssertContains(sourceFilterPolicyText, "not a clean divisor");
        AssertDoesNotContain(sourceFilterPolicyText, "private readonly record struct FrameRateTimingVariant(");
        AssertDoesNotContain(sourceFilterPolicyText, "private IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertDoesNotContain(sourceFilterPolicyText, "AvailableFrameRates.Clear();");
        AssertDoesNotContain(sourceFilterPolicyText, "ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(sourceFilterPolicyText, "DetectedSourceFrameRate =");

        return Task.CompletedTask;
    }

    internal static Task FrameRateAutoSelectionPolicy_LivesInFocusedHelper()
    {
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var autoSelectionPolicyText = ReadRepoFile("Sussudio/ViewModels/FrameRateTimingPolicy.cs").Replace("\r\n", "\n");
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureState.cs").Replace("\r\n", "\n");
        var repoRoot = GetRepoRoot();

        AssertContains(frameRateOptionsText, "FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(");
        AssertContains(frameRateRebuildControllerText, "FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(");
        AssertContains(frameRateRebuildControllerText, "_context.AvailableFrameRates.Clear();");
        AssertContains(frameRateRebuildControllerText, "_context.AvailableFrameRates.Add(option);");
        AssertContains(frameRateRebuildControllerText, "_context.SetIsAutoFrameRateSelected(selection.SelectAutoOption);");
        AssertContains(frameRateRebuildControllerText, "_context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertContains(frameRateRebuildControllerText, "_context.SetPendingSdrAutoSelectionForDeviceChange(false);");
        AssertDoesNotContain(frameRateOptionsText, "OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))");
        AssertDoesNotContain(captureModeTransactionsText, "OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))");
        AssertContains(autoSelectionPolicyText, "internal static class FrameRateAutoSelectionPolicy");
        AssertContains(autoSelectionPolicyText, "internal readonly record struct FrameRateAutoSelectionSource(");
        AssertContains(autoSelectionPolicyText, "internal sealed record FrameRateAutoSelectionRequest(");
        AssertContains(autoSelectionPolicyText, "internal sealed record FrameRateAutoSelection(");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.FrameRateAutoSelectionPolicy.cs")), "old nested frame-rate auto-selection partial removed");
        AssertContains(autoSelectionPolicyText, "internal static FrameRateAutoSelection Select(FrameRateAutoSelectionRequest request)");
        AssertContains(autoSelectionPolicyText, "request.PendingSdrAutoSelectionForDeviceChange");
        AssertContains(autoSelectionPolicyText, "request.PendingSdrAutoFriendlyFrameRateBucket.Value");
        AssertContains(autoSelectionPolicyText, ".OrderBy(option => Math.Abs(option.Value - source.Rate.Value))");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily)");
        AssertContains(autoSelectionPolicyText, "optionFamily == source.TimingFamily");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFrameRateMatch(option.Value, previousRate)");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, 60)");
        AssertContains(autoSelectionPolicyText, "FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, 30)");
        AssertDoesNotContain(autoSelectionPolicyText, "AvailableFrameRates.Clear();");
        AssertDoesNotContain(autoSelectionPolicyText, "ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(autoSelectionPolicyText, "SelectedFrameRate =");
        AssertContains(modeSelectionText, "SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);");
        AssertContains(modeSelectionText, "SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;");
        AssertContains(modeSelectionText, "SelectedExactFrameRateArg = selected?.Rational;");

        return Task.CompletedTask;
    }

    internal static Task FrameRateTimingPolicy_LivesInFocusedPartial()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var timingResolverText = captureModeOptionsControllerText;
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs").Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var compositionText = rootText;
        var timingPolicyText = ReadRepoFile("Sussudio/ViewModels/FrameRateTimingPolicy.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "private void UpdateSelectedFormat()");
        AssertContains(captureModeTransactionsText, "private void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.UpdateSelectedFormat();");
        AssertContains(captureModeOptionsControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionsControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds, HDR/SDR changes,");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertDoesNotContain(captureModeTransactionsText, "private FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool TryInferFrameRateTimingFamily(");
        AssertDoesNotContain(captureModeTransactionsText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(
            ReadRepoFile("Sussudio/ViewModels/CaptureFormatSelectionPolicy.cs").Replace("\r\n", "\n"),
            "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(rootText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertContains(compositionText, "private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;");
        AssertContains(controllerGraphText, "internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)");
        AssertContains(controllerGraphText, "new MainViewModelFrameRateTimingResolverContext");
        AssertContains(timingResolverText, "namespace Sussudio.Controllers;");
        AssertContains(timingResolverText, "internal sealed class MainViewModelFrameRateTimingResolverContext");
        AssertContains(timingResolverText, "public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }");
        AssertContains(timingResolverText, "public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }");
        AssertContains(timingResolverText, "internal sealed class MainViewModelFrameRateTimingResolver");
        AssertContains(timingResolverText, "public FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(timingResolverText, "public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(timingResolverText, "public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertContains(timingResolverText, "FrameRateTimingPolicy.BuildTimingVariants(formats)");
        AssertContains(timingResolverText, "FrameRateTimingPolicy.TryInferFrameRateTimingFamily(");
        AssertContains(timingResolverText, "CaptureResolutionSelectionPolicy.TryParseResolutionKey(");
        AssertDoesNotContain(timingResolverText, "private readonly record struct FrameRateTimingVariant(");
        AssertDoesNotContain(timingResolverText, "private static MediaFormat SelectPreferredFrameRateFormat(");
        AssertDoesNotContain(timingResolverText, "private static bool TryInferFrameRateTimingFamily(");
        AssertDoesNotContain(timingResolverText, "private static bool TryParseFrameRateRational(");
        AssertDoesNotContain(timingResolverText, "private static int GetFriendlyFrameRateBucket(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FrameRateTiming.cs")),
            "old MainViewModel frame-rate timing partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelFrameRateTimingResolver.cs")),
            "frame-rate timing resolver lives with capture mode option rebuild owner");
        AssertContains(timingPolicyText, "internal enum FrameRateTimingFamily");
        AssertContains(timingPolicyText, "internal readonly record struct FrameRateTimingVariant(int FriendlyBucket, FrameRateTimingFamily Family);");
        AssertContains(timingPolicyText, "internal static IReadOnlyList<FrameRateTimingVariant> BuildTimingVariants(IEnumerable<MediaFormat> formats)");
        AssertContains(timingPolicyText, "internal static MediaFormat SelectPreferredFrameRateFormat(");
        AssertContains(timingPolicyText, "internal static bool TryInferFrameRateTimingFamily(");
        AssertContains(timingPolicyText, "internal static bool TryParseFrameRateRational(");

        return Task.CompletedTask;
    }
}
