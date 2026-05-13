using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
#if DEBUG
    /// <summary>
    /// One-shot diagnostic: compares raw COM vtable dispatch with managed interface dispatch
    /// to detect vtable misalignment in the IMFSample COM interop definition.
    /// IMFSample inherits IMFAttributes (30 methods). If .NET miscalculates the derived
    /// method offsets, managed calls will hit wrong vtable slots.
    /// Expected vtable layout: IUnknown(3) + IMFAttributes(30) + IMFSample(14) = 47 slots.
    /// </summary>
    private unsafe void DiagnoseVtable(IMFSample sample)
    {
        try
        {
            // Raw vtable dispatch is the ground truth.
            var punk = Marshal.GetIUnknownForObject(sample);
            try
            {
                var iidSample = new Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4");
                var qiHr = Marshal.QueryInterface(punk, ref iidSample, out var pSample);
                Log($"VTABLE_DIAG QI_for_IMFSample hr=0x{qiHr:X8} pUnk=0x{punk:X16} pSample=0x{pSample:X16} same={punk == pSample}");

                if (qiHr < 0 || pSample == IntPtr.Zero)
                {
                    Log("VTABLE_DIAG QI FAILED — cannot diagnose vtable");
                    return;
                }

                try
                {
                    var vtable = *(IntPtr*)pSample;

                    // GetSampleTime = slot 35 (3 IUnknown + 30 IMFAttributes + 2 IMFSample)
                    // HRESULT GetSampleTime(IMFSample* this, LONGLONG* phnsSampleTime)
                    {
                        var fn = *(IntPtr*)((byte*)vtable + 35 * sizeof(IntPtr));
                        long time = -1;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, long*, int>)fn)(pSample, &time);
                        Log($"VTABLE_DIAG RAW slot35_GetSampleTime hr=0x{hr:X8} time={time}");
                    }

                    // GetBufferCount = slot 39
                    // HRESULT GetBufferCount(IMFSample* this, DWORD* pdwBufferCount)
                    {
                        var fn = *(IntPtr*)((byte*)vtable + 39 * sizeof(IntPtr));
                        int count = -1;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)fn)(pSample, &count);
                        Log($"VTABLE_DIAG RAW slot39_GetBufferCount hr=0x{hr:X8} count={count}");
                    }

                    // ConvertToContiguousBuffer = slot 41
                    // HRESULT ConvertToContiguousBuffer(IMFSample* this, IMFMediaBuffer** ppBuffer)
                    {
                        var fn = *(IntPtr*)((byte*)vtable + 41 * sizeof(IntPtr));
                        IntPtr buf = IntPtr.Zero;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)fn)(pSample, &buf);
                        Log($"VTABLE_DIAG RAW slot41_ConvertToContiguousBuffer hr=0x{hr:X8} buffer=0x{buf:X16}");
                        if (buf != IntPtr.Zero)
                        {
                            // Probe the buffer: Lock it to see actual frame data
                            // IMFMediaBuffer::Lock = slot 3 (IUnknown + first method)
                            var bufVtable = *(IntPtr*)buf;
                            var lockFn = *(IntPtr*)((byte*)bufVtable + 3 * sizeof(IntPtr));
                            IntPtr dataPtr = IntPtr.Zero;
                            int maxLen = 0, curLen = 0;
                            var lockHr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, int*, int>)lockFn)(
                                buf, &dataPtr, &maxLen, &curLen);
                            Log($"VTABLE_DIAG RAW buffer_Lock hr=0x{lockHr:X8} data=0x{dataPtr:X16} maxLen={maxLen} curLen={curLen}");

                            if (lockHr >= 0)
                            {
                                // Unlock: slot 4
                                var unlockFn = *(IntPtr*)((byte*)bufVtable + 4 * sizeof(IntPtr));
                                ((delegate* unmanaged[Stdcall]<IntPtr, int>)unlockFn)(buf);
                            }

                            Marshal.Release(buf);
                        }
                    }

                    // Managed interface dispatch is what .NET thinks the slots are.
                    {
                        var hr = sample.GetSampleTime(out var time);
                        Log($"VTABLE_DIAG MANAGED GetSampleTime hr=0x{hr:X8} time={time}");
                    }
                    {
                        var hr = sample.GetBufferCount(out var count);
                        Log($"VTABLE_DIAG MANAGED GetBufferCount hr=0x{hr:X8} count={count}");
                    }
                    {
                        var hr = sample.ConvertToContiguousBuffer(out var buf);
                        Log($"VTABLE_DIAG MANAGED ConvertToContiguousBuffer hr=0x{hr:X8} buffer={(buf != null ? "non-null" : "null")}");
                        if (buf != null)
                        {
                            Marshal.ReleaseComObject(buf);
                        }
                    }
                }
                finally
                {
                    Marshal.Release(pSample);
                }
            }
            finally
            {
                Marshal.Release(punk);
            }
        }
        catch (Exception ex)
        {
            Log($"VTABLE_DIAG EXCEPTION type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }
#endif
}
