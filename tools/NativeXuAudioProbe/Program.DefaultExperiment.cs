using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Telemetry;
using static NativeXuProbeCommands;
using static NativeXuProbeExperimentPayloads;
using static NativeXuProbeFormatting;

static class NativeXuProbeDefaultExperiment
{
    public static async Task<int> RunAsync(CaptureDevice device)
    {
        var baselineSnapshot = await new NativeXuAtCommandProvider().ReadAsync(device);
        PrintSnapshot("Baseline snapshot", baselineSnapshot);

        var getterSpecs = new[]
        {
            new GetterSpec("AudioFormat", CmdAudioFormat, ValueKind.Byte),
            new GetterSpec("AudioSamplingRate", CmdAudioSamplingRate, ValueKind.Byte),
            new GetterSpec("CurrentInputSource", CmdInputSource, ValueKind.Byte),
            new GetterSpec("AdcOnOff", CmdAudioGetAdcOnOff, ValueKind.Byte),
            new GetterSpec("DacHpOnOff", CmdAudioGetDacHpOnOff, ValueKind.Byte),
            new GetterSpec("AdcVolumeGain", CmdAudioGetAdcVolumeGain, ValueKind.Int16),
            new GetterSpec("HdmiDprxVolumeGain", CmdAudioGetHdmiDprxVolumeGain, ValueKind.Int16),
            new GetterSpec("UacVolumeGain", CmdAudioGetUacVolumeGain, ValueKind.Int16),
            new GetterSpec("AuxInVolume", CmdGetAuxInVolume, ValueKind.Int16),
            new GetterSpec("AuxOutVolume", CmdGetAuxOutVolume, ValueKind.Int16),
            new GetterSpec("UacOut2MixerSource", CmdAudioGetUacOut2MixerSource, ValueKind.Int16),
            new GetterSpec("DacHpMixerSource", CmdAudioGetDacHpMixerSource, ValueKind.Int16),
            new GetterSpec("I2sOutMixerSource", CmdAudioGetI2sOutMixerSource, ValueKind.Int16),
            new GetterSpec("UacOut1Mute", CmdAudioGetUacOut1Mute, ValueKind.Byte),
            new GetterSpec("UacOut2Mute", CmdAudioGetUacOut2Mute, ValueKind.Byte),
            new GetterSpec("DacHpMute", CmdAudioGetDacHpMute, ValueKind.Byte),
            new GetterSpec("I2sOutMute", CmdAudioGetI2sOutMute, ValueKind.Int32),
        };

        var baselineReads = await ReadAllAsync(device, getterSpecs);
        PrintReads("Baseline AT reads", baselineReads);

        var experiments = new List<SetExperiment>();

        experiments.AddRange(BuildShortExperiments(
            "Audio routing",
            new[]
            {
                new SetterSpec("SetUacOut2MixerSource", CmdAudioSetUacOut2MixerSource, CmdAudioGetUacOut2MixerSource),
                new SetterSpec("SetDacHpMixerSource", CmdAudioSetDacHpMixerSource, CmdAudioGetDacHpMixerSource),
                new SetterSpec("SetI2sOutMixerSource", CmdAudioSetI2sOutMixerSource, CmdAudioGetI2sOutMixerSource),
            },
            new short[] { 0, 1, 2, 3, 4, 8, 9 }));

        experiments.AddRange(BuildIntExperiments(
            "Input source",
            new[]
            {
                new SetterSpec("SetInputSourceByte", CmdSetInputSource, CmdInputSource, PayloadWidth: 1),
                new SetterSpec("SetInputSourceShort", CmdSetInputSource, CmdInputSource, PayloadWidth: 2),
                new SetterSpec("SetInputSourceInt", CmdSetInputSource, CmdInputSource, PayloadWidth: 4),
            },
            new[] { 0, 1, 2, 3 }));

        experiments.AddRange(BuildIntExperiments(
            "Audio on/off",
            new[]
            {
                new SetterSpec("SetAdcOnOff", CmdAudioSetAdcOnOff, CmdAudioGetAdcOnOff, PayloadWidth: 4),
                new SetterSpec("SetDacHpOnOff", CmdAudioSetDacHpOnOff, CmdAudioGetDacHpOnOff, PayloadWidth: 4),
            },
            new[] { 0, 1 }));

        experiments.AddRange(BuildByteExperiments(
            "Audio mutes",
            new[]
            {
                new SetterSpec("SetUacOut1Mute", CmdAudioSetUacOut1Mute, CmdAudioGetUacOut1Mute, PayloadWidth: 1),
                new SetterSpec("SetUacOut2Mute", CmdAudioSetUacOut2Mute, CmdAudioGetUacOut2Mute, PayloadWidth: 1),
                new SetterSpec("SetDacHpMute", CmdAudioSetDacHpMute, CmdAudioGetDacHpMute, PayloadWidth: 1),
                new SetterSpec("SetI2sOutMute", CmdAudioSetI2sOutMute, CmdAudioGetI2sOutMute, PayloadWidth: 1),
            },
            new byte[] { 0, 1 }));

        experiments.AddRange(BuildIntExperiments(
            "Audio gain",
            new[]
            {
                new SetterSpec("SetAdcVolumeGainByte", CmdAudioSetAdcVolumeGain, CmdAudioGetAdcVolumeGain, PayloadWidth: 1),
                new SetterSpec("SetAdcVolumeGainShort", CmdAudioSetAdcVolumeGain, CmdAudioGetAdcVolumeGain, PayloadWidth: 2),
                new SetterSpec("SetAdcVolumeGainInt", CmdAudioSetAdcVolumeGain, CmdAudioGetAdcVolumeGain, PayloadWidth: 4),
                new SetterSpec("SetHdmiDprxVolumeGain", CmdAudioSetHdmiDprxVolumeGain, CmdAudioGetHdmiDprxVolumeGain, PayloadWidth: 4),
                new SetterSpec("SetUacVolumeGain", CmdAudioSetUacVolumeGain, CmdAudioGetUacVolumeGain, PayloadWidth: 4),
                new SetterSpec("SetAuxInVolumeByte", CmdSetAuxInVolume, CmdGetAuxInVolume, PayloadWidth: 1),
                new SetterSpec("SetAuxInVolumeShort", CmdSetAuxInVolume, CmdGetAuxInVolume, PayloadWidth: 2),
                new SetterSpec("SetAuxInVolumeInt", CmdSetAuxInVolume, CmdGetAuxInVolume, PayloadWidth: 4),
                new SetterSpec("SetAuxOutVolumeByte", CmdSetAuxOutVolume, CmdGetAuxOutVolume, PayloadWidth: 1),
                new SetterSpec("SetAuxOutVolumeShort", CmdSetAuxOutVolume, CmdGetAuxOutVolume, PayloadWidth: 2),
                new SetterSpec("SetAuxOutVolumeInt", CmdSetAuxOutVolume, CmdGetAuxOutVolume, PayloadWidth: 4),
            },
            new[] { 0, 64, 128, 192, 255 }));

        var results = new List<ExperimentResult>();
        foreach (var experiment in experiments)
        {
            Console.WriteLine();
            Console.WriteLine($"== {experiment.Group} :: {experiment.Setter.Name} -> {experiment.DisplayValue} ==");

            var before = await ReadAllAsync(device, getterSpecs);
            var writeOk = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, experiment.Payload);
            await Task.Delay(200);
            var after = await ReadAllAsync(device, getterSpecs);

            PrintDiff(before, after);
            results.Add(new ExperimentResult(experiment, writeOk, before, after));

            if (before.TryGetValue(experiment.Setter.ReadbackCmd, out var readbackBefore) &&
                readbackBefore.TypedValue is byte byteValue)
            {
                var restorePayload = BuildPayload(experiment.Setter.PayloadWidth, byteValue);
                var restored = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, restorePayload);
                await Task.Delay(150);
                Console.WriteLine($"Restore to {byteValue}: {(restored ? "ok" : "failed")}");
            }
            else if (before.TryGetValue(experiment.Setter.ReadbackCmd, out readbackBefore) &&
                     readbackBefore.TypedValue is short shortValue)
            {
                var restorePayload = BuildPayload(experiment.Setter.PayloadWidth, shortValue);
                var restored = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, restorePayload);
                await Task.Delay(150);
                Console.WriteLine($"Restore to {shortValue}: {(restored ? "ok" : "failed")}");
            }
            else if (before.TryGetValue(experiment.Setter.ReadbackCmd, out readbackBefore) &&
                     readbackBefore.TypedValue is int intValue)
            {
                var restorePayload = BuildPayload(experiment.Setter.PayloadWidth, intValue);
                var restored = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, experiment.Setter.SetCmd, restorePayload);
                await Task.Delay(150);
                Console.WriteLine($"Restore to {intValue}: {(restored ? "ok" : "failed")}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("== Interesting changes ==");
        foreach (var result in results.Where(r => r.HasAnyChange))
        {
            Console.WriteLine($"{result.Experiment.Setter.Name} -> {result.Experiment.DisplayValue} (write {(result.WriteOk ? "ok" : "failed")})");
            foreach (var changed in result.ChangedValues)
            {
                Console.WriteLine($"  {changed.Label}: {changed.Before} -> {changed.After}");
            }
        }

        if (results.All(r => !r.HasAnyChange))
        {
            Console.WriteLine("No getter-visible changes were observed from the current candidate payload set.");
        }

        await RunAnalogGainSequenceAsync(device);

        var finalSnapshot = await new NativeXuAtCommandProvider().ReadAsync(device);
        PrintSnapshot("Final snapshot", finalSnapshot);
        return 0;
    }

    private static async Task RunAnalogGainSequenceAsync(CaptureDevice device)
    {
        Console.WriteLine();
        Console.WriteLine("== Analog gain sequence ==");

        var baselineInput = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdInputSource, "CurrentInputSource");
        var baselineAdcOn = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcOnOff, "AdcOnOff");
        var baselineAdcGain = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcVolumeGain, "AdcVolumeGain");
        Console.WriteLine($"Baseline: input={FormatRaw(baselineInput)} adcOn={FormatRaw(baselineAdcOn)} adcGain={FormatRaw(baselineAdcGain)}");

        var inputOk = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdSetInputSource, BuildPayload(1, 1));
        var adcOnOk = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcOnOff, BuildPayload(4, 1));
        await Task.Delay(200);
        var afterAdcOn = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcOnOff, "AdcOnOff");
        Console.WriteLine($"Set input=1 ok={inputOk}; set adc-on=1 ok={adcOnOk}; adcOnNow={FormatRaw(afterAdcOn)}");

        foreach (var width in new[] { 1, 2, 4 })
        {
            foreach (var value in new[] { 0, 64, 128, 192, 255 })
            {
                var ok = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcVolumeGain, BuildPayload(width, value));
                await Task.Delay(150);
                var gain = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, CmdAudioGetAdcVolumeGain, "AdcVolumeGain");
                Console.WriteLine($"  width={width} value={value} ok={ok} gain={FormatRaw(gain)}");
            }
        }

        if (baselineAdcGain?.Length > 0)
        {
            var baselineAdcGainValue = BitConverter.ToInt32(PadToFourBytes(baselineAdcGain), 0);
            var restoredGain = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcVolumeGain, BuildPayload(4, baselineAdcGainValue));
            Console.WriteLine($"Restore adc gain to baseline={baselineAdcGainValue} ok={restoredGain}");
        }

        if (baselineAdcOn?.Length > 0)
        {
            var baselineAdcOnValue = baselineAdcOn[0];
            var restoredAdcOn = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdAudioSetAdcOnOff, BuildPayload(4, baselineAdcOnValue));
            Console.WriteLine($"Restore adc on/off to baseline={baselineAdcOnValue} ok={restoredAdcOn}");
        }

        if (baselineInput?.Length > 0)
        {
            var baselineInputValue = baselineInput[0];
            var restoredInput = await NativeXuAtCommandProvider.SendAtSetCommandAsync(device, CmdSetInputSource, BuildPayload(1, baselineInputValue));
            Console.WriteLine($"Restore input source to baseline={baselineInputValue} ok={restoredInput}");
        }
    }

    private static byte[] PadToFourBytes(byte[] value)
    {
        if (value.Length >= 4)
        {
            return value;
        }

        var padded = new byte[4];
        Array.Copy(value, padded, value.Length);
        return padded;
    }

    private static async Task<Dictionary<int, AtReadResult>> ReadAllAsync(CaptureDevice device, IEnumerable<GetterSpec> specs)
    {
        var results = new Dictionary<int, AtReadResult>();
        foreach (var spec in specs)
        {
            var payload = await NativeXuAtCommandProvider.ReadAtCommandAsync(device, spec.Cmd, spec.Name);
            results[spec.Cmd] = Decode(spec, payload);
        }

        return results;
    }

    private static AtReadResult Decode(GetterSpec spec, byte[]? payload)
    {
        if (payload == null || payload.Length == 0)
        {
            return new AtReadResult(spec.Name, payload, "unavailable", null);
        }

        return spec.Kind switch
        {
            ValueKind.Byte => new AtReadResult(spec.Name, payload, payload[0].ToString(CultureInfo.InvariantCulture), payload[0]),
            ValueKind.Int16 when payload.Length >= 2 => new AtReadResult(spec.Name, payload, BitConverter.ToInt16(payload, 0).ToString(CultureInfo.InvariantCulture), BitConverter.ToInt16(payload, 0)),
            ValueKind.Int32 when payload.Length >= 4 => new AtReadResult(spec.Name, payload, BitConverter.ToInt32(payload, 0).ToString(CultureInfo.InvariantCulture), BitConverter.ToInt32(payload, 0)),
            _ => new AtReadResult(spec.Name, payload, BitConverter.ToString(payload), null)
        };
    }

    private static void PrintReads(string title, IReadOnlyDictionary<int, AtReadResult> reads)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title} ==");
        foreach (var item in reads.Values)
        {
            Console.WriteLine($"{item.Label}: {item.DisplayValue} raw={FormatRaw(item.Payload)}");
        }
    }

    private static void PrintDiff(IReadOnlyDictionary<int, AtReadResult> before, IReadOnlyDictionary<int, AtReadResult> after)
    {
        foreach (var key in before.Keys.OrderBy(k => k))
        {
            var left = before[key];
            var right = after[key];
            if (left.DisplayValue != right.DisplayValue || !RawEqual(left.Payload, right.Payload))
            {
                Console.WriteLine($"  {left.Label}: {left.DisplayValue} -> {right.DisplayValue} ({FormatRaw(left.Payload)} -> {FormatRaw(right.Payload)})");
            }
        }
    }

    private static bool RawEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null || a.Length != b.Length)
        {
            return false;
        }

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void PrintSnapshot(string title, SourceSignalTelemetrySnapshot snapshot)
    {
        Console.WriteLine();
        Console.WriteLine($"== {title} ==");
        Console.WriteLine($"Availability: {snapshot.Availability}");
        Console.WriteLine($"Origin: {snapshot.Origin} ({snapshot.Confidence})");
        Console.WriteLine($"Source video: {snapshot.Width}x{snapshot.Height} @ {snapshot.FrameRateExact:0.###}");
        Console.WriteLine($"HDR: {snapshot.IsHdr}");
        Console.WriteLine($"Video format: {snapshot.VideoFormat}");
        Console.WriteLine($"Audio format: {snapshot.AudioFormat}");
        Console.WriteLine($"Audio sample rate: {snapshot.AudioSampleRate}");
        Console.WriteLine($"Input source: {snapshot.InputSource}");
        Console.WriteLine($"Diagnostic: {snapshot.DiagnosticSummary}");
    }
}
