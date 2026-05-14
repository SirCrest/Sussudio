using System.Globalization;

static class NativeXuProbeExperimentPayloads
{
    public static IEnumerable<SetExperiment> BuildShortExperiments(string group, IReadOnlyList<SetterSpec> setters, IReadOnlyList<short> values)
    {
        foreach (var setter in setters)
        {
            foreach (var value in values)
            {
                yield return new SetExperiment(group, setter, value.ToString(CultureInfo.InvariantCulture), BuildPayload(setter.PayloadWidth, value));
            }
        }
    }

    public static IEnumerable<SetExperiment> BuildIntExperiments(string group, IReadOnlyList<SetterSpec> setters, IReadOnlyList<int> values)
    {
        foreach (var setter in setters)
        {
            foreach (var value in values)
            {
                yield return new SetExperiment(group, setter, value.ToString(CultureInfo.InvariantCulture), BuildPayload(setter.PayloadWidth, value));
            }
        }
    }

    public static IEnumerable<SetExperiment> BuildByteExperiments(string group, IReadOnlyList<SetterSpec> setters, IReadOnlyList<byte> values)
    {
        foreach (var setter in setters)
        {
            foreach (var value in values)
            {
                yield return new SetExperiment(group, setter, value.ToString(CultureInfo.InvariantCulture), BuildPayload(setter.PayloadWidth, value));
            }
        }
    }

    public static byte[] BuildPayload(int width, long value)
    {
        return width switch
        {
            1 => new[] { unchecked((byte)value) },
            2 => BitConverter.GetBytes(unchecked((short)value)),
            4 => BitConverter.GetBytes(unchecked((int)value)),
            _ => throw new InvalidOperationException($"Unsupported payload width {width}.")
        };
    }
}
