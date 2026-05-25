using System.Diagnostics;
using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Sussudio.Services.Telemetry;

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

// CLI-only device locator for NativeXuAudioProbe. It finds supported Elgato
// KS/XU interfaces and turns the selected interface path into a lightweight
// CaptureDevice model for the shared audio-control services.
internal static class NativeXuProbeDeviceLocator
{
    private const ushort ElgatoVendorId = 0x0FD9;
    private const string PreferredInterfaceFragment = "{65e8773d-8f56-11d0-a3b9-00a0c9223196}";

    private static readonly (ushort ProductId, string Name)[] SupportedDevices =
    {
        (0x009B, "Elgato 4K X"),
        (0x009C, "Elgato 4K X Revision"),
        (0x009D, "Elgato 4K X Audio Mode")
    };

    public static CaptureDevice? Find(string? nameFilter)
    {
        var candidates = EnumerateCandidates().ToArray();
        if (candidates.Length == 0)
        {
            Console.Error.WriteLine("No supported Elgato native XU interface was found.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(nameFilter))
        {
            if (candidates.Length == 1)
            {
                return candidates[0].Device;
            }

            WriteAmbiguousSelection(
                "Multiple supported Elgato native XU devices were found. Pass a device filter.",
                candidates);
            return null;
        }

        var matches = candidates
            .Where(candidate => IsMatch(candidate, nameFilter))
            .ToArray();

        if (matches.Length == 1)
        {
            return matches[0].Device;
        }

        if (matches.Length == 0)
        {
            Console.Error.WriteLine($"No supported Elgato native XU device matched filter '{nameFilter}'.");
            WriteAvailableCandidates(candidates);
            return null;
        }

        if (matches.Length > 1)
        {
            WriteAmbiguousSelection(
                $"Device filter '{nameFilter}' matched multiple supported Elgato native XU devices.",
                matches);
        }

        return null;
    }

    private static IEnumerable<NativeXuProbeCandidate> EnumerateCandidates()
    {
        foreach (var (productId, name) in SupportedDevices)
        {
            var groups = KsExtensionUnitNative
                .EnumerateKsInterfaces(ElgatoVendorId, productId)
                .Select(candidate => candidate.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .GroupBy(GetDeviceInstanceKey, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var interfacePath = group
                    .OrderByDescending(path =>
                        path.IndexOf(PreferredInterfaceFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .First();

                yield return new NativeXuProbeCandidate(
                    new CaptureDevice
                    {
                        Id = interfacePath,
                        NativeXuInterfacePath = interfacePath,
                        Name = $"{name} (PID 0x{productId:X4})"
                    },
                    productId,
                    GetDeviceInstanceKey(interfacePath));
            }
        }
    }

    private static string GetDeviceInstanceKey(string interfacePath)
    {
        var categoryStart = interfacePath.LastIndexOf("#{", StringComparison.Ordinal);
        return categoryStart > 0
            ? interfacePath[..categoryStart]
            : interfacePath;
    }

    private static bool IsMatch(NativeXuProbeCandidate candidate, string nameFilter)
    {
        var filter = nameFilter.Trim();
        var normalizedProductId = filter.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? filter[2..]
            : filter;

        return candidate.Device.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               candidate.Device.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               (candidate.Device.NativeXuInterfacePath?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
               candidate.DeviceInstanceKey.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               candidate.ProductId.ToString("X4").Contains(normalizedProductId, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteAmbiguousSelection(string message, IReadOnlyCollection<NativeXuProbeCandidate> candidates)
    {
        Console.Error.WriteLine(message);
        WriteAvailableCandidates(candidates);
    }

    private static void WriteAvailableCandidates(IEnumerable<NativeXuProbeCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            Console.Error.WriteLine(
                $"  {candidate.Device.Name} id='{candidate.Device.Id}' instance='{candidate.DeviceInstanceKey}'");
        }
    }

    // Candidate pairing of a display device and the selected interface metadata.
    private sealed record NativeXuProbeCandidate(
        CaptureDevice Device,
        ushort ProductId,
        string DeviceInstanceKey);
}

static class NativeXuProbeAtCommands
{
    public static async Task<int> RunAtReadAsync(string[] args)
    {
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");

        for (var ai = 1; ai < args.Length; ai++)
        {
            if (!int.TryParse(args[ai].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out var opcode))
            {
                Console.Error.WriteLine($"Invalid opcode: {args[ai]}");
                continue;
            }

            var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, opcode, $"0x{opcode:X2}");
            if (raw != null)
            {
                Console.WriteLine($"  AT 0x{opcode:X2}: {BitConverter.ToString(raw)} ({raw.Length} bytes) int32={BitConverter.ToInt32(raw.Length >= 4 ? raw[..4] : raw.Concat(new byte[4 - raw.Length]).ToArray(), 0)}");
            }
            else
            {
                Console.WriteLine($"  AT 0x{opcode:X2}: (null/failed)");
            }
        }

        return 0;
    }

    public static async Task<int> RunAtWriteAsync(string[] args)
    {
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");

        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: at-write <opcode_hex> <value_int> [--read-back <get_opcode_hex>]");
            return 1;
        }

        int.TryParse(args[1].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out var setOpcode);
        int.TryParse(args[2], out var value);
        var getOpcode = setOpcode + 1;
        for (var ai = 3; ai < args.Length - 1; ai++)
        {
            if (args[ai] == "--read-back")
            {
                int.TryParse(args[ai + 1].Replace("0x", "").Replace("0X", ""), NumberStyles.HexNumber, null, out getOpcode);
            }
        }

        var before = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, getOpcode, $"GET 0x{getOpcode:X2}");
        Console.WriteLine($"BEFORE: AT 0x{getOpcode:X2} = {(before != null ? BitConverter.ToString(before) : "(null)")}");

        Console.WriteLine($"WRITING: AT 0x{setOpcode:X2} value={value} (bytes: {BitConverter.ToString(BitConverter.GetBytes(value))})");
        var ok = await NativeXuAtCommandProvider.SendNamedSetCommandPublicAsync(dev, setOpcode, BitConverter.GetBytes(value), $"SET 0x{setOpcode:X2}={value}");
        Console.WriteLine($"Result: {ok}");

        await Task.Delay(500);

        var after = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, getOpcode, $"GET 0x{getOpcode:X2}");
        Console.WriteLine($"AFTER: AT 0x{getOpcode:X2} = {(after != null ? BitConverter.ToString(after) : "(null)")}");

        return ok ? 0 : 1;
    }

    public static async Task<int> RunAtSetInputAsync(string[] args)
    {
        var atVal = args.Length > 1 ? int.Parse(args[1]) : 0;
        var noRestore = args.Any(a => a == "--no-restore");
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");
        var beforeInput = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, NativeXuProbeCommands.CmdInputSource, "InputSource");
        Console.WriteLine($"Before: InputSource={NativeXuProbeFormatting.FormatRaw(beforeInput)}");
        Console.WriteLine($"Sending AT SetInputSource(0x34) = {atVal} (1 byte)...");
        var ok = await NativeXuAtCommandProvider.SetInputSourceAsync(dev, atVal);
        Console.WriteLine($"Result: {ok}");
        await Task.Delay(500);
        var afterInput = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, NativeXuProbeCommands.CmdInputSource, "InputSource");
        Console.WriteLine($"After: InputSource={NativeXuProbeFormatting.FormatRaw(afterInput)}");
        if (!noRestore && beforeInput?.Length > 0)
        {
            Console.WriteLine($"Restoring to {beforeInput[0]}...");
            await NativeXuAtCommandProvider.SetInputSourceAsync(dev, beforeInput[0]);
            await Task.Delay(300);
            var restored = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, NativeXuProbeCommands.CmdInputSource, "InputSource");
            Console.WriteLine($"Restored: InputSource={NativeXuProbeFormatting.FormatRaw(restored)}");
        }

        return ok ? 0 : 1;
    }
}

