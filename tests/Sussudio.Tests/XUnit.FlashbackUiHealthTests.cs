using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Source-contract tests for UI health surfacing (Task 5 of the 2026-07-08
/// flashback bulletproofing plan): the involuntary snap-to-live notice, the
/// dead-backend banner, and the pre-warm hook relocated from Task 6.
/// </summary>
public sealed class FlashbackUiHealthTests
{
    private static string ViewModelSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs");

    private static string UiControllersSource()
        => RuntimeContractSource.ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs");

    private static string MainWindowXaml()
        => RuntimeContractSource.ReadRepoFile("Sussudio/MainWindow.xaml");

    [Fact]
    public void FlashbackHealthMessage_PropertyExists()
    {
        var vmSource = ViewModelSource();
        Assert.Contains("public partial string FlashbackHealthMessage { get; set; }", vmSource);
    }

    [Fact]
    public void FlashbackHealthMessage_PropertyChangedControllerRoutesToHealthPresenter()
    {
        var uiSource = UiControllersSource();
        Assert.Contains("case nameof(MainViewModel.FlashbackHealthMessage):", uiSource);
        Assert.Contains("public required Action UpdateHealthMessage { get; init; }", uiSource);
    }

    [Fact]
    public void InvoluntaryLiveReasonFilter_ExcludesExactlyTheVoluntarySet()
    {
        var viewModelType = SussudioAssembly.Load().GetType(
            "Sussudio.ViewModels.MainViewModel", throwOnError: true)!;
        var reasonsField = viewModelType.GetField(
            "FlashbackVoluntaryLiveReasons",
            BindingFlags.Static | BindingFlags.NonPublic);

        var reasons = Assert.IsAssignableFrom<ISet<string>>(reasonsField?.GetValue(null));
        Assert.Equal(
            new[] { "", "go_live", "thread_stop", "user" },
            reasons.OrderBy(reason => reason, StringComparer.Ordinal));
    }

    [Fact]
    public void OnFlashbackPlaybackStateChanged_SkipsVoluntaryReasons_AndMarshalsToDispatcher()
    {
        var source = ViewModelSource();
        var method = global::Program.ExtractDeclaredMemberCode(source, "private void OnFlashbackPlaybackStateChanged(");
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
        var pollMethod = global::Program.ExtractDeclaredMemberCode(source, "public void UpdateFlashbackBufferStatus()");
        Assert.Contains("RefreshFlashbackStateChangedSubscription();", pollMethod);
        Assert.Contains("DetachFlashbackStateChangedSubscription();", pollMethod);

        var refreshMethod = global::Program.ExtractDeclaredMemberCode(source, "private void RefreshFlashbackStateChangedSubscription()");
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
        var pollMethod = global::Program.ExtractDeclaredMemberCode(source, "public void UpdateFlashbackBufferStatus()");
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
        var startPolling = global::Program.ExtractDeclaredMemberCode(uiSource, "public void StartStatusPolling()");
        Assert.Contains("_context.ViewModel.PreWarmFlashbackPlayback();", startPolling);

        var vmSource = ViewModelSource();
        var preWarmMethod = global::Program.ExtractDeclaredMemberCode(vmSource, "public void PreWarmFlashbackPlayback()");
        // Guard: no-op on missing/disposed/uninitialized controller or one already
        // pre-warmed. The IsInitialized gate is load-bearing: PreWarm() no-ops
        // silently before Initialize(), so latching early would consume the one
        // warm-up this instance gets.
        Assert.Contains("controller == null || controller.IsDisposed || !controller.IsInitialized ||", preWarmMethod);
        Assert.Contains("ReferenceEquals(controller, _flashbackPreWarmedController)", preWarmMethod);
        Assert.Contains("controller.PreWarm();", preWarmMethod);

        // The poll must re-attempt: polling starts before the controller is
        // initialized, and the controller is rebuilt on backend cycles.
        var pollMethod = global::Program.ExtractDeclaredMemberCode(vmSource, "public void UpdateFlashbackBufferStatus()");
        Assert.Contains("PreWarmFlashbackPlayback();", pollMethod);
    }
}
