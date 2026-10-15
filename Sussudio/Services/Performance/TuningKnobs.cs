using System;
using System.Collections.Generic;

namespace Sussudio.Services.Performance;

/// <summary>
/// Process-wide tuning knobs: <see cref="TuningKnobCatalog"/> over the real process environment, with every change
/// logged as <c>TUNING_KNOB_SET</c>. The store logic (validation, launch-value capture, reset) lives in
/// <see cref="TuningKnobStore"/>; this class only binds it to the environment and the logger. To add a knob see the
/// remarks on <see cref="TuningKnobCatalog"/>.
/// </summary>
public static class TuningKnobs
{
    private static readonly TuningKnobStore Store = new(
        TuningKnobCatalog.Knobs,
        new ProcessEnvironment(),
        message => Logger.Log(message, nameof(TuningKnobs)));

    /// <summary>Captures every knob's launch value. Call once at startup, before any command can change one.</summary>
    public static void CaptureLaunchValues() => Store.CaptureLaunchValues();

    public static IReadOnlyList<TuningKnobState> List(string? filter) => Store.List(filter);

    /// <summary>Knobs whose current value differs from the launch value.</summary>
    public static IReadOnlyList<TuningKnobState> Overrides() => Store.Overrides();

    public static TuningKnobChangeResult Set(string? name, string? value) => Store.Set(name, value);

    public static TuningKnobChangeResult Reset(string? name) => Store.Reset(name);

    private sealed class ProcessEnvironment : ITuningKnobEnvironment
    {
        public string? Get(string name) => Environment.GetEnvironmentVariable(name);

        public void Set(string name, string? value) => Environment.SetEnvironmentVariable(name, value);
    }
}