static class NativeXuProbeI2cSwitch
{
    public static async Task<int> RunAsync(string[] args)
    {
        var target = args.Length > 1 ? args[1].ToLowerInvariant() : "analog";
        var dev = NativeXuProbeDeviceLocator.Find("4K X");
        if (dev == null)
        {
            Console.Error.WriteLine("No device");
            return 1;
        }

        Console.WriteLine($"Device: {dev.Name}");
        Console.WriteLine($"Target: {target}");

        Console.WriteLine("\n--- Current I2C AT state ---");
        foreach (var (label, op, param) in new[] {
            ("DacHpOnOff(0x09)", 0x09, 0x42),
            ("Opcode(0x03)", 0x03, 0xA0),
            ("Opcode(0x04)", 0x04, 0x0E),
            ("Opcode(0x07)", 0x07, 0x00),
            ("Opcode(0x0E)", 0x0E, 0x00),
            ("Opcode(0x0F)", 0x0F, 0x00),
            ("Opcode(0x10)", 0x10, 0x00),
            ("Opcode(0x11)", 0x11, 0x00),
        })
        {
            var frame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)op, (byte)param };
            var resp = await NativeXuProbeI2cTransport.SendI2cAtGetAsync(dev, frame);
            Console.WriteLine($"  {label}: {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
        }

