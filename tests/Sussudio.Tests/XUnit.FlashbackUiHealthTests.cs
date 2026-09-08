using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Ownership checks for the runtime health subscription, message presentation,
/// and presentation-driven prewarm request. Backend lifetimes execute in
/// FlashbackHealthLifetimeTests.
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
        Assert.Contains("FlashbackVoluntaryLiveReasons.Contains(change.Reason)", method);
        Assert.Contains("_dispatcherQueue.TryEnqueue(", method);
        Assert.Contains("FlashbackSnapToLiveHealthMessage", method);
        Assert.True(method.IndexOf("IsCurrentFlashbackPlaybackStateChange(change)", StringComparison.Ordinal) >
            method.IndexOf("_dispatcherQueue.TryEnqueue(", StringComparison.Ordinal));
        Assert.Contains("Volatile.Read(ref _disposeState) != 0", method);
        Assert.Contains("FlashbackHealthMessage == FlashbackDeadBackendHealthMessage", method);
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
    public void RuntimeIngressOwnsStablePlaybackSubscription_OutsideTimelinePolling()
    {
        var source = ViewModelSource();
        var pollMethod = global::Program.ExtractDeclaredMemberCode(source, "public void UpdateFlashbackBufferStatus()");
        Assert.DoesNotContain("StateChanged", pollMethod);
        Assert.DoesNotContain("FlashbackPlaybackControllerInstance", source);
        Assert.DoesNotContain("_flashbackHealthSubscribedController", source);

        var lifecycle = RuntimeContractSource.ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs");
        var attach = global::Program.ExtractDeclaredMemberCode(lifecycle, "public void Attach()");
        var detach = global::Program.ExtractDeclaredMemberCode(lifecycle, "public void Detach()");
        Assert.Contains("_context.AttachFlashbackPlaybackStateChanged(_context.OnFlashbackPlaybackStateChanged);", attach);
        Assert.Contains("_context.DetachFlashbackPlaybackStateChanged(_context.OnFlashbackPlaybackStateChanged);", detach);
    }

    [Fact]
    public void ExistingRuntimeTimerRefreshesHealth_EvenWithoutPreviewOrTimeline()
    {
        var source = ViewModelSource();
        var healthMethod = global::Program.ExtractDeclaredMemberCode(source, "private void UpdateFlashbackHealthStatus()");
        Assert.Contains("IsFlashbackEnabled && !_sessionCoordinator.IsFlashbackActive", healthMethod);
        Assert.Contains("FlashbackHealthMessage = FlashbackDeadBackendHealthMessage;", healthMethod);
        Assert.DoesNotContain("IsFlashbackTimelineVisible", healthMethod);

        var lifecycle = RuntimeContractSource.ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs");
        var timer = global::Program.ExtractDeclaredMemberCode(lifecycle, "private void SetupTimer()");
        Assert.True(timer.IndexOf("_context.UpdateFlashbackHealthStatus();", StringComparison.Ordinal) >= 0);
        Assert.True(timer.IndexOf("_context.UpdateFlashbackHealthStatus();", StringComparison.Ordinal) <
            timer.IndexOf("if (_context.IsRecording())", StringComparison.Ordinal));
        Assert.Contains("_context.UpdateFlashbackHealthStatus();",
            global::Program.ExtractDeclaredMemberCode(lifecycle, "public void InitializePresentation()"));
        Assert.Contains("_context.StopFlashbackHealthPresentation();",
            global::Program.ExtractDeclaredMemberCode(lifecycle, "public void StopForDispose()"));
    }

    [Fact]
    public void PreWarmRequestRemainsPresentationDriven_WithBackendOwnedReadiness()
    {
        var uiSource = UiControllersSource();
        var startPolling = global::Program.ExtractDeclaredMemberCode(uiSource, "public void StartStatusPolling()");
        Assert.Contains("_context.ViewModel.PreWarmFlashbackPlayback();", startPolling);

        var vmSource = ViewModelSource();
        Assert.Contains("public void PreWarmFlashbackPlayback() => _sessionCoordinator.PreWarmFlashbackPlayback();", vmSource);
        Assert.DoesNotContain("_flashbackPreWarmedController", vmSource);

        var backend = RuntimeContractSource.ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var preWarmMethod = global::Program.ExtractDeclaredMemberCode(backend, "public void PreWarmPlayback()");
        Assert.Contains("controller == null || controller.IsDisposed || !controller.IsInitialized", preWarmMethod);
        Assert.Contains("controller.PreWarm();", preWarmMethod);

        // The poll must re-attempt: polling starts before the controller is
        // initialized, and the controller is rebuilt on backend cycles.
        var pollMethod = global::Program.ExtractDeclaredMemberCode(vmSource, "public void UpdateFlashbackBufferStatus()");
        Assert.Contains("PreWarmFlashbackPlayback();", pollMethod);
    }

    [Fact]
    public void HealthTimerTeardownReturnsToDispatcherOwner_AndSchedulingHonorsDisposal()
    {
        var source = ViewModelSource();
        var stop = global::Program.ExtractDeclaredMemberCode(source, "private void StopFlashbackHealthPresentation()");
        Assert.Contains("if (!_dispatcherQueue.HasThreadAccess)", stop);
        Assert.Contains("_dispatcherQueue.TryEnqueue(StopFlashbackHealthPresentation)", stop);
        Assert.True(stop.IndexOf("return;", StringComparison.Ordinal) <
            stop.IndexOf("_flashbackHealthClearTimer = null;", StringComparison.Ordinal));
        Assert.Contains("var timer = _flashbackHealthClearTimer;", stop);
        Assert.Contains("timer.Tick -= FlashbackHealthClearTimer_Tick;", stop);

        var schedule = global::Program.ExtractDeclaredMemberCode(source, "private void ScheduleFlashbackHealthMessageClear()");
        Assert.Contains("Volatile.Read(ref _disposeState) != 0", schedule);
        Assert.Contains("var timer = _flashbackHealthClearTimer ??= _dispatcherQueue.CreateTimer();", schedule);
    }
}
