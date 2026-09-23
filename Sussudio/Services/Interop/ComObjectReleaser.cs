using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Interop;

internal static class ComObjectReleaser
{
    internal static void ReleaseComObject<T>(ref T? comObject, string failureContext)
        where T : class
    {
        if (comObject == null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Suppressed exception in {failureContext}: {ex.Message}");
        }
        finally
        {
            comObject = null;
        }
    }

    internal static void ReleaseComObjectSafe(object? obj, string failureContext)
    {
        if (obj == null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(obj))
            {
                Marshal.ReleaseComObject(obj);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Suppressed exception in {failureContext}: {ex.Message}");
        }
    }
}
