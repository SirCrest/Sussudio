using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Sussudio.Tests;

// Every D3D preview start creates a fresh renderer and starts its render thread before the capture service hands
// over its D3D11 device (SetPreviewFrameSink -> SetSharedDevice), so the render thread's device decision races that
// hand-over. These tests run the real device selection on the test thread (standing in for the render thread) and
// use the renderer's fault-injection point to land a hand-over from another thread inside the race window.
// Failure modes covered:
//   1.  Hand-over after the decision saw no device, before the renderer-owned device exists -> a reset is queued and
//       re-selects onto the handed-over device. (Missed, the renderer stayed on its own device with texture
//       submission inactive: every SubmitTexture threw and preview start timed out.)
//   2.  Hand-over of a different device after an older one was adopted -> submission stays inactive until the reset
//       re-selects onto the newer device.
//   3.  Hand-over before the decision -> adopted directly, no reset (no startup teardown).
//   4.  The adopted device handed over again once active -> no reset, still active.
//   6.  A handed-over device that cannot be adopted -> renderer-owned device and no reset: the reset would fail the
//       same way and repeat.
//   8.  The hand-over never waits on the render thread's device creation.
//   10. Two hand-overs inside one selection -> one reset, onto the latest device.
//   11. A reset queued during selection wakes the render loop.
// Real D3D11 devices: WARP stands in for the capture device, the renderer creates its own as in the app.
public sealed class PreviewRendererSharedDeviceRaceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void HandOverWhileTheRendererCreatesItsOwnDeviceQueuesAResetOntoIt(int handOvers)
    {
        using var fixture = new RendererFixture();
        var captureDevices = Enumerable.Range(0, handOvers).Select(_ => fixture.CreateCaptureDevice()).ToArray();
        fixture.HandOverDuringNextSelection(captureDevices);

        fixture.SelectRenderDevice();

        Assert.False(fixture.RendersOn(captureDevices[^1]));
        Assert.Equal(0, fixture.SharedDeviceActive);
        Assert.Equal(1, fixture.ResetPending);
        Assert.True(fixture.FrameReady.IsSet);

        fixture.RunQueuedReset();

        Assert.True(fixture.RendersOn(captureDevices[^1]));
        Assert.Equal(1, fixture.SharedDeviceActive);
        Assert.Equal(0, fixture.ResetPending);
    }

    [Fact]
    public void HandOverAfterAdoptingAnOlderDeviceKeepsSubmissionInactiveUntilTheReset()
    {
        using var fixture = new RendererFixture();
        var olderDevice = fixture.CreateCaptureDevice();
        var newerDevice = fixture.CreateCaptureDevice();
        fixture.HandOver(olderDevice);
        fixture.HandOverDuringNextSelection(newerDevice);

        fixture.SelectRenderDevice();

        Assert.True(fixture.RendersOn(olderDevice));
        Assert.Equal(0, fixture.SharedDeviceActive);
        Assert.Equal(1, fixture.ResetPending);

        fixture.RunQueuedReset();

        Assert.True(fixture.RendersOn(newerDevice));
        Assert.Equal(1, fixture.SharedDeviceActive);
        Assert.Equal(0, fixture.ResetPending);
    }

    [Fact]
    public void HandOverBeforeTheDecisionIsAdoptedWithoutAReset()
    {
        using var fixture = new RendererFixture();
        var captureDevice = fixture.CreateCaptureDevice();
        fixture.HandOver(captureDevice);

        fixture.SelectRenderDevice();

        Assert.True(fixture.RendersOn(captureDevice));
        Assert.Equal(1, fixture.SharedDeviceActive);
        Assert.Equal(0, fixture.ResetPending);
        Assert.False(fixture.FrameReady.IsSet);
    }

    [Fact]
    public void HandingOverTheActiveDeviceAgainKeepsItActiveWithoutAReset()
    {
        using var fixture = new RendererFixture();
        var captureDevice = fixture.CreateCaptureDevice();
        fixture.HandOver(captureDevice);
        fixture.SelectRenderDevice();

        fixture.HandOver(captureDevice);

        Assert.True(fixture.RendersOn(captureDevice));
        Assert.Equal(1, fixture.SharedDeviceActive);
        Assert.Equal(0, fixture.ResetPending);
        Assert.False(fixture.FrameReady.IsSet);
    }

    [Fact]
    public void HandedOverDeviceThatCannotBeAdoptedFallsBackWithoutAReset()
    {
        using var fixture = new RendererFixture();
        var unusableDevice = DeviceWithoutImmediateContext.Create();
        fixture.HandOver(unusableDevice);

        fixture.SelectRenderDevice();

        Assert.False(fixture.RendersOn(unusableDevice));
        Assert.True(fixture.HasRenderDevice);
        Assert.Equal(0, fixture.SharedDeviceActive);
        Assert.Equal(0, fixture.ResetPending);
    }

    // An uninitialized renderer with only the state device selection and hand-over touch: no panel, dispatcher, or
    // swap chain. The test thread plays the render thread, so _isRendering is set as RenderThreadMain sets it.
    private sealed class RendererFixture : IDisposable
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly TimeSpan HandOverTimeout = TimeSpan.FromSeconds(5);
        private readonly Type _rendererType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Preview.D3D11PreviewRenderer", throwOnError: true)!;
        private readonly object _renderer;
        private readonly List<IntPtr> _captureDevices = new();

        public RendererFixture()
        {
            _renderer = RuntimeHelpers.GetUninitializedObject(_rendererType);
            Set("_lifecycleLock", new object());
            Set("_frameReadyEvent", FrameReady);
            Set("_isRendering", 1);
        }

        public ManualResetEventSlim FrameReady { get; } = new(false);
        public int SharedDeviceActive => Get<int>("_sharedDeviceActive");
        public int ResetPending => Get<int>("_sharedDeviceResetPending");
        public bool HasRenderDevice => DevicePointer("_device") != IntPtr.Zero;

        public bool RendersOn(IntPtr device) => DevicePointer("_device") == device;

        public IntPtr CreateCaptureDevice()
        {
            const int WarpDriver = 5;
            const uint BgraSupport = 0x20;
            const uint SdkVersion = 7;
            var result = D3D11CreateDevice(
                IntPtr.Zero, WarpDriver, IntPtr.Zero, BgraSupport, IntPtr.Zero, 0, SdkVersion,
                out var device, out _, out var context);
            Assert.True(result >= 0, $"D3D11CreateDevice(WARP) failed: 0x{result:X8}.");
            Marshal.Release(context);
            _captureDevices.Add(device);
            return device;
        }

        // Mirrors CaptureService.TryApplySharedPreviewDevice: a fresh reference per hand-over, released afterwards.
        public void HandOver(IntPtr device)
        {
            Marshal.AddRef(device);
            using var reference = (IDisposable)Activator.CreateInstance(DeviceType, device)!;
            Invoke("SetSharedDevice", reference);
        }

        public void HandOverDuringNextSelection(params IntPtr[] devices)
        {
            var fired = 0;
            Action handOverFromCaptureThread = () =>
            {
                if (Interlocked.Exchange(ref fired, 1) != 0)
                {
                    return;
                }

                ExceptionDispatchInfo? failure = null;
                var captureThread = new Thread(() =>
                {
                    try
                    {
                        foreach (var device in devices)
                        {
                            HandOver(device);
                        }
                    }
                    catch (Exception ex)
                    {
                        failure = ExceptionDispatchInfo.Capture(ex);
                    }
                }) { IsBackground = true, Name = "capture device hand-over" };
                captureThread.Start();
                Assert.True(captureThread.Join(HandOverTimeout), "The hand-over waited on the render thread's device selection.");
                failure?.Throw();
            };
            Set("_afterSharedDeviceDecision", handOverFromCaptureThread);
        }

        public void SelectRenderDevice() => Invoke("SelectRenderDevice");

        // What the render loop does with a queued reset, minus the swap chain: consume the flag, release the device
        // (as CleanupD3DResources does), and select again.
        public void RunQueuedReset()
        {
            Assert.Equal(1, ResetPending);
            Set("_sharedDeviceResetPending", 0);
            FrameReady.Reset();
            ReleaseRenderDevice();
            SelectRenderDevice();
        }

        public void Dispose()
        {
            ReleaseRenderDevice();
            DisposeField("_sharedDevice");
            foreach (var device in _captureDevices)
            {
                Marshal.Release(device);
            }

            FrameReady.Dispose();
        }

        private Type DeviceType => Field("_sharedDevice").FieldType;

        private void ReleaseRenderDevice()
        {
            DisposeField("_deviceContext");
            DisposeField("_device");
            Set("_sharedDeviceActive", 0);
        }

        private IntPtr DevicePointer(string name)
            => Get<object?>(name) is { } wrapper
                ? (IntPtr)wrapper.GetType().GetProperty("NativePointer")!.GetValue(wrapper)!
                : IntPtr.Zero;

        private void DisposeField(string name)
        {
            (Get<object?>(name) as IDisposable)?.Dispose();
            Set(name, null);
        }

        private void Set(string name, object? value) => Field(name).SetValue(_renderer, value);

        private T Get<T>(string name) => (T)Field(name).GetValue(_renderer)!;

        private object? Invoke(string name, params object?[] values)
            => (_rendererType.GetMethod(name, InstanceFlags)
                ?? throw new MissingMethodException(_rendererType.FullName, name))
                .Invoke(_renderer, BindingFlags.DoNotWrapExceptions, null, values, null);

        private FieldInfo Field(string name)
            => _rendererType.GetField(name, InstanceFlags) ?? throw new MissingFieldException(_rendererType.FullName, name);
    }

    // A native object the renderer cannot adopt: ID3D11Device::GetImmediateContext hands back no context, every other
    // slot fails with E_NOTIMPL, and IUnknown has no lifetime. x64 has one caller-cleaned calling convention, so one
    // stub answers slots with different parameter lists. The memory is never freed: an undisposed Vortice wrapper's
    // finalizer may still call Release through it after the test ends.
    private static unsafe class DeviceWithoutImmediateContext
    {
        private const int SlotCount = 64;
        private const int GetImmediateContextSlot = 40;
        private static readonly IntPtr SharedVtable = CreateVtable();

        public static IntPtr Create()
        {
            var instance = (IntPtr*)Marshal.AllocHGlobal(IntPtr.Size);
            instance[0] = SharedVtable;
            return (IntPtr)instance;
        }

        private static IntPtr CreateVtable()
        {
            var slots = (IntPtr*)Marshal.AllocHGlobal(SlotCount * IntPtr.Size);
            slots[0] = (IntPtr)(delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)&QueryInterface;
            slots[1] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&InertReference;
            slots[2] = (IntPtr)(delegate* unmanaged<IntPtr, uint>)&InertReference;
            for (var slot = 3; slot < SlotCount; slot++)
            {
                slots[slot] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&NotImplemented;
            }

            slots[GetImmediateContextSlot] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr*, void>)&GetImmediateContext;
            return (IntPtr)slots;
        }

        [UnmanagedCallersOnly]
        private static int QueryInterface(IntPtr self, Guid* interfaceId, IntPtr* result)
        {
            *result = IntPtr.Zero;
            return unchecked((int)0x80004002); // E_NOINTERFACE
        }

        [UnmanagedCallersOnly]
        private static uint InertReference(IntPtr self) => 1;

        [UnmanagedCallersOnly]
        private static int NotImplemented(IntPtr self) => unchecked((int)0x80004001); // E_NOTIMPL

        [UnmanagedCallersOnly]
        private static void GetImmediateContext(IntPtr self, IntPtr* context) => *context = IntPtr.Zero;
    }

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int D3D11CreateDevice(
        IntPtr adapter,
        int driverType,
        IntPtr software,
        uint flags,
        IntPtr featureLevels,
        uint featureLevelCount,
        uint sdkVersion,
        out IntPtr device,
        out int featureLevel,
        out IntPtr immediateContext);
}
