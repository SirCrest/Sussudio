using System;
using System.Collections.Generic;

namespace Sussudio.Services.Performance;

/// <summary>
/// Process-wide tuning knobs: <see cref="TuningKnobCatalog"/> over the real process environment, with every change
/// logged as <c>TUNING_KNOB_SET</c>. The store logic (validation, launch-value capture, reset) lives in
/// <see cref="TuningKnobStore"/>, and who may change a knob while a perf experiment runs lives in
/// <see cref="PerfExperimentLease"/>; this class only binds them to the environment, the clock and the logger. Knob
/// changes go through <see cref="Lease"/>. To add a knob see the remarks on <see cref="TuningKnobCatalog"/>.
/// </summary>
public static class TuningKnobs
{
    private static readonly TuningKnobStore Store = new(
        TuningKnobCatalog.Knobs,
        new ProcessEnvironment(),
        message => Logger.Log(message, nameof(TuningKnobs)));

    /// <summary>The perf experiment lease; every automation knob change goes through <see cref="PerfExperimentLease.ChangeKnob"/>.</summary>
    public static PerfExperimentLease Lease { get; } = new(
        Store,
        TimeProvider.System,
        message => Logger.Log(message, nameof(TuningKnobs)));

    /// <summary>Raised after any knob was set or reset; see <see cref="TuningKnobStore.Changed"/>.</summary>
    public static event Action Changed
    {
        add => Store.Changed += value;
        remove => Store.Changed -= value;
    }

    /// <summary>Captures every knob's launch value. Call once at startup, before any command can change one.</summary>
    public static void CaptureLaunchValues() => Store.CaptureLaunchValues();

    public static IReadOnlyList<TuningKnobState> List(string? filter) => Store.List(filter);

    /// <summary>Knobs whose current value differs from the launch value.</summary>
    public static IReadOnlyList<TuningKnobState> Overrides() => Store.Overrides();

    private sealed class ProcessEnvironment : ITuningKnobEnvironment
    {
        public string? Get(string name) => Environment.GetEnvironmentVariable(name);

        public void Set(string name, string? value) => Environment.SetEnvironmentVariable(name, value);
    }
}
