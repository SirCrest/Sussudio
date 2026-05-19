using Microsoft.Win32.SafeHandles;
using static KsAudioNodeProbeNative;

static partial class KsAudioNodeProbeScanWorkflows
{
    private static void RunExtendedNodeTests(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Extended node tests ==");
        for (var nodeId = 0; nodeId < 32; nodeId++)
        {
            for (var propId = 1; propId <= 20; propId++)
            {
                for (var ch = -1; ch <= 1; ch++)
                {
                    if (!TryAudioGetLong(handle, nodeId, propId, ch, out var val, out _))
                    {
                        continue;
                    }

                    var pName = PropertyNames.TryGetValue(propId, out var n) ? n : $"Property({propId})";
                    Console.WriteLine($"  Node {nodeId}, {pName}, ch={ch}: GET={val}");
                    RunExtendedSetTest(handle, nodeId, propId, ch, val);
                }
            }
        }
    }

    private static void RunExtendedSetTest(SafeFileHandle handle, int nodeId, int propId, int ch, int val)
    {
        if (propId == 12) // MUTE
        {
            var newVal = val == 0 ? 1 : 0;
            if (TryAudioSetLong(handle, nodeId, propId, ch, newVal, out var setErr))
            {
                TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                Console.WriteLine($"    SET {newVal} -> readback={afterSet}");
                TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                Console.WriteLine($"    RESTORED to {val}");
            }
            else
            {
                Console.WriteLine($"    SET {newVal} FAILED: {DescribeWin32(setErr)}");
            }
        }
        else if (propId == 11) // MUX
        {
            foreach (var target in new[] { 0, 1, 2, 3 })
            {
                if (TryAudioSetLong(handle, nodeId, propId, ch, target, out var setErr))
                {
                    TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                    Console.WriteLine($"    MUX SET {target} -> readback={afterSet}");
                }
                else
                {
                    Console.WriteLine($"    MUX SET {target} FAILED: {DescribeWin32(setErr)}");
                }
            }

            TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
        }
        else if (propId == 4) // VOLUME
        {
            var testVal = val + 65536;
            if (TryAudioSetLong(handle, nodeId, propId, ch, testVal, out var setErr))
            {
                TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                Console.WriteLine($"    VOLUME SET {testVal} -> readback={afterSet}");
                TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                Console.WriteLine($"    RESTORED to {val}");
            }
            else
            {
                Console.WriteLine($"    VOLUME SET {testVal} FAILED: {DescribeWin32(setErr)}");
            }
        }
        else if (propId == 13) // BASS (might be repurposed)
        {
            foreach (var target in new[] { 0, 1, 2, 3 })
            {
                if (target == val) continue;
                if (TryAudioSetLong(handle, nodeId, propId, ch, target, out var setErr))
                {
                    TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                    Console.WriteLine($"    BASS SET {target} -> readback={afterSet}");
                }
                else
                {
                    Console.WriteLine($"    BASS SET {target} FAILED: {DescribeWin32(setErr)}");
                }
            }

            TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
        }
    }

    private static void RunAdcVolumeProbe(SafeFileHandle handle)
    {
        Console.WriteLine("== ADC volume probe ==");
        foreach (var channel in new[] { -1, 0, 1 })
        {
            if (TryAudioGetLong(handle, nodeId: 0, propertyId: 4, channel, out var value, out var error))
            {
                Console.WriteLine($"Read volume channel={channel}: {value}");

                var target = value == 0 ? -6 * 65536 : value + 65536;
                if (TryAudioSetLong(handle, nodeId: 0, propertyId: 4, channel, target, out var setError))
                {
                    if (TryAudioGetLong(handle, nodeId: 0, propertyId: 4, channel, out var after, out error))
                    {
                        Console.WriteLine($"  Set volume -> {target}; readback={after}");
                    }
                    else
                    {
                        Console.WriteLine($"  Set volume -> {target}; readback failed: {DescribeWin32(error)}");
                    }

                    TryAudioSetLong(handle, nodeId: 0, propertyId: 4, channel, value, out _);
                }
                else
                {
                    Console.WriteLine($"  Set volume channel={channel} failed: {DescribeWin32(setError)}");
                }
            }
            else
            {
                Console.WriteLine($"Read volume channel={channel} failed: {DescribeWin32(error)}");
            }
        }
    }

    private static void RunMuxProbe(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Mux probe ==");
        if (TryAudioGetLong(handle, nodeId: 3, propertyId: 11, channel: 0, out var muxValue, out var muxError))
        {
            Console.WriteLine($"Read mux source: {muxValue}");
            foreach (var target in new[] { 0, 1, 2, 3 })
            {
                if (TryAudioSetLong(handle, nodeId: 3, propertyId: 11, channel: 0, target, out var setError))
                {
                    if (TryAudioGetLong(handle, nodeId: 3, propertyId: 11, channel: 0, out var after, out muxError))
                    {
                        Console.WriteLine($"  Set mux -> {target}; readback={after}");
                    }
                    else
                    {
                        Console.WriteLine($"  Set mux -> {target}; readback failed: {DescribeWin32(muxError)}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Set mux -> {target} failed: {DescribeWin32(setError)}");
                }
            }

            TryAudioSetLong(handle, nodeId: 3, propertyId: 11, channel: 0, muxValue, out _);
        }
        else
        {
            Console.WriteLine($"Read mux source failed: {DescribeWin32(muxError)}");
        }
    }

    private static void RunMuteProbe(SafeFileHandle handle)
    {
        Console.WriteLine();
        Console.WriteLine("== Mute probe ==");
        if (TryAudioGetLong(handle, nodeId: 2, propertyId: 12, channel: 0, out var muteValue, out var muteError))
        {
            Console.WriteLine($"Read mute: {muteValue}");
            foreach (var target in new[] { 0, 1 })
            {
                if (TryAudioSetLong(handle, nodeId: 2, propertyId: 12, channel: 0, target, out var setError))
                {
                    if (TryAudioGetLong(handle, nodeId: 2, propertyId: 12, channel: 0, out var after, out muteError))
                    {
                        Console.WriteLine($"  Set mute -> {target}; readback={after}");
                    }
                    else
                    {
                        Console.WriteLine($"  Set mute -> {target}; readback failed: {DescribeWin32(muteError)}");
                    }
                }
                else
                {
                    Console.WriteLine($"  Set mute -> {target} failed: {DescribeWin32(setError)}");
                }
            }

            TryAudioSetLong(handle, nodeId: 2, propertyId: 12, channel: 0, muteValue, out _);
        }
        else
        {
            Console.WriteLine($"Read mute failed: {DescribeWin32(muteError)}");
        }
    }
}
