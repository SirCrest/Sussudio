using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools.Performance;

public sealed class PerfExperimentOptions
{
    /// <summary>Waits between steps. Defaults to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>; tests inject virtual time.</summary>
    public Func<TimeSpan, CancellationToken, Task>? DelayAsync { get; init; }

    /// <summary>Where artifacts go. Defaults to <c>artifacts/perf-experiments/&lt;timestamp&gt;-&lt;name&gt;</c> under the current directory.</summary>
    public string? OutputDirectory { get; init; }

    /// <summary>Write result.json, summary.md, spec.json and one raw window report per run. A write failure is a warning, never a status change.</summary>
    public bool WriteArtifacts { get; init; } = true;

    /// <summary>How long a preview or Flashback restart may take to read back as running.</summary>
    public TimeSpan ReadyTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan ReadyPollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Upper bound for the whole restore after a run, which uses its own token so a canceled run still restores.</summary>
    public TimeSpan CleanupTimeout { get; init; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// How long the app keeps the experiment lease without a renewal (10 to 600 s). The runner renews it four times per
    /// TTL while it runs; if the runner dies, the app restores the leased knobs this long after the last renewal.
    /// </summary>
    public TimeSpan LeaseTimeToLive { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>One human-readable line per milestone (run starting, restore). Called on the runner's thread.</summary>
    public Action<string>? Progress { get; init; }
}

/// <summary>One scheduled run: <see cref="Repeat"/> and <see cref="Variant"/> are zero-based indexes.</summary>
public readonly record struct PerfExperimentScheduleStep(int Repeat, int Variant);

/// <summary>
/// Runs a knob A/B experiment against the live app through the automation pipe (docs/architecture/performance-telemetry.md
/// section 7): validate the spec against the app, run the variants interleaved, measure one performance window per run,
/// compare the variants, and always put the app back the way it was. The command sender is injected, so ssctl, the MCP
/// server and the tests all drive this one runner.
/// </summary>
public static class PerfExperimentRunner
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    /// <summary>
    /// Run order. Each repeat runs every variant once, and the direction flips every repeat (A B, B A, A B ...), so a
    /// slow drift in the machine (thermals, background load) lands on both variants equally instead of favoring the one
    /// that always ran first.
    /// </summary>
    public static IReadOnlyList<PerfExperimentScheduleStep> BuildSchedule(int variantCount, int repeats)
    {
        var schedule = new List<PerfExperimentScheduleStep>(Math.Max(0, variantCount * repeats));
        for (var repeat = 0; repeat < repeats; repeat++)
        {
            for (var i = 0; i < variantCount; i++)
            {
                schedule.Add(new PerfExperimentScheduleStep(repeat, repeat % 2 == 0 ? i : variantCount - 1 - i));
            }
        }

        return schedule;
    }

    public static async Task<PerfExperimentResult> RunAsync(
        PerfExperimentSpec spec,
        Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> send,
        PerfExperimentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(send);
        options ??= new PerfExperimentOptions();

        var clock = Stopwatch.StartNew();
        var startedUtc = DateTimeOffset.UtcNow;
        var warnings = new List<string>();
        var runs = new List<PerfExperimentRun>();
        var samples = new List<PerfRunSamples>();
        var session = new PerfExperimentSession(new PerfAppLink(send), spec, options);
        var settings = new PerfExperimentSettings(
            spec.Name,
            spec.Description,
            spec.WarmupSeconds,
            spec.MeasureSeconds,
            spec.Repeats,
            spec.Recording,
            spec.VerdictStatistic,
            spec.Variants);
        var untouched = new PerfExperimentRestore(true, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

        PerfExperimentResult Finish(PerfExperimentStatus status, string? error, IReadOnlyList<string> problems)
            => new(
                status,
                settings,
                error,
                problems,
                warnings,
                runs,
                Array.Empty<PerfExperimentMetric>(),
                untouched,
                startedUtc.ToString("O", CultureInfo.InvariantCulture),
                Math.Round(clock.Elapsed.TotalSeconds, 1),
                null);

        // Preflight only reads. Whatever happens here, nothing has been changed yet, so there is nothing to restore.
        IReadOnlyList<string> preflightProblems;
        try
        {
            preflightProblems = await session.PreflightAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Finish(PerfExperimentStatus.Canceled, "Canceled before the experiment started; nothing was changed.", Array.Empty<string>());
        }
        catch (Exception ex)
        {
            return Finish(PerfExperimentStatus.Failed, $"Could not read the app before starting; nothing was changed: {ex.Message}", Array.Empty<string>());
        }

        if (preflightProblems.Count > 0)
        {
            return Finish(PerfExperimentStatus.Rejected, null, preflightProblems);
        }

        // The lease comes before the first change: a second experiment is turned away here with nothing changed.
        string? leaseProblem;
        try
        {
            leaseProblem = await session.AcquireLeaseAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await session.ReleaseUnconfirmedLeaseAsync().ConfigureAwait(false);
            return Finish(PerfExperimentStatus.Canceled, "Canceled before the experiment started; nothing was changed.", Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await session.ReleaseUnconfirmedLeaseAsync().ConfigureAwait(false);
            return Finish(PerfExperimentStatus.Failed, $"Could not take the experiment lease; nothing was changed: {ex.Message}", Array.Empty<string>());
        }

        if (leaseProblem is not null)
        {
            return Finish(PerfExperimentStatus.Rejected, null, new[] { leaseProblem });
        }

        var artifacts = new ArtifactWriter(options, spec.Name, startedUtc, warnings);
        var schedule = BuildSchedule(spec.Variants.Count, spec.Repeats);
        var status = PerfExperimentStatus.Completed;
        string? error = null;
        var label = "before the first run";
        options.Progress?.Invoke(string.Create(
            CultureInfo.InvariantCulture,
            $"Experiment '{spec.Name}': {schedule.Count} runs ({spec.Variants.Count} variants x {spec.Repeats} repeats), about {schedule.Count * (spec.WarmupSeconds + spec.MeasureSeconds)} s of warmup and measurement plus restarts."));

        try
        {
            for (var i = 0; i < schedule.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var variant = spec.Variants[schedule[i].Variant];
                var repeat = schedule[i].Repeat + 1;
                label = $"variant {variant.Name} run {repeat} (run {i + 1} of {schedule.Count})";
                options.Progress?.Invoke("Starting " + label);

                var outcome = await session.RunOnceAsync(schedule[i].Variant, repeat, i + 1, cancellationToken).ConfigureAwait(false);
                var runSamples = outcome.Report is { } report ? PerfExperimentStatistics.ExtractSamples(report) : Array.Empty<PerfMetricSample>();
                if (!runSamples.Any(sample => sample.Kind == PerfExperimentStatistics.HistogramKind))
                {
                    warnings.Add($"Variant {variant.Name} run {repeat} returned a window report with no metrics; the pipeline recorded nothing in the window.");
                }

                samples.Add(new PerfRunSamples(schedule[i].Variant, repeat, runSamples));
                var reportFile = outcome.Report is { } data ? artifacts.WriteRunReport(i + 1, variant.Name, repeat, data) : null;
                runs.Add(new PerfExperimentRun(variant.Name, repeat, outcome.Window, reportFile, PerfMath.Round(outcome.DurationMs)));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status = PerfExperimentStatus.Canceled;
            error = $"Canceled during {label}.";
        }
        catch (Exception ex)
        {
            status = PerfExperimentStatus.Failed;
            error = $"{ex.Message} (during {label})";
        }

        options.Progress?.Invoke("Restoring the app's pre-experiment state");
        var restore = await session.RestoreAsync().ConfigureAwait(false);

        if (status != PerfExperimentStatus.Completed)
        {
            warnings.Add($"The experiment did not finish: statistics cover only the {runs.Count} of {schedule.Count} runs that did, so read the verdicts with that in mind.");
        }

        var metrics = PerfExperimentStatistics.Compare(spec.Variants, samples, spec.Metrics, spec.VerdictStatistic, warnings);
        var result = new PerfExperimentResult(
            status,
            settings,
            error,
            Array.Empty<string>(),
            warnings,
            runs,
            metrics,
            restore,
            startedUtc.ToString("O", CultureInfo.InvariantCulture),
            Math.Round(clock.Elapsed.TotalSeconds, 1),
            artifacts.Directory);

        artifacts.WriteResult(spec, result);
        return result with { Warnings = warnings.ToList(), OutputDirectory = artifacts.Directory };
    }

    // Artifacts are a convenience for the next reader. Any failure to write them is one warning and the experiment's
    // status, statistics and restore are unaffected.
    private sealed class ArtifactWriter
    {
        private readonly PerfExperimentOptions _options;
        private readonly string _name;
        private readonly DateTimeOffset _started;
        private readonly List<string> _warnings;
        private bool _failed;
        private bool _created;

        public ArtifactWriter(PerfExperimentOptions options, string name, DateTimeOffset started, List<string> warnings)
        {
            _options = options;
            _name = name;
            _started = started;
            _warnings = warnings;
        }

        public string? Directory { get; private set; }

        public string? WriteRunReport(int runNumber, string variant, int repeat, JsonElement report)
        {
            if (!EnsureDirectory())
            {
                return null;
            }

            var relative = string.Create(CultureInfo.InvariantCulture, $"reports/{runNumber:00}-{Slug(variant, 32)}-r{repeat}.json");
            return TryWrite(relative, JsonSerializer.Serialize(report, IndentedJson)) ? relative : null;
        }

        public void WriteResult(PerfExperimentSpec spec, PerfExperimentResult result)
        {
            if (!EnsureDirectory())
            {
                return;
            }

            TryWrite("spec.json", spec.ToJson());
            TryWrite("result.json", result.ToJson());
            TryWrite("summary.md", result.ToMarkdown());
        }

        private bool EnsureDirectory()
        {
            if (!_options.WriteArtifacts || _failed)
            {
                return false;
            }

            if (_created)
            {
                return true;
            }

            var path = _options.OutputDirectory
                ?? Path.Combine(
                    "artifacts",
                    "perf-experiments",
                    $"{_started.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{Slug(_name, 40)}");
            try
            {
                path = _options.OutputDirectory is null ? Path.GetFullPath(path) : path;
                System.IO.Directory.CreateDirectory(path);
                System.IO.Directory.CreateDirectory(Path.Combine(path, "reports"));
            }
            catch (Exception ex) when (IsFileSystemFailure(ex))
            {
                _failed = true;
                _warnings.Add($"Artifacts were not written: could not create '{path}': {ex.Message}");
                return false;
            }

            Directory = path;
            _created = true;
            return true;
        }

        private bool TryWrite(string relativePath, string content)
        {
            try
            {
                var target = Path.Combine(Directory!, relativePath.Replace('/', Path.DirectorySeparatorChar));
                File.WriteAllText(target, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return true;
            }
            catch (Exception ex) when (IsFileSystemFailure(ex))
            {
                if (!_failed)
                {
                    _failed = true;
                    _warnings.Add($"Some artifacts were not written ('{relativePath}'): {ex.Message}");
                }

                return false;
            }
        }

        private static bool IsFileSystemFailure(Exception ex)
            => ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Security.SecurityException;

        private static string Slug(string text, int maxLength)
        {
            var slug = new StringBuilder();
            foreach (var ch in text)
            {
                slug.Append(char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_');
            }

            var value = slug.ToString().Trim('_');
            if (value.Length > maxLength)
            {
                value = value[..maxLength];
            }

            return value.Length == 0 ? "experiment" : value;
        }
    }
}
