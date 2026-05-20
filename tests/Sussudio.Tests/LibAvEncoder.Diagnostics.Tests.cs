using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task LibAvEncoder_ThrowIfError_ThrowsOnNegative()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ThrowIfError",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ThrowIfError not found.");

        // Non-negative should not throw
        method.Invoke(null, new object[] { 0, "test" });
        method.Invoke(null, new object[] { 1, "test" });

        // Negative should throw (may throw InvalidOperationException or
        // DllNotFoundException if FFmpeg runtime isn't loaded for GetErrorString)
        var threw = false;
        try
        {
            method.Invoke(null, new object[] { -1, "test operation" });
        }
        catch (TargetInvocationException)
        {
            threw = true;
        }
        AssertEqual(true, threw, "ThrowIfError throws on negative error code");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_DiagnosticsHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Diagnostics.cs")
            .Replace("\r\n", "\n");

        AssertContains(diagnosticsText, "private void EnsureOpen()");
        AssertContains(diagnosticsText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(diagnosticsText, "private static string GetErrorString(int errorCode)");
        AssertContains(diagnosticsText, "private static InvalidOperationException CreateLibAvException(string message)");
        AssertContains(diagnosticsText, "private static void CheckDeviceRemoved(IntPtr d3d11Device)");
        AssertDoesNotContain(rootText, "private void EnsureOpen()");
        AssertDoesNotContain(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertDoesNotContain(rootText, "private static string GetErrorString(int errorCode)");
        AssertDoesNotContain(rootText, "private static InvalidOperationException CreateLibAvException(string message)");
        AssertDoesNotContain(rootText, "private static void CheckDeviceRemoved(IntPtr d3d11Device)");

        return Task.CompletedTask;
    }
}
