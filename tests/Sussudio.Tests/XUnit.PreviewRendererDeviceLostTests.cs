using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Sussudio.Tests;

// Device removal cannot be triggered on demand on the live rig, so these tests inject each HRESULT through a native
// COM object whose vtable they control and drive the renderer's real Present, ResizeBuffers and shared-device
// adoption code. Failure modes covered:
//   1-4. Present returns DEVICE_REMOVED / DEVICE_RESET / DEVICE_HUNG / DRIVER_INTERNAL_ERROR -> routed to recovery.
//   5.   Present returns a non-loss failure (INVALID_CALL, E_INVALIDARG, E_FAIL) -> not recovery (per-frame failure).
//   8.   ResizeBuffers returns a loss code -> recovered in place, render thread survives; other codes still escape.
//   9.   The capture-shared device is removed -> recovery falls back to a renderer-owned device instead of
//        re-adopting the dead one (which would end the render thread); a healthy shared device is still adopted.
public sealed class PreviewRendererDeviceLostTests
{
    private const int DeviceRemoved = unchecked((int)0x887A0005);
    private const int DeviceHung = unchecked((int)0x887A0006);
    private const int DeviceReset = unchecked((int)0x887A0007);
    private const int DriverInternalError = unchecked((int)0x887A0020);
    private const int InvalidCall = unchecked((int)0x887A0001);
    private const int InvalidArg = unchecked((int)0x80070057);
    private const int Fail = unchecked((int)0x80004005);

    public static TheoryData<int, bool> PresentResults => new()
    {
        { DeviceRemoved, true },
        { DeviceReset, true },
        { DeviceHung, true },
        { DriverInternalError, true },
        { InvalidCall, false },
        { InvalidArg, false },
        { Fail, false },
    };

    [Theory]
    [MemberData(nameof(PresentResults))]
    public void PresentFailureKeepsItsHResultAndRoutesDeviceLossToRecovery(int hresult, bool expectRecovery)
    {
        var fixture = new RendererFixture();
        var swapChain = InjectedComObject.Create(hresult);
        fixture.SetWrapper("_swapChain", swapChain);

        var failure = Assert.ThrowsAny<Exception>(() => fixture.Invoke(
            "PresentAndTrackFrame", null, "test", "first frame", 0L, 0L, 0L, 0L));

        Assert.Equal(hresult, ResultCodeOf(failure));
        Assert.Equal(expectRecovery, fixture.IsDeviceLost(failure));
        Assert.Equal(0L, fixture.Get<long>("_framesRendered"));
    }

    [Theory]
    [MemberData(nameof(PresentResults))]
    public void ResizeBuffersDeviceLossRecoversWithoutEndingTheRenderThread(int hresult, bool expectRecovery)
    {
        var fixture = new RendererFixture();
        var native = InjectedComObject.Create(hresult);
        fixture.SetWrapper("_swapChain", native);
        fixture.SetWrapper("_device", native);
        fixture.SetWrapper("_videoDevice", native);
        fixture.Set("_requestedOutputWidth", 1920);
        fixture.Set("_requestedOutputHeight", 1080);
        // Stop is pending, so recovery returns after its log line instead of rebuilding on the UI thread.
        fixture.Set("_stopRequested", 1);

        if (expectRecovery)
        {
            fixture.Invoke("TryResizeOutputForPendingFrame");
            Assert.Equal(0L, fixture.Get<long>("_outputResizeCount"));
        }
        else
        {
            var failure = Assert.ThrowsAny<Exception>(() => fixture.Invoke("TryResizeOutputForPendingFrame"));
            Assert.Equal(hresult, ResultCodeOf(failure));
        }
    }

    [Theory]
    [InlineData(DeviceRemoved)]
    [InlineData(DeviceReset)]
    [InlineData(DeviceHung)]
    [InlineData(DriverInternalError)]
    public void RemovedSharedDeviceIsNotReadoptedDuringRecovery(int removedReason)
    {
        var fixture = new RendererFixture();
        fixture.SetWrapper("_sharedDevice", InjectedComObject.Create(removedReason));

        var arguments = new object?[] { null };
        var adopted = (bool)fixture.Invoke("TryInitializeWithSharedDevice", arguments)!;

        Assert.False(adopted);
        Assert.Null(fixture.Get<object?>("_device"));
        Assert.Null(fixture.Get<object?>("_deviceContext"));
    }

