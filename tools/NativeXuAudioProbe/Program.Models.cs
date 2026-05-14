enum ValueKind
{
    Byte,
    Int16,
    Int32
}

sealed record GetterSpec(string Name, int Cmd, ValueKind Kind);

sealed record SetterSpec(string Name, int SetCmd, int ReadbackCmd, int PayloadWidth = 2);

sealed record SetExperiment(string Group, SetterSpec Setter, string DisplayValue, byte[] Payload);

sealed record AtReadResult(string Label, byte[]? Payload, string DisplayValue, object? TypedValue);

sealed record ChangedValue(string Label, string Before, string After);

sealed class ExperimentResult
{
    public ExperimentResult(SetExperiment experiment, bool writeOk, IReadOnlyDictionary<int, AtReadResult> before, IReadOnlyDictionary<int, AtReadResult> after)
    {
        Experiment = experiment;
        WriteOk = writeOk;
        ChangedValues = before.Keys
            .Where(key => before[key].DisplayValue != after[key].DisplayValue || !AreEqual(before[key].Payload, after[key].Payload))
            .Select(key => new ChangedValue(before[key].Label, before[key].DisplayValue, after[key].DisplayValue))
            .ToArray();
    }

    public SetExperiment Experiment { get; }
    public bool WriteOk { get; }
    public IReadOnlyList<ChangedValue> ChangedValues { get; }
    public bool HasAnyChange => ChangedValues.Count > 0;

    private static bool AreEqual(byte[]? a, byte[]? b)
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
}