        Console.WriteLine("\n--- Current UVC AT state ---");
        foreach (var (label, op) in new[] {
            ("InputSource(0x35)", 0x35),
            ("AudioFormat(0x04)", 0x04),
            ("AdcOnOff(0x74)", 0x74),
            ("DacHpOnOff(0x75)", 0x75),
            ("AT_0x5B", 0x5B),
            ("AT_0x52", 0x52),
            ("AT_0x51", 0x51),
        })
        {
            var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, op, label);
            Console.WriteLine($"  {label}: {(raw != null ? BitConverter.ToString(raw) : "(null)")}");
        }

        Console.WriteLine($"\n--- Sending audio switch sequence ({target}) ---");

        var dacState = await NativeXuProbeI2cTransport.SendI2cAtGetAsync(dev, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 });
        Console.WriteLine($"  1. I2C GET 0x09/42 = {(dacState != null ? BitConverter.ToString(dacState) : "(null)")}");

        var audioSourceValue = (byte)(target == "analog" ? 0x01 : 0x00);
        var set04 = await NativeXuProbeI2cTransport.SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, audioSourceValue });
        Console.WriteLine($"  2. I2C SET 0x04 = 0x{audioSourceValue:X2}: {(set04 ? "OK" : "failed")}");

        var state03 = await NativeXuProbeI2cTransport.SendI2cAtGetAsync(dev, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 });
        Console.WriteLine($"  3. I2C GET 0x03/A0 = {(state03 != null ? BitConverter.ToString(state03) : "(null)")}");

        var set0E = await NativeXuProbeI2cTransport.SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x0E, 0x01 });
        Console.WriteLine($"  4. I2C SET 0x0E = 01: {(set0E ? "OK" : "failed")}");

        var set10 = await NativeXuProbeI2cTransport.SendI2cAtSetAsync(dev, new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x10, 0x01 });
        Console.WriteLine($"  5. I2C SET 0x10 = 01: {(set10 ? "OK" : "failed")}");

        var set5B = await NativeXuAtCommandProvider.SendNamedSetCommandPublicAsync(
            dev, 0x5B, new byte[] { 0x00, 0x05, 0x00, 0x00 }, "AT_0x5B_commit");
        Console.WriteLine($"  6. UVC AT SET 0x5B = 00-05-00-00: {(set5B ? "OK" : "failed")}");

        await Task.Delay(500);

        Console.WriteLine("\n--- Final I2C AT state ---");
        foreach (var (label, op, param) in new[] {
            ("DacHpOnOff(0x09)", 0x09, 0x42),
            ("Opcode(0x04)", 0x04, 0x0E),
            ("Opcode(0x0E)", 0x0E, 0x00),
            ("Opcode(0x10)", 0x10, 0x00),
        })
        {
            var frame = new byte[] { 0x00, 0x4A, 0x02, 0x00, (byte)op, (byte)param };
            var resp = await NativeXuProbeI2cTransport.SendI2cAtGetAsync(dev, frame);
            Console.WriteLine($"  {label}: {(resp != null ? BitConverter.ToString(resp) : "(null)")}");
        }

        Console.WriteLine("\n--- Final UVC AT state ---");
        foreach (var (label, op) in new[] {
            ("InputSource(0x35)", 0x35),
            ("AudioFormat(0x04)", 0x04),
        })
        {
            var raw = await NativeXuAtCommandProvider.ReadAtCommandAsync(dev, op, label);
            Console.WriteLine($"  {label}: {(raw != null ? BitConverter.ToString(raw) : "(null)")}");
        }

        return 0;
    }
}

