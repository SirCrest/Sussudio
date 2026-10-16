using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace Sussudio.Services.Performance;

public enum TuningKnobType
{
    Int,
    Double,
    Bool,
    /// <summary>One of a fixed list of strings (for example an MMCSS task name).</summary>
    Choice,
}

/// <summary>When the app reads the knob's environment variable, so a change takes effect.</summary>
public enum TuningKnobApply
{
    /// <summary>Read continuously (or at each use); a change applies immediately.</summary>
    Live,
    PreviewRestart,
    RecordingStart,
    FlashbackRestart,
    /// <summary>
    /// Read by each part of the capture pipeline as it starts: preview, a recording, Flashback. They share one capture,
    /// so a change takes effect when preview restarts with no recording running, and Flashback restarts with it.
    /// </summary>
    PipelineRestart,
    /// <summary>Read once (constructor, static initializer or startup); needs a relaunch.</summary>
    AppRestart,
}

public static class TuningKnobApplyNames
{
    public static string ToWire(TuningKnobApply apply) => apply switch
    {
        TuningKnobApply.Live => "live",
        TuningKnobApply.PreviewRestart => "preview-restart",
        TuningKnobApply.RecordingStart => "recording-start",
        TuningKnobApply.FlashbackRestart => "flashback-restart",
        TuningKnobApply.PipelineRestart => "pipeline-restart",
        TuningKnobApply.AppRestart => "app-restart",
        _ => "unknown",
    };

    public static string DescribeEffect(TuningKnobApply apply) => apply switch
    {
        TuningKnobApply.Live => "Applies immediately.",
        TuningKnobApply.PreviewRestart => "Takes effect the next time preview starts or reinitializes.",
        TuningKnobApply.RecordingStart => "Takes effect the next time a recording starts.",
        TuningKnobApply.FlashbackRestart => "Takes effect the next time the Flashback backend starts.",
        TuningKnobApply.PipelineRestart =>
            "Takes effect the next time preview starts with no recording running; preview, recordings and Flashback share that capture.",
        TuningKnobApply.AppRestart => "Read once at startup: requires an app restart to take effect.",
        _ => string.Empty,
    };
}

/// <summary>
/// One tunable <c>SUSSUDIO_*</c> environment variable: wire name, value type, default (display text), allowed range
/// or choices, when it is read, the file that reads it, and a one-line description.
/// </summary>
public sealed record TuningKnobDefinition(
    string Name,
    TuningKnobType Type,
    string Default,
    double? Min,
    double? Max,
    IReadOnlyList<string>? Choices,
    TuningKnobApply Apply,
    string Owner,
    string Description);

/// <summary>A knob that exists but is deliberately not settable at run time, with the reason.</summary>
public sealed record TuningKnobExclusion(string Name, string Reason);

public enum TuningKnobFailure
{
    None,
    UnknownKnob,
    WrongType,
    OutOfRange,
    MissingValue,
    /// <summary>A running perf experiment holds the knob (<see cref="PerfExperimentLease"/>).</summary>
    Leased,
}

public sealed record TuningKnobChangeResult(
    bool Success,
    TuningKnobFailure Failure,
    string? Error,
    string Name,
    string? Old,
    string? New,
    string? Apply,
    string? Note);

/// <summary>A knob's current state. <see cref="Value"/> and <see cref="Launch"/> are null when the variable is unset.</summary>
public sealed record TuningKnobState(TuningKnobDefinition Definition, string? Value, string? Launch, bool Overridden);

/// <summary>Seam over the process environment so knob logic is testable without mutating the real environment.</summary>
public interface ITuningKnobEnvironment
{
    string? Get(string name);

    /// <summary>Sets the variable, or removes it when <paramref name="value"/> is null.</summary>
    void Set(string name, string? value);
}

/// <summary>
/// Validates and applies runtime knob changes by writing the process environment variable every existing read site
/// already consults. Values of the wrong type or outside the range are rejected, never clamped. The launch value
/// (including "unset") is captured the first time a knob is touched and <see cref="Reset"/> restores it.
/// Overrides are never persisted. Control-plane only: it takes a lock.
/// </summary>
public sealed class TuningKnobStore
{
    private const string UnsetText = "<unset>";

