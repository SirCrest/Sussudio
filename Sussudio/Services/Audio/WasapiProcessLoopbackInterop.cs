using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Audio;

[ComImport]
[Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IActivateAudioInterfaceCompletionHandler
{
    [PreserveSig]
    int ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation);
}

[ComImport]
[Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IActivateAudioInterfaceAsyncOperation
{
    [PreserveSig]
    int GetActivateResult(
        out int activateResult,
        [MarshalAs(UnmanagedType.IUnknown)] out object? activatedInterface);
}

// Marker interface. ActivateAudioInterfaceAsync refuses a completion handler that is not agile, because it invokes the
// handler on its own worker thread.
[ComImport]
[Guid("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAgileObject
{
}

internal sealed class WasapiProcessLoopbackActivationException : Exception
{
    public WasapiProcessLoopbackActivationException(string stage, int hresult, string message)
        : base(message)
    {
        Stage = stage;
        HResult = hresult;
    }

    /// <summary>Which step failed: <c>activate</c> (the call), <c>activation</c> (the async result) or <c>timeout</c>.</summary>
    public string Stage { get; }
}

// Windows process loopback: activates an IAudioClient on the virtual device "VAD\Process_Loopback" that captures what a
// target process (and its child processes) renders, mixed before the endpoint. Windows 10 build 20348 and Windows 11.
// The activation parameters travel as a VT_BLOB PROPVARIANT; the completion handler must be agile. The returned client
// is initialized by the caller with an explicit format (GetMixFormat is not implemented on the virtual device).
internal static class WasapiProcessLoopbackInterop
{
    internal const string VirtualDeviceInterfacePath = "VAD\\Process_Loopback";

    // Documented minimum for AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK.
    internal const int MinimumBuildNumber = 20348;

    private const int AudioClientActivationTypeProcessLoopback = 1;
    private const int ProcessLoopbackModeIncludeTargetProcessTree = 0;
    private const ushort VtBlob = 65;

    // AUDIOCLIENT_ACTIVATION_PARAMS { type; PROCESS_LOOPBACK params { pid; mode } } is 12 bytes.
    [StructLayout(LayoutKind.Sequential)]
    private struct ActivationParams
    {
        public int ActivationType;
        public uint TargetProcessId;
        public int ProcessLoopbackMode;
    }

    // PROPVARIANT with a BLOB payload on x64: vt and three reserved words (8 bytes), BLOB { ULONG cbSize; BYTE* pBlobData }
    // aligned to 8 (cbSize at 8, pBlobData at 16): 24 bytes. Sussudio ships x64 only.
    [StructLayout(LayoutKind.Sequential)]
    private struct BlobPropVariant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public uint cbSize;
        public uint padding;
        public IntPtr pBlobData;
    }

    [DllImport("Mmdevapi.dll", ExactSpelling = true)]
    private static extern int ActivateAudioInterfaceAsync(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
        ref Guid riid,
        IntPtr activationParams,
        IActivateAudioInterfaceCompletionHandler completionHandler,
        out IActivateAudioInterfaceAsyncOperation activationOperation);

    internal static bool IsSupported =>
        IntPtr.Size == 8 && Environment.OSVersion.Version.Build >= MinimumBuildNumber;

    // Activates the loopback client for <processId> and its process tree. Blocks for the asynchronous activation (bounded
    // by <paramref name="timeout"/>). The caller owns the returned client.
    internal static IAudioClient Activate(int processId, TimeSpan timeout)
    {
        var parameters = Marshal.AllocHGlobal(Marshal.SizeOf<ActivationParams>());
        var variant = Marshal.AllocHGlobal(Marshal.SizeOf<BlobPropVariant>());
        try
        {
            Marshal.StructureToPtr(
                new ActivationParams
                {
                    ActivationType = AudioClientActivationTypeProcessLoopback,
                    TargetProcessId = (uint)processId,
                    ProcessLoopbackMode = ProcessLoopbackModeIncludeTargetProcessTree
                },
                parameters,
                fDeleteOld: false);
            Marshal.StructureToPtr(
                new BlobPropVariant
                {
                    vt = VtBlob,
                    cbSize = (uint)Marshal.SizeOf<ActivationParams>(),
                    pBlobData = parameters
                },
                variant,
                fDeleteOld: false);

            var handler = new ActivationCompletionHandler();
            var iid = typeof(IAudioClient).GUID;
            var hr = ActivateAudioInterfaceAsync(VirtualDeviceInterfacePath, ref iid, variant, handler, out var operation);
            if (hr < 0)
            {
                throw new WasapiProcessLoopbackActivationException(
                    "activate",
                    hr,
                    $"ActivateAudioInterfaceAsync failed with HRESULT 0x{hr:X8}.");
            }

            try
            {
                if (!handler.Completed.Wait(timeout))
                {
                    throw new WasapiProcessLoopbackActivationException(
                        "timeout",
                        unchecked((int)0x800705B4), // ERROR_TIMEOUT
                        $"Process loopback activation did not complete within {timeout.TotalMilliseconds:F0} ms.");
                }

                if (handler.Failure is { } failure)
                {
                    throw failure;
                }

                if (handler.ActivateResult < 0)
                {
                    throw new WasapiProcessLoopbackActivationException(
                        "activation",
                        handler.ActivateResult,
                        $"Process loopback activation failed with HRESULT 0x{handler.ActivateResult:X8}.");
                }

                if (handler.Activated is not IAudioClient client)
                {
                    throw new WasapiProcessLoopbackActivationException(
                        "activation",
                        unchecked((int)0x80004002), // E_NOINTERFACE
                        "Process loopback activation returned no IAudioClient.");
                }

                return client;
            }
            finally
            {
                WasapiComInterop.ReleaseComObjectSafe(operation);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(variant);
            Marshal.FreeHGlobal(parameters);
        }
    }

    // Agile completion handler: runs on a Windows worker thread, publishes the result and signals the waiting caller.
    private sealed class ActivationCompletionHandler : IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        public ManualResetEventSlim Completed { get; } = new(false);

        public int ActivateResult { get; private set; } = unchecked((int)0x80004005); // E_FAIL until reported

        public object? Activated { get; private set; }

        public Exception? Failure { get; private set; }

        public int ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            try
            {
                var hr = activateOperation.GetActivateResult(out var activateResult, out var activated);
                if (hr < 0)
                {
                    ActivateResult = hr;
                }
                else
                {
                    ActivateResult = activateResult;
                    Activated = activated;
                }
            }
            catch (Exception ex)
            {
                Failure = new WasapiProcessLoopbackActivationException(
                    "activation",
                    unchecked((int)0x80004005),
                    $"Reading the activation result threw {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                Completed.Set();
            }

            return 0;
        }
    }
}