static class NativeXuProbeServiceProbe
{
    public static async Task<int> RunServiceControlProbeAsync(string[] args)
    {
        var deviceNameFilter = "4K X";
        string? targetMode = null;
        double? targetGain = null;
        var dumpPayload = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--device" when i + 1 < args.Length:
                    deviceNameFilter = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    targetMode = args[++i];
                    break;
                case "--gain" when i + 1 < args.Length && double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out var gain):
                    targetGain = gain;
                    break;
                case "--dump-payload":
                    dumpPayload = true;
                    break;
            }
        }

        var device = NativeXuProbeDeviceLocator.Find(deviceNameFilter);

        if (device == null)
        {
            Console.Error.WriteLine("No capture device found.");
            return 1;
        }

        var service = new NativeXuAudioControlService();

        if (dumpPayload)
        {
            await PrintServicePayloadSnapshotAsync(service, device).ConfigureAwait(false);
        }

        var initial = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
        PrintServiceState("Initial", initial);

        if (!initial.IsSupported)
        {
            Console.Error.WriteLine("Service reports device audio control unsupported.");
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(targetMode))
        {
            var applied = await service.SetAudioModeAsync(device, targetMode, CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine($"Set mode '{targetMode}': {(applied ? "ok" : "failed")}");
        }

        if (targetGain.HasValue)
        {
            var applied = await service.SetAnalogGainPercentAsync(device, targetGain.Value, CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine($"Set gain '{targetGain.Value:0}': {(applied ? "ok" : "failed")}");
        }

        var final = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
        PrintServiceState("Final", final);
        return 0;
    }

    public static async Task<int> RunServiceSmokeAsync(CaptureDevice device)
    {
        var service = new NativeXuAudioControlService();

        await PrintServiceStateAsync(service, device, "Before");

        var setModeResult = await service.SetAudioModeAsync(device, "Analog", CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine($"SetAudioModeAsync('Analog') => {setModeResult}");

        await PrintServiceStateAsync(service, device, "After mode");

        var setGainResult = await service.SetAnalogGainPercentAsync(device, 50d, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine($"SetAnalogGainPercentAsync(50) => {setGainResult}");

        await PrintServiceStateAsync(service, device, "After gain");
        return 0;
    }

    private static Task<NativeXuAudioControlService.DeviceAudioControlState> ReadServiceStateAsync(
        NativeXuAudioControlService service,
        CaptureDevice device)
        => service.ReadStateAsync(device, CancellationToken.None);

    private static void PrintServiceState(string title, NativeXuAudioControlService.DeviceAudioControlState state)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title} service state ==");
        Console.WriteLine($"IsSupported: {state.IsSupported}");
        Console.WriteLine($"InterfacePath: {state.InterfacePath ?? "(null)"}");
        Console.WriteLine($"Mode: {state.Mode ?? "(null)"}");
        Console.WriteLine($"AnalogGainPercent: {state.AnalogGainPercent?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}");
        Console.WriteLine($"RawGainValue: {state.RawGainValue?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}");
    }

    private static async Task PrintServicePayloadSnapshotAsync(NativeXuAudioControlService service, CaptureDevice device)
    {
        var snapshot = await service.ReadPayloadSnapshotAsync(device, CancellationToken.None).ConfigureAwait(false);
        if (snapshot == null)
        {
            Console.WriteLine("Service payload snapshot: null");
            return;
        }

        Console.WriteLine("== Service payload snapshot ==");
        Console.WriteLine($"DeviceId: {snapshot.DeviceId ?? "(null)"}");
        Console.WriteLine($"DeviceName: {snapshot.DeviceName ?? "(null)"}");
        Console.WriteLine($"VendorProduct: {FormatVendorProduct(snapshot.VendorId, snapshot.ProductId)}");
        Console.WriteLine($"InterfacePath: {snapshot.InterfacePath}");
        Console.WriteLine($"NodeId: {snapshot.NodeId}");
        Console.WriteLine($"SelectorId: {snapshot.SelectorId}");
        Console.WriteLine($"TimestampUtc: {snapshot.TimestampUtc:O}");
        Console.WriteLine($"ControlByteIndexes: {string.Join(",", snapshot.ControlByteIndexes)}");
        Console.WriteLine($"VolatileByteIndexes: {string.Join(",", snapshot.VolatileByteIndexes)}");
        Console.WriteLine($"RawLength: {snapshot.RawPayload.Length}");
        Console.WriteLine($"RawHex: {BitConverter.ToString(snapshot.RawPayload).Replace("-", string.Empty)}");
        Console.WriteLine($"NormalizedLength: {snapshot.NormalizedPayload.Length}");
        Console.WriteLine($"NormalizedHex: {BitConverter.ToString(snapshot.NormalizedPayload).Replace("-", string.Empty)}");
    }

    private static async Task PrintServiceStateAsync(NativeXuAudioControlService service, CaptureDevice device, string label)
    {
        var result = await ReadServiceStateAsync(service, device).ConfigureAwait(false);
        PrintServiceState(label, result);
    }

    private static string FormatVendorProduct(ushort? vendorId, ushort? productId)
        => vendorId.HasValue && productId.HasValue
            ? $"VID_0x{vendorId.Value:X4} PID_0x{productId.Value:X4}"
            : "(unknown)";
}