    private readonly object _gate = new();
    private readonly Dictionary<string, TuningKnobDefinition> _byName;
    private readonly IReadOnlyList<TuningKnobDefinition> _catalog;
    private readonly ITuningKnobEnvironment _environment;
    private readonly Action<string> _log;
    private readonly Dictionary<string, string?> _launch = new(StringComparer.OrdinalIgnoreCase);

    public TuningKnobStore(IReadOnlyList<TuningKnobDefinition> catalog, ITuningKnobEnvironment environment, Action<string> log)
    {
        _catalog = catalog;
        _environment = environment;
        _log = log;
        _byName = new Dictionary<string, TuningKnobDefinition>(catalog.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var definition in catalog)
        {
            _byName[definition.Name] = definition;
        }
    }

    /// <summary>
    /// Raised, outside the lock and on the calling thread, after a knob was set or reset. Demand-driven sensors
    /// subscribe so a live <c>auto|on|off</c> knob applies at once without a polling thread. Handlers must be quick
    /// and must not throw.
    /// </summary>
    public event Action? Changed;

    /// <summary>Captures every knob's launch value now. Call once early so later changes cannot be mistaken for launch state.</summary>
    public void CaptureLaunchValues()
    {
        lock (_gate)
        {
            foreach (var definition in _catalog)
            {
                CaptureLaunchLocked(definition);
            }
        }
    }

