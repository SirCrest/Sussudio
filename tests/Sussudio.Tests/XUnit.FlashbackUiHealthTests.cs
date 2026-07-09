using System;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Source-contract tests for UI health surfacing (Task 5 of the 2026-07-08
/// flashback bulletproofing plan): the involuntary snap-to-live notice, the
/// dead-backend banner, and the pre-warm hook relocated from Task 6.
/// </summary>
public sealed class FlashbackUiHealthTests
{
    private static string ViewModelSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/ViewModels/MainViewModel.FlashbackState.cs"));

    private static string UiControllersSource() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs"));

    private static string MainWindowXaml() =>
        File.ReadAllText(TestPaths.Repo("Sussudio/MainWindow.xaml"));

    [Fact]
    public void FlashbackHealthMessage_PropertyExists_AndIsRaisedThroughPropertyChangedSwitch()
    {
        var vmSource = ViewModelSource();
        Assert.Contains("public partial string FlashbackHealthMessage { get; set; }", vmSource);

        // CommunityToolkit's [ObservableProperty] raises INotifyPropertyChanged
        // automatically; the UI-facing switch in FlashbackUiControllers.cs is
        // what actually reacts to it and must carry a case for the new property.
        var uiSource = UiControllersSource();
        Assert.Contains("case nameof(MainViewModel.FlashbackHealthMessage):", uiSource);
        Assert.Contains("public required Action UpdateHealthMessage { get; init; }", uiSource);
    }

    [Fact]
    public void InvoluntaryLiveReasonFilter_ExcludesExactlyTheVoluntarySet()
    {
        var vmSource = ViewModelSource();
        Assert.Contains(
            "new(StringComparer.Ordinal) { \"\", \"user\", \"go_live\", \"thread_stop\" }",
            vmSource);
    }

    [Fact]
    public void OnFlashbackPlaybackStateChanged_SkipsVoluntaryReasons_AndMarshalsToDispatcher()
    {
        var source = ViewModelSource();
        var method = SourceSlice.Method(source, "private void OnFlashbackPlaybackStateChanged(");
        Assert.Contains("FlashbackVoluntaryLiveReasons.Contains(reason)", method);
        Assert.Contains("_dispatcherQueue.TryEnqueue(", method);
        Assert.Contains("FlashbackSnapToLiveHealthMessage", method);
    }

    [Fact]
    public void MainWindowXaml_HasNewFlashbackHealthInfoBar_AndDoesNotRenameDiskWarningInfoBar()
    {
        var xaml = MainWindowXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"FlashbackHealthInfoBar\"", xaml);

        // Guard against accidental rename of the pre-existing AutomationId this
        // task's InfoBar sits next to (hard project rail: never rename an
        // existing AutomationId).
        Assert.Contains("AutomationProperties.AutomationId=\"DiskWarningInfoBar\"", xaml);
    }

    [Fact]
    public void UpdateFlashbackBufferStatus_ResubscribesOnControllerInstanceChange()
    {
        var source = ViewModelSource();
        var pollMethod = SourceSlice.Method(source, "public void UpdateFlashbackBufferStatus()");
        Assert.Contains("RefreshFlashbackStateChangedSubscription();", pollMethod);
        Assert.Contains("DetachFlashbackStateChangedSubscription();", pollMethod);

        var refreshMethod = SourceSlice.Method(source, "private void RefreshFlashbackStateChangedSubscription()");
        // Must cache the last-seen instance, unsubscribe from the stale one,
        // and subscribe to the new one — the controller is rebuilt on every
        // backend cycle (FlashbackBackendResources.CycleSinkOnlyAsync).
        Assert.Contains("ReferenceEquals(current, _flashbackHealthSubscribedController)", refreshMethod);
        Assert.Contains("_flashbackHealthSubscribedController.StateChanged -= OnFlashbackPlaybackStateChanged;", refreshMethod);
        Assert.Contains("current.StateChanged += OnFlashbackPlaybackStateChanged;", refreshMethod);
    }

    [Fact]
    public void UpdateFlashbackBufferStatus_SetsPersistentDeadBackendBanner_WhenEnabledButInactive()
    {
        var source = ViewModelSource();
        var pollMethod = SourceSlice.Method(source, "public void UpdateFlashbackBufferStatus()");
        // The dead-backend branch lives inside the `!bufferStatus.IsActive` guard
        // and is gated on the enabled toggle so a user-initiated disable doesn't
        // read as a failure.
        Assert.Contains("if (IsFlashbackEnabled)", pollMethod);
        Assert.Contains("FlashbackHealthMessage = FlashbackDeadBackendHealthMessage;", pollMethod);
    }

    [Fact]
    public void PreWarmFlashbackPlayback_IsCalledOncePerInstance_FromStartStatusPolling()
    {
        var uiSource = UiControllersSource();
        var startPolling = SourceSlice.Method(uiSource, "public void StartStatusPolling()");
        Assert.Contains("_context.ViewModel.PreWarmFlashbackPlayback();", startPolling);

        var vmSource = ViewModelSource();
        var preWarmMethod = SourceSlice.Method(vmSource, "public void PreWarmFlashbackPlayback()");
        // Guard: no-op on missing/disposed controller or one already pre-warmed.
        Assert.Contains("controller == null || controller.IsDisposed || ReferenceEquals(controller, _flashbackPreWarmedController)", preWarmMethod);
        Assert.Contains("controller.PreWarm();", preWarmMethod);
    }
}

// TestPaths/SourceSlice do not exist as shared helpers in the test project (the
// established convention here is Assembly.LoadFrom + reflection against the
// staged Sussudio.dll — see MIGRATION.md — rather than a compile-time
// ProjectReference or a shared source-slicing utility). Per the plan's fallback
// instruction, these are private, file-scoped copies rather than edits to any
// shared test file. (Mirrors the copy in XUnit.FlashbackSinkHardeningTests.cs.)
file static class TestPaths
{
    public static string Repo(string relativePath) => Path.Combine(FindRepoRoot(), relativePath);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sussudio.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root from '{AppContext.BaseDirectory}'.");
    }
}

file static class SourceSlice
{
    /// <summary>
    /// Returns the source text of the method whose declaration starts with
    /// <paramref name="signaturePrefix"/> (e.g. "private void Foo"), from its
    /// signature through the matching closing brace of its body.
    /// </summary>
    public static string Method(string source, string signaturePrefix)
    {
        var start = source.IndexOf(signaturePrefix, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Could not find method starting with '{signaturePrefix}'.");
        }

        var braceOpen = source.IndexOf('{', start);
        if (braceOpen < 0)
        {
            throw new InvalidOperationException($"Could not find method body open brace for '{signaturePrefix}'.");
        }

        var depth = 0;
        for (var i = braceOpen; i < source.Length; i++)
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
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not find matching closing brace for '{signaturePrefix}'.");
    }
}
