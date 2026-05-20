using System.Reflection;
using System.Threading.Tasks;

// Tests for Realtek/I2C audio-control payload decisions.
static partial class Program
{
    private static readonly object RtkI2cProbeConsoleLock = new();

    internal static Task RtkI2cProbe_GuardsUnsafeNativePaths()
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine(
            "tools",
            "NativeXuAudioProbe",
            "bin",
            "Debug",
            "net8.0-windows10.0.19041.0",
            "win-x64",
            "NativeXuAudioProbe.dll"));
        var probeType = assembly.GetType("RtkI2cProbe")
            ?? throw new InvalidOperationException("RtkI2cProbe type not found.");
        var run = probeType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.Run method not found.");
        var getRtkDeviceName = probeType.GetMethod("GetRtkDeviceName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RtkI2cProbe.GetRtkDeviceName method not found.");
        var rtkProbeSource = ReadRepoFile("tools/NativeXuAudioProbe/RtkI2cProbe.cs");

        var missingPathDevice = CreateNativeXuProbeDevice(assembly, "capture-1", "Elgato 4K X (PID 0x0070)", null);
        var missingPath = CaptureConsole(() => InvokeRtkRun(run, [], missingPathDevice));
        AssertEqual(1, missingPath.ExitCode, "RtkI2cProbe missing native XU path exit code");
        AssertContains(rtkProbeSource, "requires a selected native XU interface path");

        var selectedPathDevice = CreateNativeXuProbeDevice(assembly, "capture-2", "Elgato 4K X (PID 0x0070)", @"\\?\hid#vid_0fd9&pid_0070#xu");
        var disabledSwitch = CaptureConsole(() => InvokeRtkRun(run, ["switch", "analog"], selectedPathDevice));
        AssertEqual(1, disabledSwitch.ExitCode, "RtkI2cProbe disabled switch exit code");
        AssertContains(rtkProbeSource, "RTK I2C switch is disabled");
        AssertContains(rtkProbeSource, "Use the native XU service/probe path");

        var trimmedName = getRtkDeviceName.Invoke(null, [selectedPathDevice]) as string;
        AssertEqual("Elgato 4K X", trimmedName, "RtkI2cProbe strips PID suffix for RTK device name");
        var defaultNameDevice = CreateNativeXuProbeDevice(assembly, "capture-3", string.Empty, @"\\?\hid#vid_0fd9&pid_0070#xu");
        var defaultName = getRtkDeviceName.Invoke(null, [defaultNameDevice]) as string;
        AssertEqual("Elgato 4K X", defaultName, "RtkI2cProbe default RTK device name");

        return Task.CompletedTask;
    }

    private static object CreateNativeXuProbeDevice(
        Assembly assembly,
        string id,
        string name,
        string? nativeXuInterfacePath)
    {
        var deviceType = assembly.GetType("CaptureDevice")
            ?? throw new InvalidOperationException("NativeXuAudioProbe CaptureDevice type not found.");
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("Failed to create NativeXuAudioProbe CaptureDevice.");
        deviceType.GetProperty("Id")?.SetValue(device, id);
        deviceType.GetProperty("Name")?.SetValue(device, name);
        deviceType.GetProperty("NativeXuInterfacePath")?.SetValue(device, nativeXuInterfacePath);
        return device;
    }

    private static int InvokeRtkRun(MethodInfo run, string[] args, object device)
    {
        try
        {
            return (int)(run.Invoke(null, [args, device])
                         ?? throw new InvalidOperationException("RtkI2cProbe.Run returned null."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static (int ExitCode, string Output, string Error) CaptureConsole(Func<int> action)
    {
        lock (RtkI2cProbeConsoleLock)
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var output = new StringWriter();
            using var error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);
                var exitCode = action();
                return (exitCode, output.ToString(), error.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