    /// <summary>All knobs, optionally filtered by a case-insensitive substring of the name.</summary>
    public IReadOnlyList<TuningKnobState> List(string? filter)
    {
        lock (_gate)
        {
            var states = new List<TuningKnobState>();
            foreach (var definition in _catalog)
            {
                if (!string.IsNullOrWhiteSpace(filter)
                    && definition.Name.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                states.Add(StateLocked(definition));
            }

            return states;
        }
    }

    /// <summary>One knob by case-insensitive name, or <c>null</c> when the catalog has no such knob.</summary>
    public TuningKnobState? Find(string? name)
    {
        if (!TryFind(name, out var definition))
        {
            return null;
        }

        lock (_gate)
        {
            return StateLocked(definition);
        }
    }

    /// <summary>Knobs whose current value differs from the launch value. Untouched knobs are never read.</summary>
    public IReadOnlyList<TuningKnobState> Overrides()
    {
        lock (_gate)
        {
            var states = new List<TuningKnobState>();
            foreach (var definition in _catalog)
            {
                if (!_launch.ContainsKey(definition.Name))
                {
                    continue;
                }

                var state = StateLocked(definition);
                if (state.Overridden)
                {
                    states.Add(state);
                }
            }

            return states;
        }
    }

    public TuningKnobChangeResult Set(string? name, string? value)
    {
        if (!TryFind(name, out var definition))
        {
            return Fail(TuningKnobFailure.UnknownKnob, name ?? string.Empty, $"Unknown tuning knob '{name}'. Use the knob listing for valid names.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return Fail(
                TuningKnobFailure.MissingValue,
                definition.Name,
                $"{definition.Name} needs a value. To restore the launch value use reset instead.");
        }

        if (!TryNormalize(definition, value.Trim(), out var canonical, out var failure, out var error))
        {
            return Fail(failure, definition.Name, error);
        }

        TuningKnobChangeResult result;
        lock (_gate)
        {
            CaptureLaunchLocked(definition);
            var previous = _environment.Get(definition.Name);
            _environment.Set(definition.Name, canonical);
            result = Succeed(definition, previous, canonical);
        }

        RaiseChanged();
        return result;
    }

    /// <summary>Restores the launch value; an unset launch value removes the variable again.</summary>
    public TuningKnobChangeResult Reset(string? name)
    {
        if (!TryFind(name, out var definition))
        {
            return Fail(TuningKnobFailure.UnknownKnob, name ?? string.Empty, $"Unknown tuning knob '{name}'. Use the knob listing for valid names.");
        }

        TuningKnobChangeResult result;
        lock (_gate)
        {
            var launch = CaptureLaunchLocked(definition);
            var previous = _environment.Get(definition.Name);
            _environment.Set(definition.Name, launch);
            result = Succeed(definition, previous, launch);
        }

        RaiseChanged();
        return result;
    }

    private void RaiseChanged()
    {
        // A subscriber must never turn a knob command into a failure; sensors report their own faults.
        try
        {
            Changed?.Invoke();
        }
        catch (Exception)
        {
            // Intentionally swallowed: the environment variable was already written.
        }
    }

    private bool TryFind(string? name, out TuningKnobDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(name) && _byName.TryGetValue(name.Trim(), out var found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }

    private string? CaptureLaunchLocked(TuningKnobDefinition definition)
    {
        if (!_launch.TryGetValue(definition.Name, out var launch))
        {
            launch = _environment.Get(definition.Name);
            _launch[definition.Name] = launch;
        }

        return launch;
    }

    private TuningKnobState StateLocked(TuningKnobDefinition definition)
    {
        var launch = CaptureLaunchLocked(definition);
        var value = _environment.Get(definition.Name);
        return new TuningKnobState(definition, value, launch, !string.Equals(value, launch, StringComparison.Ordinal));
    }

    private TuningKnobChangeResult Succeed(TuningKnobDefinition definition, string? previous, string? current)
    {
        var apply = TuningKnobApplyNames.ToWire(definition.Apply);

        // Only a change is logged: resetting a knob that was never set, or setting the value it already has, would
        // otherwise write an old=X new=X line that says nothing happened. The caller still gets a success result.
        if (!string.Equals(previous, current, StringComparison.Ordinal))
        {
            _log($"TUNING_KNOB_SET name={definition.Name} old={previous ?? UnsetText} new={current ?? UnsetText} apply={apply}");
        }

        return new TuningKnobChangeResult(
            true,
            TuningKnobFailure.None,
            null,
            definition.Name,
            previous,
            current,
            apply,
            TuningKnobApplyNames.DescribeEffect(definition.Apply));
    }

    private static TuningKnobChangeResult Fail(TuningKnobFailure failure, string name, string error) =>
        new(false, failure, error, name, null, null, null, null);

    private static bool TryNormalize(
        TuningKnobDefinition definition,
        string text,
        out string canonical,
        out TuningKnobFailure failure,
        out string error)
    {
        canonical = string.Empty;
        failure = TuningKnobFailure.None;
        error = string.Empty;

        switch (definition.Type)
        {
            case TuningKnobType.Int:
            {
                var min = (long)(definition.Min ?? long.MinValue);
                var max = (long)(definition.Max ?? long.MaxValue);
                if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                {
                    if (integer < min || integer > max)
                    {
                        failure = TuningKnobFailure.OutOfRange;
                        error = $"{definition.Name} must be an integer in [{min}, {max}]; got {text}.";
                        return false;
                    }

                    canonical = integer.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                if (BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
                {
                    failure = TuningKnobFailure.OutOfRange;
                    error = $"{definition.Name} must be an integer in [{min}, {max}]; got {text}.";
                    return false;
                }

                failure = TuningKnobFailure.WrongType;
                error = $"{definition.Name} expects an integer in [{min}, {max}]; got '{text}'.";
                return false;
            }

            case TuningKnobType.Double:
            {
                var min = definition.Min ?? double.MinValue;
                var max = definition.Max ?? double.MaxValue;
                if (!double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
                    || double.IsNaN(number)
                    || double.IsInfinity(number))
                {
                    failure = TuningKnobFailure.WrongType;
                    error = $"{definition.Name} expects a finite number in [{Format(min)}, {Format(max)}]; got '{text}'.";
                    return false;
                }

                if (number < min || number > max)
                {
                    failure = TuningKnobFailure.OutOfRange;
                    error = $"{definition.Name} must be a number in [{Format(min)}, {Format(max)}]; got {text}.";
                    return false;
                }

                canonical = Format(number);
                return true;
            }

            case TuningKnobType.Bool:
            {
                if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    canonical = "1";
                    return true;
                }

                if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    canonical = "0";
                    return true;
                }

                failure = TuningKnobFailure.WrongType;
                error = $"{definition.Name} expects a boolean (0, 1, true or false); got '{text}'.";
                return false;
            }

            case TuningKnobType.Choice:
            {
                var match = definition.Choices?.FirstOrDefault(choice => choice.Equals(text, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                {
                    failure = TuningKnobFailure.WrongType;
                    error = $"{definition.Name} expects one of: {string.Join(", ", definition.Choices ?? Array.Empty<string>())}; got '{text}'.";
                    return false;
                }

                canonical = match;
                return true;
            }

            default:
                failure = TuningKnobFailure.WrongType;
                error = $"{definition.Name} has an unsupported type.";
                return false;
        }
    }

    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
