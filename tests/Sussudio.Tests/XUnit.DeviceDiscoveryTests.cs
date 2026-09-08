using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class DeviceDiscoveryTests
{
    private static readonly TimeSpan WorkerTestTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DiscoveryEntryPointsReturnBeforeSynchronousEnumerationFinishes(bool collectionOnly, bool audioBlocks)
    {
        var nativeCall = new BlockingNativeCall();
        var service = CreateService((audio, taskType) =>
        {
            if (audio == audioBlocks) nativeCall.Block();
            return EmptyListTask(taskType);
        });
        var entryPoint = collectionOnly ? "EnumerateVideoCaptureDevicesAsync" : "EnumerateCaptureDeviceDiscoveryAsync";

        var result = await AssertWorkerHandoffAsync(nativeCall, () => InvokeResultAsync(service, entryPoint, false));

        Assert.Empty(collectionOnly ? (IEnumerable)result : Get<IEnumerable>(result, "CaptureDevices"));
        if (!collectionOnly) Assert.True(Get<bool>(result, "Succeeded"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DiscoveryEntryPointsReturnBeforeSynchronousInlineProbeFinishes(bool collectionOnly)
    {
        var nativeCall = new BlockingNativeCall();
        var service = CreateProbeService(taskType =>
        {
            nativeCall.Block();
            return EmptyListTask(taskType);
        });
        var entryPoint = collectionOnly ? "EnumerateVideoCaptureDevicesAsync" : "EnumerateCaptureDeviceDiscoveryAsync";

        var result = await AssertWorkerHandoffAsync(nativeCall, () => InvokeResultAsync(service, entryPoint, true));

        var devices = collectionOnly ? (IEnumerable)result : Get<IEnumerable>(result, "CaptureDevices");
        Assert.Single(devices.Cast<object>());
        if (!collectionOnly) Assert.True(Get<bool>(result, "Succeeded"));
    }

    [Fact]
    public async Task AudioOnlyEnumerationReturnsBeforeNativeWorkWithoutEnumeratingVideo()
    {
        var nativeCall = new BlockingNativeCall();
        var videoCalls = 0;
        var service = CreateService((audio, taskType) =>
        {
            if (audio) nativeCall.Block();
            else Interlocked.Increment(ref videoCalls);
            return EmptyListTask(taskType);
        });

        var result = await AssertWorkerHandoffAsync(nativeCall,
            () => InvokeResultAsync(service, "EnumerateAudioCaptureEndpointsAsync"));

        Assert.Empty((IEnumerable)result);
        Assert.Equal(0, videoCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioOnlyEnumerationPreservesNativeFailure(bool synchronousFailure)
    {
        var service = CreateService((_, taskType) =>
        {
            var failure = new IOException("audio endpoints unavailable");
            if (synchronousFailure) throw failure;
            return FailedTask(taskType, failure);
        });

        var observedFailure = await Assert.ThrowsAsync<IOException>(
            () => InvokeResultAsync(service, "EnumerateAudioCaptureEndpointsAsync"));

        Assert.Equal("audio endpoints unavailable", observedFailure.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioRefreshKeepsSelectionMadeWhileEnumerationIsPending(bool microphoneSelection)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enumeration = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var fixture = new AudioRefreshFixture(() =>
        {
            entered.TrySetResult();
            return enumeration.Task;
        });
        var chosenDevice = CreateAudioEndpoint("chosen-during-scan");
        var endpoints = new[] { fixture.OriginalAudio, fixture.OriginalMicrophone, chosenDevice };
        var refresh = fixture.RefreshAsync();
        try
        {
            await entered.Task.WaitAsync(WorkerTestTimeout);
            fixture.SelectDevice(microphoneSelection, chosenDevice);
            enumeration.SetResult(CreateAudioEndpointList(endpoints));
            await refresh.WaitAsync(WorkerTestTimeout);

            fixture.AssertState(
                microphoneSelection ? fixture.OriginalAudio : chosenDevice,
                microphoneSelection ? chosenDevice : fixture.OriginalMicrophone,
                endpoints);
        }
        finally
        {
            enumeration.TrySetResult(CreateAudioEndpointList(endpoints));
            await refresh.WaitAsync(WorkerTestTimeout);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioRefreshIgnoresOlderCompletionAfterNewerRefresh(bool newerFails)
    {
        var olderEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var newerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var olderEnumeration = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newerEnumeration = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var fixture = new AudioRefreshFixture(() =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                olderEntered.TrySetResult();
                return olderEnumeration.Task;
            }

            newerEntered.TrySetResult();
            return newerEnumeration.Task;
        });
        var olderAudio = CreateAudioEndpoint("older-audio");
        var newerAudio = CreateAudioEndpoint("newer-audio");
        var expectedAudio = newerFails ? fixture.OriginalAudio : newerAudio;
        var expectedEndpoints = new[] { expectedAudio, fixture.OriginalMicrophone };
        var olderRefresh = fixture.RefreshAsync();
        Task? newerRefresh = null;
        try
        {
            await olderEntered.Task.WaitAsync(WorkerTestTimeout);
            newerRefresh = fixture.RefreshAsync();
            await newerEntered.Task.WaitAsync(WorkerTestTimeout);
            if (newerFails) newerEnumeration.SetException(new IOException("newer audio scan failed"));
            else newerEnumeration.SetResult(CreateAudioEndpointList(newerAudio, fixture.OriginalMicrophone));
            await newerRefresh.WaitAsync(WorkerTestTimeout);
            fixture.AssertState(expectedAudio, fixture.OriginalMicrophone, expectedEndpoints);

            olderEnumeration.SetResult(CreateAudioEndpointList(olderAudio, fixture.OriginalMicrophone));
            await olderRefresh.WaitAsync(WorkerTestTimeout);

            fixture.AssertState(expectedAudio, fixture.OriginalMicrophone, expectedEndpoints);
            Assert.Equal(2, fixture.EnumerationCalls);
        }
        finally
        {
            olderEnumeration.TrySetResult(CreateAudioEndpointList(expectedEndpoints));
            newerEnumeration.TrySetResult(CreateAudioEndpointList(expectedEndpoints));
            await Task.WhenAll(olderRefresh, newerRefresh ?? Task.CompletedTask).WaitAsync(WorkerTestTimeout);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AudioRefreshDoesNotCommitAfterDisposalBegins(bool disposeBeforeInvocation)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enumeration = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var fixture = new AudioRefreshFixture(() =>
        {
            entered.TrySetResult();
            return enumeration.Task;
        });
        if (disposeBeforeInvocation) fixture.BeginDispose();
        var refresh = fixture.RefreshAsync();
        try
        {
            if (!disposeBeforeInvocation)
            {
                await entered.Task.WaitAsync(WorkerTestTimeout);
                fixture.BeginDispose();
                enumeration.SetResult(CreateAudioEndpointList(CreateAudioEndpoint("after-dispose"), fixture.OriginalMicrophone));
            }

            await refresh.WaitAsync(WorkerTestTimeout);

            fixture.AssertState(fixture.OriginalAudio, fixture.OriginalMicrophone,
                fixture.OriginalAudio, fixture.OriginalMicrophone);
            Assert.Equal(disposeBeforeInvocation ? 0 : 1, fixture.EnumerationCalls);
        }
        finally
        {
            enumeration.TrySetResult(CreateAudioEndpointList(fixture.OriginalAudio, fixture.OriginalMicrophone));
            await refresh.WaitAsync(WorkerTestTimeout);
        }
    }

    [Fact]
    public async Task AudioRefreshIgnoresCompletionAfterStartupAudioScan()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enumeration = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var fixture = new AudioRefreshFixture(() =>
        {
            entered.TrySetResult();
            return enumeration.Task;
        });
        var newerAudio = CreateAudioEndpoint("startup-audio");
        var endpoints = new[] { newerAudio, fixture.OriginalMicrophone };
        var refresh = fixture.RefreshAsync();
        try
        {
            await entered.Task.WaitAsync(WorkerTestTimeout);
            fixture.ApplyStartupScan(endpoints);
            fixture.AssertState(newerAudio, fixture.OriginalMicrophone, endpoints);
            enumeration.SetResult(CreateAudioEndpointList(fixture.OriginalAudio, fixture.OriginalMicrophone));
            await refresh.WaitAsync(WorkerTestTimeout);

            fixture.AssertState(newerAudio, fixture.OriginalMicrophone, endpoints);
        }
        finally
        {
            enumeration.TrySetResult(CreateAudioEndpointList(endpoints));
            await refresh.WaitAsync(WorkerTestTimeout);
        }
    }

    [Fact]
    public async Task BackgroundProbeReturnsBeforeSynchronousNativeWorkFinishes()
    {
        var nativeCall = new BlockingNativeCall();
        var service = CreateProbeService(taskType =>
        {
            nativeCall.Block();
            return EmptyListTask(taskType);
        });

        var result = await AssertWorkerHandoffAsync(nativeCall,
            () => BackgroundProbeAsync(service, CreateDevice("background-worker"), 41));

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Empty(Get<IEnumerable>(result, "Formats"));
        Assert.Equal(41L, Get<long>(result, "RequestId"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BackgroundProbesKeepConcurrencyBoundAndCapturedIdentityAfterFailure(bool synchronousFailure)
    {
        var firstCall = new BlockingNativeCall();
        var secondCall = new BlockingNativeCall();
        var queuedEntered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var concurrencySync = new object();
        var active = 0;
        var maximumActive = 0;
        var service = CreateProbeService((deviceId, taskType) =>
        {
            lock (concurrencySync)
            {
                active++;
                maximumActive = Math.Max(maximumActive, active);
            }

            try
            {
                if (deviceId == "first-worker")
                {
                    firstCall.Block();
                    var failure = new IOException("first probe failed");
                    if (synchronousFailure) throw failure;
                    return FailedTask(taskType, failure);
                }

                if (deviceId == "second-worker") secondCall.Block();
                else queuedEntered.TrySetResult(deviceId);
                return EmptyListTask(taskType);
            }
            finally
            {
                lock (concurrencySync) active--;
            }
        });
        var first = BackgroundProbeAsync(service, CreateDevice("first-worker"), 41);
        var second = BackgroundProbeAsync(service, CreateDevice("second-worker"), 41);
        Task<object>? queued = null;
        try
        {
            await Task.WhenAll(firstCall.Entered, secondCall.Entered).WaitAsync(WorkerTestTimeout);
            var device = CreateDevice("queued-worker");
            queued = BackgroundProbeAsync(service, device, 42);
            Set(device, "Id", "changed-after-scheduling");
            Set(device, "Name", "changed after scheduling");

            await Assert.ThrowsAsync<TimeoutException>(
                () => queuedEntered.Task.WaitAsync(TimeSpan.FromMilliseconds(100)));
            firstCall.Release();
            var failedResult = await first.WaitAsync(WorkerTestTimeout);
            var queuedResult = await queued.WaitAsync(WorkerTestTimeout);

            Assert.False(Get<bool>(failedResult, "Succeeded"));
            Assert.Contains("first probe failed", Get<string>(failedResult, "Error"));
            Assert.True(Get<bool>(queuedResult, "Succeeded"));
            Assert.Equal("queued-worker", await queuedEntered.Task.WaitAsync(WorkerTestTimeout));
            Assert.Equal("queued-worker", Get<string>(queuedResult, "DeviceId"));
            Assert.Equal("queued-worker", Get<string>(queuedResult, "DeviceName"));
            Assert.Equal(42L, Get<long>(queuedResult, "RequestId"));
            Assert.False(second.IsCompleted);

            secondCall.Release();
            Assert.True(Get<bool>(await second.WaitAsync(WorkerTestTimeout), "Succeeded"));
            Assert.Equal(2, maximumActive);
            Assert.Equal(0, active);
        }
        finally
        {
            firstCall.Release();
            secondCall.Release();
            await Task.WhenAll(first, second).WaitAsync(WorkerTestTimeout);
            if (queued != null) await queued.WaitAsync(WorkerTestTimeout);
        }
    }

    [Fact]
    public async Task CancellationDuringScheduledNativeDiscoveryPreservesRefreshState()
    {
        var nativeCall = new BlockingNativeCall();
        var service = CreateService((audio, taskType) =>
        {
            if (!audio) nativeCall.Block();
            return EmptyListTask(taskType);
        });
        var fixture = new RefreshFixture
        {
            Discover = () => InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false)
        };
        using var cancellation = new CancellationTokenSource();
        var invocation = InvokeOnStaThreadAsync(() => fixture.RefreshAsync(cancellation.Token));
        try
        {
            await nativeCall.Entered.WaitAsync(WorkerTestTimeout);
            var caller = await invocation.WaitAsync(WorkerTestTimeout);
            cancellation.Cancel();
            fixture.AssertOriginalState();
            nativeCall.Release();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caller.Operation);

            fixture.AssertOriginalState();
            Assert.Equal("Device scan canceled", fixture.Status);
        }
        finally
        {
            nativeCall.Release();
            var caller = await invocation.WaitAsync(WorkerTestTimeout);
            try { await caller.Operation.WaitAsync(WorkerTestTimeout); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OlderScheduledNativeDiscoveryCannotReplaceNewerRefreshOutcome(bool newerFails)
    {
        var nativeCall = new BlockingNativeCall();
        var videoCalls = 0;
        var service = CreateService((audio, taskType) =>
        {
            if (audio) return EmptyListTask(taskType);
            if (Interlocked.Increment(ref videoCalls) == 1)
            {
                nativeCall.Block();
                return EmptyListTask(taskType);
            }

            return newerFails ? FailedTask(taskType, new IOException("newer scan failed")) : EmptyListTask(taskType);
        });
        var fixture = new RefreshFixture
        {
            Discover = () => InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false)
        };
        var invocation = InvokeOnStaThreadAsync(() => fixture.RefreshAsync());
        try
        {
            await nativeCall.Entered.WaitAsync(WorkerTestTimeout);
            var caller = await invocation.WaitAsync(WorkerTestTimeout);
            await fixture.RefreshAsync();
            var newerStatus = fixture.Status;
            nativeCall.Release();
            await caller.Operation.WaitAsync(WorkerTestTimeout);

            if (newerFails) fixture.AssertOriginalState();
            else
            {
                Assert.Empty(fixture.Devices);
                Assert.Equal(42, fixture.ProbeGeneration);
                Assert.Equal(1, fixture.DeviceReplacements);
                Assert.Equal(1, fixture.AudioReplacements);
            }

            Assert.Equal(newerStatus, fixture.Status);
            Assert.Equal(2, fixture.DiscoveryCalls);
        }
        finally
        {
            nativeCall.Release();
            var caller = await invocation.WaitAsync(WorkerTestTimeout);
            await caller.Operation.WaitAsync(WorkerTestTimeout);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleBackgroundProbePreservesCachedCapabilitiesAndPendingSelection(bool fails)
    {
        var fixture = new FormatProbeFixture();
        var service = CreateProbeService(taskType => fails
            ? FailedTask(taskType, new IOException("stale probe failed"))
            : EmptyListTask(taskType));

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 40));

        Assert.Same(fixture.CachedFormat, Assert.Single(Get<IEnumerable>(fixture.Device, "SupportedFormats").Cast<object>()));
        Assert.True(Get<bool>(fixture.Device, "IsHdrCapable"));
        Assert.Same(fixture.Device, fixture.SelectedDevice);
        Assert.Same(fixture.CachedFormat, fixture.SelectedFormat);
        Assert.True(fixture.PendingAutoSelection);
        Assert.Equal(60, fixture.PendingFrameRateBucket);
        Assert.Equal(0, fixture.CapabilityRebuilds);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task EnumerationFailureHasAnExplicitPerCallOutcome(bool audioFails, bool synchronousFailure)
    {
        var service = CreateService((audio, taskType) =>
        {
            if (audio == audioFails)
            {
                var failure = new InvalidOperationException(audio ? "audio enumeration unavailable" : "video enumeration unavailable");
                if (synchronousFailure) throw failure;
                return FailedTask(taskType, failure);
            }

            return EmptyListTask(taskType);
        });

        var result = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Contains(audioFails ? "audio enumeration unavailable" : "video enumeration unavailable", Get<string>(result, "Error"));
        Assert.Empty(Get<IEnumerable>(result, "CaptureDevices"));
        Assert.Empty(Get<IEnumerable>(result, "AudioInputDevices"));
    }

    [Fact]
    public async Task SuccessfulEmptyEnumerationIsSuccessfulForBothPublicEntryPoints()
    {
        var service = CreateService((_, taskType) => EmptyListTask(taskType));

        var result = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);
        var devices = await InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", false);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Null(Get<string?>(result, "Error"));
        Assert.Empty((IEnumerable)devices);
    }

    [Fact]
    public async Task CollectionOnlyEntryPointThrowsInsteadOfReturningFailedEmptyDiscovery()
    {
        var service = CreateService((_, taskType) => FailedTask(taskType, new IOException("device transport failed")));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", false));

        Assert.Contains("device transport failed", failure.Message);
    }

    [Fact]
    public async Task ResultErrorSurvivesAFollowingSuccessfulScan()
    {
        var fail = true;
        var service = CreateService((_, taskType) => fail
            ? FailedTask(taskType, new IOException("first scan failed"))
            : EmptyListTask(taskType));

        var failedResult = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);
        fail = false;
        var successfulResult = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);

        Assert.True(Get<bool>(successfulResult, "Succeeded"));
        Assert.False(Get<bool>(failedResult, "Succeeded"));
        Assert.Contains("first scan failed", Get<string>(failedResult, "Error"));
        Assert.DoesNotContain("first scan failed", Get<string>(service, "LastDiscoverySummary"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FormatProbeFailureHasAnExplicitPerCallOutcome(bool background, bool synchronousFailure)
    {
        var service = CreateProbeService(taskType =>
        {
            var failure = new IOException("capture device busy");
            if (synchronousFailure) throw failure;
            return FailedTask(taskType, failure);
        });

        var result = await ProbeResultAsync(service, background);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Contains("capture device busy", Get<string>(result, "Error"));
        Assert.Empty(Get<IEnumerable>(result, background ? "Formats" : "CaptureDevices"));
        if (!background)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                () => InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", true));
            Assert.Contains("capture device busy", failure.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulEmptyFormatProbeRemainsSuccessful(bool background)
    {
        var service = CreateProbeService(EmptyListTask);

        var result = await ProbeResultAsync(service, background);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Null(Get<string?>(result, "Error"));
        if (background)
        {
            Assert.False(Get<bool>(result, "HasEnumeratedFormats"));
            Assert.Empty(Get<IEnumerable>(result, "Formats"));
        }
        else
        {
            var device = Assert.Single(Get<IEnumerable>(result, "CaptureDevices").Cast<object>());
            Assert.Empty(Get<IEnumerable>(device, "SupportedFormats"));
            var devices = await InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", true);
            Assert.Single(((IEnumerable)devices).Cast<object>());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormatProbeErrorSurvivesALaterSuccessfulProbeEvenWhenExceptionMessageIsEmpty(bool background)
    {
        var fail = true;
        var service = CreateProbeService(taskType => fail
            ? FailedTask(taskType, new IOException(string.Empty))
            : EmptyListTask(taskType));

        var failedResult = await ProbeResultAsync(service, background);
        fail = false;
        var successfulResult = await ProbeResultAsync(service, background);

        Assert.True(Get<bool>(successfulResult, "Succeeded"));
        Assert.False(Get<bool>(failedResult, "Succeeded"));
        Assert.Contains(nameof(IOException), Get<string>(failedResult, "Error"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedBackgroundProbePreservesCachedCapabilitiesAndSelection(bool synchronousFailure)
    {
        var fixture = new FormatProbeFixture();
        var service = CreateProbeService(taskType =>
        {
            var failure = new IOException("device disconnected during format probe");
            if (synchronousFailure) throw failure;
            return FailedTask(taskType, failure);
        });

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 41));

        Assert.Same(fixture.CachedFormat, Assert.Single(Get<IEnumerable>(fixture.Device, "SupportedFormats").Cast<object>()));
        Assert.True(Get<bool>(fixture.Device, "IsHdrCapable"));
        Assert.Same(fixture.Device, fixture.SelectedDevice);
        Assert.Same(fixture.CachedFormat, fixture.SelectedFormat);
        Assert.Equal("3840x2160", fixture.SelectedResolution);
        Assert.Equal(60d, fixture.SelectedFrameRate);
        Assert.Equal(0, fixture.CapabilityRebuilds);
        Assert.False(fixture.PendingAutoSelection);
        Assert.Null(fixture.PendingFrameRateBucket);
    }

    [Fact]
    public async Task FailedUnselectedBackgroundProbePreservesPendingSelectionForSelectedDevice()
    {
        var selectedDevice = CreateDevice("currently-selected-device");
        var fixture = new FormatProbeFixture { SelectedDevice = selectedDevice };
        var service = CreateProbeService(taskType => FailedTask(taskType, new IOException("unselected device probe failed")));

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 41));

        Assert.True(fixture.PendingAutoSelection);
        Assert.Equal(60, fixture.PendingFrameRateBucket);
        Assert.Same(selectedDevice, fixture.SelectedDevice);
        Assert.Same(fixture.CachedFormat, Assert.Single(Get<IEnumerable>(fixture.Device, "SupportedFormats").Cast<object>()));
        Assert.True(Get<bool>(fixture.Device, "IsHdrCapable"));
        Assert.Equal(0, fixture.CapabilityRebuilds);
    }

    [Fact]
    public async Task SuccessfulEmptyBackgroundProbeClearsCachedCapabilities()
    {
        var fixture = new FormatProbeFixture { SelectedDevice = null };
        var service = CreateProbeService(EmptyListTask);

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 41));

        Assert.Empty(Get<IEnumerable>(fixture.Device, "SupportedFormats"));
        Assert.False(Get<bool>(fixture.Device, "IsHdrCapable"));
    }

    [Fact]
    public async Task FailedInlineFormatProbePreservesExistingRefreshState()
    {
        var service = CreateProbeService(taskType => FailedTask(taskType, new IOException("probe failed")));
        var fixture = new RefreshFixture
        {
            Discover = () => InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", true)
        };

        await fixture.RefreshAsync();

        fixture.AssertOriginalState();
        Assert.Contains("probe failed", fixture.Status);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailedRefreshPreservesDevicesSelectionsSavedIdsAndProbeGeneration(bool throwOnScanFailure, bool enumerationThrows)
    {
        var fixture = new RefreshFixture();
        fixture.Discover = () => enumerationThrows
            ? Task.FromException<object>(new InvalidOperationException("device scan failed"))
            : Task.FromResult(CreateDiscovery(error: "device scan failed"));

        if (throwOnScanFailure)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.RefreshAsync(throwOnScanFailure: true));
            Assert.Contains("device scan failed", failure.Message);
        }
        else
        {
            await fixture.RefreshAsync();
        }

        fixture.AssertOriginalState();
        Assert.Equal("Error scanning devices: device scan failed", fixture.Status);
    }

    [Fact]
    public async Task SuccessfulEmptyRefreshCommitsTheEmptyState()
    {
        var fixture = new RefreshFixture();
        fixture.Discover = () => Task.FromResult(CreateDiscovery());

        await fixture.RefreshAsync(throwOnScanFailure: true);

        Assert.Empty(fixture.Devices);
        Assert.Null(fixture.SelectedDevice);
        Assert.Empty(fixture.AudioDevices);
        Assert.Null(fixture.SelectedAudioId);
        Assert.Null(fixture.SelectedMicrophoneId);
        Assert.Null(fixture.PendingSavedAudioId);
        Assert.Null(fixture.PendingSavedMicrophoneId);
        Assert.Equal(42, fixture.ProbeGeneration);
        Assert.Equal(1, fixture.DeviceReplacements);
        Assert.Equal(1, fixture.AudioReplacements);
        Assert.Equal(0, fixture.PreviewStarts);
        Assert.Contains("No compatible video capture devices found", fixture.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeCommitPreservesState(bool beforeInvocation)
    {
        var fixture = new RefreshFixture();
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => completion.Task;
        using var cancellation = new CancellationTokenSource();
        if (beforeInvocation) cancellation.Cancel();

        var refresh = fixture.RefreshAsync(cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(CreateDiscovery());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
        fixture.AssertOriginalState();
        Assert.Equal(beforeInvocation ? "Original status" : "Device scan canceled", fixture.Status);
        Assert.Equal(beforeInvocation ? 0 : 1, fixture.DiscoveryCalls);
    }

    [Fact]
    public async Task OlderSuccessfulScanCannotReplaceANewerSuccessfulScan()
    {
        var fixture = new RefreshFixture();
        var olderCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => olderCompletion.Task;
        var olderRefresh = fixture.RefreshAsync();
        var newerDevice = CreateDevice("newer");
        fixture.Discover = () => Task.FromResult(CreateDiscovery(devices: [newerDevice]));
        await fixture.RefreshAsync();
        var newerStatus = fixture.Status;

        olderCompletion.SetResult(CreateDiscovery(devices: [CreateDevice("older")]));
        await olderRefresh;

        Assert.Same(newerDevice, Assert.Single(fixture.Devices.Cast<object>()));
        Assert.Same(newerDevice, fixture.SelectedDevice);
        Assert.Equal(newerStatus, fixture.Status);
        Assert.Equal(42, fixture.ProbeGeneration);
        Assert.Equal(1, fixture.DeviceReplacements);
        Assert.Equal(1, fixture.PreviewStarts);
    }

    [Fact]
    public async Task SuccessfulScanKeepsASelectionMadeWhileDiscoveryWasPending()
    {
        var fixture = new RefreshFixture();
        var selectedDuringScan = CreateDevice("chosen-during-scan");
        fixture.Devices.Add(selectedDuringScan);
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => completion.Task;
        var refresh = fixture.RefreshAsync();
        fixture.SelectedDevice = selectedDuringScan;
        var refreshedSelection = CreateDevice("chosen-during-scan");

        completion.SetResult(CreateDiscovery(devices: [CreateDevice("current"), refreshedSelection]));
        await refresh;

        Assert.Same(refreshedSelection, fixture.SelectedDevice);
        Assert.Equal(1, fixture.PreviewStarts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleSuccessAfterANewerFailurePreservesTheRetainedState(bool throwOnScanFailure)
    {
        var fixture = new RefreshFixture();
        var olderCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => olderCompletion.Task;
        var olderRefresh = fixture.RefreshAsync(throwOnScanFailure: throwOnScanFailure);
        fixture.Discover = () => Task.FromResult(CreateDiscovery(error: "newer scan failed"));
        await fixture.RefreshAsync();

        olderCompletion.SetResult(CreateDiscovery());
        if (throwOnScanFailure)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => olderRefresh);
            Assert.Contains("superseded", failure.Message);
        }
        else
        {
            await olderRefresh;
        }

        fixture.AssertOriginalState();
        Assert.Equal("Error scanning devices: newer scan failed", fixture.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleFailureOrCancellationCannotReplaceNewerStatus(bool cancelOlder)
    {
        var fixture = new RefreshFixture();
        var olderCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => olderCompletion.Task;
        using var cancellation = new CancellationTokenSource();
        var olderRefresh = fixture.RefreshAsync(cancellation.Token);
        fixture.Discover = () => Task.FromResult(CreateDiscovery(error: "newer scan failed"));
        await fixture.RefreshAsync();

        if (cancelOlder)
        {
            cancellation.Cancel();
            olderCompletion.SetResult(CreateDiscovery());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => olderRefresh);
        }
        else
        {
            olderCompletion.SetException(new IOException("older scan failed"));
            await olderRefresh;
        }

        fixture.AssertOriginalState();
        Assert.Equal("Error scanning devices: newer scan failed", fixture.Status);
    }

    private sealed class AudioRefreshFixture : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Type _type = RequireType("Sussudio.ViewModels.MainViewModel");
        private readonly object _viewModel;
        private readonly Func<Task> _refresh;
        private readonly PropertyChangingEventHandler _microphoneSelectionGuard;
        private int _enumerationCalls;
        private int _unexpectedMicrophoneAssignments;

        public AudioRefreshFixture(Func<Task<object>> enumerate)
        {
            _viewModel = RuntimeHelpers.GetUninitializedObject(_type);
            SetField("_deviceService", CreateService((audio, taskType) =>
            {
                Assert.True(audio, "The audio refresh must not enumerate capture devices.");
                Interlocked.Increment(ref _enumerationCalls);
                return TypedTask(taskType.GetGenericArguments()[0], enumerate());
            }));
            SetField("_isLoadingSettings", true);
            var collectionType = typeof(ObservableCollection<>).MakeGenericType(RequireType("Sussudio.Models.AudioInputDevice"));
            SetBackingField("AudioInputDevices", NewList(collectionType, OriginalAudio, OriginalMicrophone));
            SetBackingField("MicrophoneDevices", NewList(collectionType, OriginalAudio, OriginalMicrophone));
            SetBackingField("SelectedAudioInputDevice", OriginalAudio);
            SetBackingField("SelectedMicrophoneDevice", OriginalMicrophone);

            // Correct refreshes retain the selected microphone instance. Stop an
            // unexpected assignment before its callback can read a native endpoint.
            _microphoneSelectionGuard = (_, args) =>
            {
                if (args.PropertyName != "SelectedMicrophoneDevice") return;
                Interlocked.Increment(ref _unexpectedMicrophoneAssignments);
                throw new InvalidOperationException("Unexpected microphone selection during isolated audio refresh.");
            };
            ((INotifyPropertyChanging)_viewModel).PropertyChanging += _microphoneSelectionGuard;
            _refresh = _type.GetMethod("RefreshAudioDeviceListAsync", PrivateInstance)!
                .CreateDelegate<Func<Task>>(_viewModel);
        }

        public object OriginalAudio { get; } = CreateAudioEndpoint("original-audio");
        public object OriginalMicrophone { get; } = CreateAudioEndpoint("original-microphone");
        public int EnumerationCalls => Volatile.Read(ref _enumerationCalls);

        public Task RefreshAsync() => _refresh();

        public void SelectDevice(bool microphone, object device)
            // Supply the UI selection without running unrelated endpoint-volume callbacks.
            => SetBackingField(microphone ? "SelectedMicrophoneDevice" : "SelectedAudioInputDevice", device);

        public void BeginDispose() => SetField("_disposeState", 1);

        public void ApplyStartupScan(params object[] endpoints)
            => _type.GetMethod("ApplyStartupAudioDeviceScan", PrivateInstance)!.Invoke(_viewModel,
            [
                CreateAudioEndpointList(endpoints),
                Array.CreateInstance(CaptureDeviceType, 0),
                null,
                Get<string>(Get<object>(_viewModel, "SelectedAudioInputDevice"), "Id"),
                Get<string>(Get<object>(_viewModel, "SelectedMicrophoneDevice"), "Id")
            ]);

        public void AssertState(object selectedAudio, object selectedMicrophone, params object[] endpoints)
        {
            Assert.Equal(endpoints, Get<IEnumerable>(_viewModel, "AudioInputDevices").Cast<object>().ToArray());
            Assert.Equal(endpoints, Get<IEnumerable>(_viewModel, "MicrophoneDevices").Cast<object>().ToArray());
            Assert.Same(selectedAudio, Get<object>(_viewModel, "SelectedAudioInputDevice"));
            Assert.Same(selectedMicrophone, Get<object>(_viewModel, "SelectedMicrophoneDevice"));
            Assert.Equal(0, Volatile.Read(ref _unexpectedMicrophoneAssignments));
        }

        public void Dispose()
            => ((INotifyPropertyChanging)_viewModel).PropertyChanging -= _microphoneSelectionGuard;

        private void SetField(string name, object value)
            => _type.GetField(name, PrivateInstance)!.SetValue(_viewModel, value);

        private void SetBackingField(string property, object value)
            => SetField($"<{property}>k__BackingField", value);
    }

    private static object CreateAudioEndpoint(string id)
    {
        var endpoint = Activator.CreateInstance(RequireType("Sussudio.Models.AudioInputDevice"))!;
        Set(endpoint, "Id", id);
        Set(endpoint, "Name", id);
        return endpoint;
    }

    private static object CreateAudioEndpointList(params object[] endpoints)
        => NewList(typeof(List<>).MakeGenericType(RequireType("Sussudio.Models.AudioInputDevice")), endpoints);

    private sealed class RefreshFixture
    {
        private readonly object _controller;
        private readonly object _originalDevice = CreateDevice("current");

        public RefreshFixture()
        {
            Devices = NewList(typeof(List<>).MakeGenericType(CaptureDeviceType), _originalDevice);
            SelectedDevice = _originalDevice;
            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelDeviceRefreshControllerContext"))!;
            SetDelegate(context, "SetStatusText", args => { Status = (string)args[0]!; return null; });
            SetDelegate(context, "IncrementDeviceScanGeneration", _ => ++ProbeGeneration);
            SetDelegate(context, "GetSelectedAudioInputDeviceId", _ => SelectedAudioId);
            SetDelegate(context, "GetSelectedMicrophoneDeviceId", _ => SelectedMicrophoneId);
            SetDelegate(context, "GetSelectedDeviceId", _ => SelectedDevice == null ? null : Get<string>(SelectedDevice, "Id"));
            SetDelegate(context, "EnumerateCaptureDeviceDiscoveryAsync", _ =>
            {
                DiscoveryCalls++;
                return TypedTask(DiscoveryResultType, Discover());
            });
            SetDelegate(context, "ApplyStartupAudioDeviceScan", args =>
            {
                AudioReplacements++;
                AudioDevices = ((IEnumerable)args[0]!).Cast<object>().Select(device => Get<string>(device, "Id")).ToList();
                SelectedAudioId = AudioDevices.FirstOrDefault();
                SelectedMicrophoneId = AudioDevices.FirstOrDefault();
                PendingSavedAudioId = null;
                PendingSavedMicrophoneId = null;
                return null;
            });
            SetDelegate(context, "ReplaceDevices", args =>
            {
                DeviceReplacements++;
                Devices.Clear();
                foreach (var device in (IEnumerable)args[0]!) Devices.Add(device);
                return null;
            });
            SetDelegate(context, "GetDevices", _ => Devices);
            SetDelegate(context, "BeginBackgroundFormatProbe", _ => { ProbeStarts++; return null; });
            SetDelegate(context, "GetLastDiscoverySummary", _ => "Fake discovery");
            SetDelegate(context, "SetSelectedDevice", args => { SelectedDevice = args[0]; return null; });
            SetDelegate(context, "GetSelectedDevice", _ => SelectedDevice);
            SetDelegate(context, "GetPendingSavedDeviceId", _ => PendingSavedDeviceId);
            SetDelegate(context, "SetPendingSavedDeviceId", args => { PendingSavedDeviceId = (string?)args[0]; return null; });

            _controller = Activator.CreateInstance(
                RequireType("Sussudio.Controllers.MainViewModelDeviceRefreshController"),
                context,
                CreatePreviewController())!;
        }

        public Func<Task<object>> Discover { get; set; } = () => Task.FromResult(CreateDiscovery());
        public IList Devices { get; }
        public object? SelectedDevice { get; set; }
        public List<string> AudioDevices { get; private set; } = ["audio", "microphone"];
        public string? SelectedAudioId { get; private set; } = "audio";
        public string? SelectedMicrophoneId { get; private set; } = "microphone";
        public string? PendingSavedDeviceId { get; private set; } = "saved-video";
        public string? PendingSavedAudioId { get; private set; } = "saved-audio";
        public string? PendingSavedMicrophoneId { get; private set; } = "saved-microphone";
        public string Status { get; private set; } = "Original status";
        public long ProbeGeneration { get; private set; } = 41;
        public int DiscoveryCalls { get; private set; }
        public int DeviceReplacements { get; private set; }
        public int AudioReplacements { get; private set; }
        public int ProbeStarts { get; private set; }
        public int PreviewStarts { get; private set; }

        public Task RefreshAsync(CancellationToken cancellationToken = default, bool throwOnScanFailure = false)
            => (Task)_controller.GetType().GetMethod("RefreshDevicesAsync")!.Invoke(
                _controller, [cancellationToken, throwOnScanFailure])!;

        public void AssertOriginalState()
        {
            Assert.Same(_originalDevice, Assert.Single(Devices.Cast<object>()));
            Assert.Same(_originalDevice, SelectedDevice);
            Assert.Equal(new[] { "audio", "microphone" }, AudioDevices);
            Assert.Equal("audio", SelectedAudioId);
            Assert.Equal("microphone", SelectedMicrophoneId);
            Assert.Equal("saved-video", PendingSavedDeviceId);
            Assert.Equal("saved-audio", PendingSavedAudioId);
            Assert.Equal("saved-microphone", PendingSavedMicrophoneId);
            Assert.Equal(41, ProbeGeneration);
            Assert.Equal(0, DeviceReplacements);
            Assert.Equal(0, AudioReplacements);
            Assert.Equal(0, ProbeStarts);
            Assert.Equal(0, PreviewStarts);
        }

        private object CreatePreviewController()
        {
            // This fixture exercises the real no-device preview branch, which never
            // reaches the session coordinator or native capture. Reinitialization is
            // unused because refresh starts preview with userInitiated: false.
            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelPreviewLifecycleControllerContext"))!;
            SetDelegate(context, "CreateReinitializeController", _ => RuntimeHelpers.GetUninitializedObject(
                RequireType("Sussudio.Controllers.MainViewModelPreviewReinitializeController")));
            SetDelegate(context, "SelectedDevice", _ => null);
            SetDelegate(context, "IsInitialized", _ => false);
            SetDelegate(context, "ShouldStartAudioPreview", _ => false);
            SetDelegate(context, "RaisePreviewStartRequested", _ => { PreviewStarts++; return null; });
            SetDelegate(context, "SetStatusText", _ => null);
            SetDelegate(context, "SetIsInitialized", _ => null);
            return Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelPreviewLifecycleController"), context)!;
        }
    }

    private sealed class FormatProbeFixture
    {
        private readonly object _controller;

        public FormatProbeFixture()
        {
            CachedFormat = Activator.CreateInstance(RequireType("Sussudio.Models.MediaFormat"))!;
            Set(CachedFormat, "Width", 3840u);
            Set(CachedFormat, "Height", 2160u);
            Set(CachedFormat, "FrameRate", 60d);
            Set(CachedFormat, "PixelFormat", "P010");
            Set(CachedFormat, "IsHdr", true);
            Get<IList>(Device, "SupportedFormats").Add(CachedFormat);
            Set(Device, "IsHdrCapable", true);
            SelectedDevice = Device;
            SelectedFormat = CachedFormat;

            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelDeviceFormatProbeControllerContext"))!;
            SetDelegate(context, "TryEnqueueOnUiThread", args => { ((Action)args[0]!)(); return true; });
            SetDelegate(context, "ReadDeviceScanGeneration", _ => 41L);
            SetDelegate(context, "FindDeviceById", args => (string)args[0]! == Get<string>(Device, "Id") ? Device : null);
            SetDelegate(context, "SetPendingSdrAutoSelectionForDeviceChange", args => { PendingAutoSelection = (bool)args[0]!; return null; });
            SetDelegate(context, "SetPendingSdrAutoFriendlyFrameRateBucket", args => { PendingFrameRateBucket = (int?)args[0]; return null; });
            SetDelegate(context, "GetSelectedDevice", _ => SelectedDevice);
            SetDelegate(context, "RebuildSelectedDeviceCapabilities", _ =>
            {
                CapabilityRebuilds++;
                SelectedFormat = null;
                SelectedResolution = null;
                SelectedFrameRate = 0;
                return null;
            });
            // Failure and unselected-device completion stop before retargeting.
            SetDelegate(context, "CreateRetargetApplier", _ => RuntimeHelpers.GetUninitializedObject(
                RequireType("Sussudio.Controllers.MainViewModelDeviceFormatProbeRetargetApplier")));
            _controller = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelDeviceFormatProbeController"), context)!;
        }

        public object Device { get; } = CreateDevice("format-probe-device");
        public object CachedFormat { get; }
        public object? SelectedDevice { get; set; }
        public object? SelectedFormat { get; private set; }
        public string? SelectedResolution { get; private set; } = "3840x2160";
        public double SelectedFrameRate { get; private set; } = 60;
        public bool PendingAutoSelection { get; private set; } = true;
        public int? PendingFrameRateBucket { get; private set; } = 60;
        public int CapabilityRebuilds { get; private set; }

        public void Apply(object result)
            => _controller.GetType().GetMethod("OnDeviceFormatProbeCompleted")!.Invoke(_controller, [null, result]);
    }

    private static Type CaptureDeviceType => RequireType("Sussudio.Models.CaptureDevice");
    private static Type DiscoveryResultType => RequireType("Sussudio.Services.Capture.DeviceService+DeviceDiscoveryResult");
    private static Type RequireType(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static T Get<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);

    private static object CreateDevice(string id)
    {
        var device = Activator.CreateInstance(CaptureDeviceType)!;
        CaptureDeviceType.GetProperty("Id")!.SetValue(device, id);
        CaptureDeviceType.GetProperty("Name")!.SetValue(device, id);
        return device;
    }

    private static object CreateDiscovery(string? error = null, object[]? devices = null)
        => Activator.CreateInstance(DiscoveryResultType,
            NewList(typeof(ObservableCollection<>).MakeGenericType(CaptureDeviceType), devices ?? []),
            Array.CreateInstance(RequireType("Sussudio.Models.AudioInputDevice"), 0),
            error)!;

    private static IList NewList(Type listType, params object[] items)
    {
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items) list.Add(item);
        return list;
    }

    private sealed class BlockingNativeCall
    {
        private readonly TaskCompletionSource<(int ThreadId, ApartmentState Apartment)> _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<(int ThreadId, ApartmentState Apartment)> Entered => _entered.Task;

        public void Block()
        {
            _entered.TrySetResult((Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState()));
            _release.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        }

        public void Release() => _release.TrySetResult();
    }

    private static async Task<object> AssertWorkerHandoffAsync(BlockingNativeCall nativeCall, Func<Task<object>> invoke)
    {
        // Observe the immediate return separately from the operation's completion.
        // A Task.Run around the whole test invocation would hide caller-side blocking.
        var invocation = InvokeOnStaThreadAsync(invoke);
        try
        {
            var worker = await nativeCall.Entered.WaitAsync(WorkerTestTimeout);
            var caller = await invocation.WaitAsync(WorkerTestTimeout);
            Assert.Equal(ApartmentState.STA, caller.Apartment);
            Assert.Equal(ApartmentState.MTA, worker.Apartment);
            Assert.NotEqual(caller.ThreadId, worker.ThreadId);
            Assert.False(caller.Operation.IsCompleted);
            nativeCall.Release();
            return await caller.Operation.WaitAsync(WorkerTestTimeout);
        }
        finally
        {
            nativeCall.Release();
            var caller = await invocation.WaitAsync(WorkerTestTimeout);
            await caller.Operation.WaitAsync(WorkerTestTimeout);
        }
    }

    private static Task<(T Operation, int ThreadId, ApartmentState Apartment)> InvokeOnStaThreadAsync<T>(Func<T> invoke)
        where T : Task
    {
        var completion = new TaskCompletionSource<(T Operation, int ThreadId, ApartmentState Apartment)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var caller = new Thread(() =>
        {
            try
            {
                var operation = invoke();
                completion.SetResult((operation, Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState()));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }) { IsBackground = true };
        if (OperatingSystem.IsWindows()) caller.SetApartmentState(ApartmentState.STA);
        caller.Start();
        return completion.Task;
    }

    private static object CreateService(Func<bool, Type, object> enumerate)
    {
        var serviceType = RequireType("Sussudio.Services.Capture.DeviceService");
        var constructor = serviceType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(ctor => ctor.GetParameters().Length == 2);
        var parameters = constructor.GetParameters();
        return constructor.Invoke(parameters.Select((parameter, index) =>
            (object)MakeDelegate(parameter.ParameterType, _ => enumerate(index == 1,
                parameter.ParameterType.GetMethod("Invoke")!.ReturnType))).ToArray());
    }

    private static object CreateProbeService(Func<Type, object> probe)
        => CreateProbeService((_, taskType) => probe(taskType));

    private static object CreateProbeService(Func<string, Type, object> probe)
    {
        var serviceType = RequireType("Sussudio.Services.Capture.DeviceService");
        var constructor = serviceType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(ctor => ctor.GetParameters().Length == 3);
        return constructor.Invoke(constructor.GetParameters().Select((parameter, index) =>
            (object)MakeDelegate(parameter.ParameterType, args =>
            {
                var taskType = parameter.ParameterType.GetMethod("Invoke")!.ReturnType;
                if (index == 2) return probe((string)args[0]!, taskType);
                if (index == 1) return EmptyListTask(taskType);
                var listType = taskType.GetGenericArguments()[0];
                var videoDevice = Activator.CreateInstance(listType.GetGenericArguments()[0], "Test capture", "format-probe-device")!;
                return TypedTask(listType, Task.FromResult((object)NewList(listType, videoDevice)));
            })).ToArray());
    }

    private static Task<object> ProbeResultAsync(object service, bool background)
        => background
            ? BackgroundProbeAsync(service, CreateDevice("format-probe-device"), 41)
            : InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", true);

    private static async Task<object> BackgroundProbeAsync(object service, object device, long requestId)
    {
        var deviceId = Get<string>(device, "Id");
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probeEvent = service.GetType().GetEvent("FormatProbeCompleted")!;
        var handler = MakeDelegate(probeEvent.EventHandlerType!, args =>
        {
            var result = args[1]!;
            if (Get<string>(result, "DeviceId") == deviceId && Get<long>(result, "RequestId") == requestId)
            {
                completion.TrySetResult(result);
            }

            return null;
        });
        probeEvent.AddEventHandler(service, handler);
        try
        {
            service.GetType().GetMethod("BeginBackgroundFormatProbe")!.Invoke(service, [device, requestId]);
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            probeEvent.RemoveEventHandler(service, handler);
        }
    }

    private static object EmptyListTask(Type taskType)
    {
        var listType = taskType.GetGenericArguments()[0];
        return TypedTask(listType, Task.FromResult((object)NewList(listType)));
    }

    private static object FailedTask(Type taskType, Exception failure)
        => TypedTask(taskType.GetGenericArguments()[0], Task.FromException<object>(failure));

    private static object TypedTask(Type resultType, Task<object> task)
        => typeof(DeviceDiscoveryTests).GetMethod(nameof(ConvertTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType).Invoke(null, [task])!;

    private static async Task<T> ConvertTaskAsync<T>(Task<object> task) => (T)await task.ConfigureAwait(false);

    private static async Task<object> InvokeResultAsync(object instance, string method, params object[] arguments)
    {
        var task = (Task)instance.GetType().GetMethod(method)!.Invoke(instance, arguments)!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static void SetDelegate(object context, string propertyName, Func<object?[], object?> body)
    {
        var property = context.GetType().GetProperty(propertyName)!;
        property.SetValue(context, MakeDelegate(property.PropertyType, body));
    }

    private static Delegate MakeDelegate(Type delegateType, Func<object?[], object?> body)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
        var arguments = Expression.NewArrayInit(typeof(object), parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));
        var call = Expression.Invoke(Expression.Constant(body), arguments);
        Expression result = invoke.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Empty())
            : Expression.Convert(call, invoke.ReturnType);
        return Expression.Lambda(delegateType, result, parameters).Compile();
    }
}
