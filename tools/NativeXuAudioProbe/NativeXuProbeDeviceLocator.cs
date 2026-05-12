using Sussudio.Models;
using Sussudio.Services.Capture;

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
