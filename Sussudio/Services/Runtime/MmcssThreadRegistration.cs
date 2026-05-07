using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Runtime;

// Best-effort MMCSS registration wrapper for timing-sensitive worker threads.
// Failure is logged but not fatal so the app still runs on systems without AVRT.
internal sealed class MmcssThreadRegistration : IDisposable
{
    private IntPtr _handle;

    private MmcssThreadRegistration(IntPtr handle)
    {
        _handle = handle;
    }

    public static MmcssThreadRegistration? TryRegister(string taskName, int priority, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            return null;
        }

        try
        {
            var handle = AvSetMmThreadCharacteristics(taskName, out _);
            if (handle == IntPtr.Zero)
            {
                log?.Invoke($"MMCSS registration failed task={taskName} lastError={Marshal.GetLastWin32Error()}");
                return null;
            }

            var clampedPriority = (AvrtPriority)Math.Clamp(priority, -2, 2);
            if (!AvSetMmThreadPriority(handle, clampedPriority))
            {
                log?.Invoke($"MMCSS priority set failed task={taskName} priority={priority} lastError={Marshal.GetLastWin32Error()}");
            }

            log?.Invoke($"MMCSS registered task={taskName} priority={(int)clampedPriority}");
            return new MmcssThreadRegistration(handle);
        }
        catch (DllNotFoundException)
        {
            log?.Invoke("MMCSS registration unavailable: avrt.dll not found.");
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            log?.Invoke("MMCSS registration unavailable: AVRT entry point not found.");
            return null;
        }
        catch (Exception ex)
        {
            log?.Invoke($"MMCSS registration failed type={ex.GetType().Name} msg={ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        var handle = _handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _handle = IntPtr.Zero;
        AvRevertMmThreadCharacteristics(handle);
    }

    private enum AvrtPriority
    {
        VeryLow = -2,
        Low = -1,
        Normal = 0,
        High = 1,
        Critical = 2
    }

    [DllImport("avrt.dll", EntryPoint = "AvSetMmThreadCharacteristicsW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr AvSetMmThreadCharacteristics(string taskName, out int taskIndex);

    [DllImport("avrt.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AvSetMmThreadPriority(IntPtr avrtHandle, AvrtPriority priority);

    [DllImport("avrt.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AvRevertMmThreadCharacteristics(IntPtr avrtHandle);
}