    [Fact]
    public void HealthySharedDeviceIsStillAdopted()
    {
        var fixture = new RendererFixture();
        fixture.SetWrapper("_sharedDevice", InjectedComObject.Create(0));

        var arguments = new object?[] { null };
        var adopted = (bool)fixture.Invoke("TryInitializeWithSharedDevice", arguments)!;

        Assert.True(adopted);
        Assert.NotNull(fixture.Get<object?>("_device"));
        Assert.NotNull(fixture.Get<object?>("_deviceContext"));
        fixture.DisposeAndClear("_deviceContext");
        fixture.DisposeAndClear("_device");
    }

    private static int ResultCodeOf(Exception failure)
    {
        var resultCode = failure.GetType().GetProperty("ResultCode")
            ?? throw new InvalidOperationException($"{failure.GetType().Name} does not carry a native result: {failure.Message}");
        var result = resultCode.GetValue(failure)!;
        return (int)result.GetType().GetProperty("Code")!.GetValue(result)!;
    }

    // Holds an uninitialized renderer: no panel, no dispatcher, no real device. Only the fields the exercised paths
    // read are populated.
    private sealed class RendererFixture
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Type _rendererType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Preview.D3D11PreviewRenderer", throwOnError: true)!;
        private readonly object _renderer;

        public RendererFixture()
        {
            _renderer = RuntimeHelpers.GetUninitializedObject(_rendererType);
            Set("_lifecycleLock", new object());
            Set("_configuredOutputWidth", 1);
            Set("_configuredOutputHeight", 1);
            Set("_requestedOutputWidth", 1);
            Set("_requestedOutputHeight", 1);
        }

        public void SetWrapper(string name, IntPtr nativePointer)
            => Set(name, Activator.CreateInstance(Field(name).FieldType, nativePointer));

        public void DisposeAndClear(string name)
        {
            ((IDisposable)Get<object>(name)).Dispose();
            Set(name, null);
        }

        public bool IsDeviceLost(Exception failure)
            => (bool)(_rendererType.GetMethod("IsDeviceLostException", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(_rendererType.FullName, "IsDeviceLostException"))
                .Invoke(null, BindingFlags.DoNotWrapExceptions, null, new object[] { failure }, null)!;

        public void Set(string name, object? value) => Field(name).SetValue(_renderer, value);

        public T Get<T>(string name) => (T)Field(name).GetValue(_renderer)!;

        public object? Invoke(string name, params object?[] values)
            => (_rendererType.GetMethod(name, InstanceFlags)
                ?? throw new MissingMethodException(_rendererType.FullName, name))
                .Invoke(_renderer, BindingFlags.DoNotWrapExceptions, null, values, null);

        private FieldInfo Field(string name)
            => _rendererType.GetField(name, InstanceFlags) ?? throw new MissingFieldException(_rendererType.FullName, name);
    }

    // A native COM object for HRESULT injection. IUnknown is inert (no lifetime), ID3D11Device::GetImmediateContext
    // hands back the object itself, and every other slot returns the injected HRESULT: x64 has a single calling
    // convention with caller-cleaned arguments, so one stub can answer slots with different parameter lists
    // (Present, ResizeBuffers, GetDeviceRemovedReason, ...). The memory is never freed: an undisposed Vortice wrapper's
    // finalizer may still call Release through it after the test ends.
    private static unsafe class InjectedComObject
    {
        private const int SlotCount = 64;
        private const int D3D11DeviceGetImmediateContextSlot = 40;
        private static readonly IntPtr SharedVtable = CreateVtable();

        public static IntPtr Create(int hresult)
        {
            var instance = (IntPtr*)Marshal.AllocHGlobal(2 * IntPtr.Size);
            instance[0] = SharedVtable;
            instance[1] = hresult;
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
                slots[slot] = (IntPtr)(delegate* unmanaged<IntPtr, int>)&ReturnInjected;
            }

            slots[D3D11DeviceGetImmediateContextSlot] = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr*, void>)&GetImmediateContext;
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
        private static int ReturnInjected(IntPtr self) => (int)((IntPtr*)self)[1];

        [UnmanagedCallersOnly]
        private static void GetImmediateContext(IntPtr self, IntPtr* context) => *context = self;
    }
}
