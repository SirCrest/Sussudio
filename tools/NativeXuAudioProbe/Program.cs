using System.Diagnostics;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;

var deviceNameFilter = args.Length > 0 ? args[0] : "4K X";
if (args.Length > 0 && string.Equals(args[0], "rtk-i2c", StringComparison.OrdinalIgnoreCase))
{
    var rtkArgs = args.Skip(1).ToArray();
    if (rtkArgs.Any(arg =>
        arg.StartsWith("--device=", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(arg, "--device", StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("rtk-i2c cannot accept a device filter because RTK_IO selects by name, not by native XU path.");
        Console.Error.WriteLine("Disconnect other supported devices so the locator can prove the selection is unambiguous.");
        return 1;
    }

    var dev = NativeXuProbeDeviceLocator.Find(null);
    if (dev == null)
    {
        return 1;
    }

    return RtkI2cProbe.Run(rtkArgs, dev);
}
if (args.Length > 0 && string.Equals(args[0], "service", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeServiceProbe.RunServiceControlProbeAsync(args.Skip(1).ToArray());
}
if (args.Length > 0 && string.Equals(args[0], "dump-s3", StringComparison.OrdinalIgnoreCase))
{
    // Dump XU selector 3 raw bytes to hex file for diffing
    var outPath = args.Length > 1 ? args[1] : "s3-dump.hex";
    var dev = NativeXuProbeDeviceLocator.Find("4K X");
    if (dev == null) { Console.Error.WriteLine("No device"); return 1; }
    Console.WriteLine($"Device: {dev.Name}");

    var service = new NativeXuAudioControlService();
    var snapshot = await service.ReadPayloadSnapshotAsync(dev, CancellationToken.None).ConfigureAwait(false);
    if (snapshot == null)
    {
        Console.Error.WriteLine("Failed to read selector 3");
        return 1;
    }

    var raw = snapshot.RawPayload;
    Console.WriteLine($"Selector 3: {raw.Length} bytes");
    var hex = BitConverter.ToString(raw).Replace("-", "");
    File.WriteAllText(outPath, hex);
    Console.WriteLine($"Written to {outPath}");
    // Also print first 256 bytes for quick view
    for (int i = 0; i < Math.Min(raw.Length, 256); i += 16)
    {
        var line = string.Join(" ", Enumerable.Range(i, Math.Min(16, raw.Length - i)).Select(j => raw[j].ToString("X2")));
        Console.WriteLine($"  {i:X4}: {line}");
    }
    return 0;
}
if (args.Length > 0 && string.Equals(args[0], "i2c-probe", StringComparison.OrdinalIgnoreCase))
{
    return NativeXuProbeI2cLegacyProbe.Run();
}

if (args.Length > 0 && string.Equals(args[0], "i2c-cmd", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeI2cCommands.RunAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "i2c-switch", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeI2cSwitch.RunAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "at-read", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeAtCommands.RunAtReadAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "at-write", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeAtCommands.RunAtWriteAsync(args);
}
if (args.Length > 0 && string.Equals(args[0], "at-set-input", StringComparison.OrdinalIgnoreCase))
{
    return await NativeXuProbeAtCommands.RunAtSetInputAsync(args);
}

var device = NativeXuProbeDeviceLocator.Find(deviceNameFilter);

if (device == null)
{
    Console.Error.WriteLine("No capture device found.");
    return 1;
}

Console.WriteLine($"Device: {device.Name}");
Console.WriteLine($"Id: {device.Id}");

if (args.Any(arg => string.Equals(arg, "--service-smoke", StringComparison.OrdinalIgnoreCase)))
{
    return await NativeXuProbeServiceProbe.RunServiceSmokeAsync(device);
}

return await NativeXuProbeDefaultExperiment.RunAsync(device);

// Probe-local runtime shims used by linked app service sources.
internal static class Logger
{
    public static void Log(string message)
        => Trace.TraceInformation(message);
}

public sealed class CaptureDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeXuInterfacePath { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Device" : Name;

    public override string ToString() => DisplayName;
}
