using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of runtime tuning knobs (docs/architecture/performance-telemetry.md sections 2.5 and 5):
// unknown names, wrong types and out-of-range values are rejected (never clamped), reset restores the launch value
// including "unset", and the apply scope is reported. The store runs against a fake environment so these tests
// never touch the real process environment.
public sealed class TuningKnobStoreTests
{
    private sealed class FakeEnvironment : ITuningKnobEnvironment
    {
        public Dictionary<string, string?> Values { get; } = new(StringComparer.Ordinal);

        public int WriteCount { get; private set; }

        public string? Get(string name) => Values.TryGetValue(name, out var value) ? value : null;

        public void Set(string name, string? value)
        {
            WriteCount++;
            if (value is null)
            {
                Values.Remove(name);
            }
            else
            {
                Values[name] = value;
            }
        }
    }

    private static readonly TuningKnobDefinition IntKnob = new(
        "SUSSUDIO_TEST_INT", TuningKnobType.Int, "4", 1, 8, null, TuningKnobApply.PreviewRestart, "Test.cs", "int knob");

    private static readonly TuningKnobDefinition DoubleKnob = new(
        "SUSSUDIO_TEST_DOUBLE", TuningKnobType.Double, "0.25", 0.0, 4.0, null, TuningKnobApply.Live, "Test.cs", "double knob");

    private static readonly TuningKnobDefinition BoolKnob = new(
        "SUSSUDIO_TEST_BOOL", TuningKnobType.Bool, "1", 0, 1, null, TuningKnobApply.RecordingStart, "Test.cs", "bool knob");

    private static readonly TuningKnobDefinition ChoiceKnob = new(
        "SUSSUDIO_TEST_CHOICE", TuningKnobType.Choice, "Playback", null, null, new[] { "Playback", "Pro Audio" },
        TuningKnobApply.FlashbackRestart, "Test.cs", "choice knob");

    private static readonly TuningKnobDefinition AppKnob = new(
        "SUSSUDIO_TEST_APP", TuningKnobType.Int, "1000", 100, 10000, null, TuningKnobApply.AppRestart, "Test.cs", "app knob");

    private static (TuningKnobStore Store, FakeEnvironment Env, List<string> Log) Create(
        Action<FakeEnvironment>? seed = null)
    {
        var env = new FakeEnvironment();
        seed?.Invoke(env);
        var log = new List<string>();
        var store = new TuningKnobStore(
            new[] { IntKnob, DoubleKnob, BoolKnob, ChoiceKnob, AppKnob },
            env,
            log.Add);
        return (store, env, log);
    }

    [Fact]
    public void UnknownKnobIsRejectedWithoutTouchingTheEnvironmentOrLog()
    {
        var (store, env, log) = Create();
        var result = store.Set("SUSSUDIO_NOT_A_KNOB", "1");
        Assert.False(result.Success);
        Assert.Equal(TuningKnobFailure.UnknownKnob, result.Failure);
        Assert.Equal(0, env.WriteCount);
        Assert.Empty(log);

        Assert.Equal(TuningKnobFailure.UnknownKnob, store.Reset("nope").Failure);
        Assert.Equal(TuningKnobFailure.UnknownKnob, store.Set("", "1").Failure);
        Assert.Equal(TuningKnobFailure.UnknownKnob, store.Set(null!, "1").Failure);
    }

    [Theory]
    [InlineData("SUSSUDIO_TEST_INT", "abc")]
    [InlineData("SUSSUDIO_TEST_INT", "1.5")]
    [InlineData("SUSSUDIO_TEST_INT", "0x4")]
    [InlineData("SUSSUDIO_TEST_INT", "4 4")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "x")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "NaN")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "Infinity")]
    [InlineData("SUSSUDIO_TEST_BOOL", "maybe")]
    [InlineData("SUSSUDIO_TEST_BOOL", "2")]
    [InlineData("SUSSUDIO_TEST_CHOICE", "Bogus")]
    public void WrongTypeIsRejectedAndLeavesTheEnvironmentUntouched(string name, string value)
    {
        var (store, env, log) = Create();
        var result = store.Set(name, value);
        Assert.False(result.Success);
        Assert.Equal(TuningKnobFailure.WrongType, result.Failure);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        Assert.Equal(0, env.WriteCount);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("SUSSUDIO_TEST_INT", "0")]
    [InlineData("SUSSUDIO_TEST_INT", "9")]
    [InlineData("SUSSUDIO_TEST_INT", "-1")]
    [InlineData("SUSSUDIO_TEST_INT", "99999999999999999999")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "-0.01")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "4.01")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "1e9")]
    public void OutOfRangeIsRejectedNotClamped(string name, string value)
    {
        var (store, env, log) = Create();
        var result = store.Set(name, value);
        Assert.False(result.Success);
        Assert.Equal(TuningKnobFailure.OutOfRange, result.Failure);
        Assert.Contains("[", result.Error);
        Assert.Equal(0, env.WriteCount);
        Assert.Null(env.Get(name));
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("SUSSUDIO_TEST_INT", "1", "1")]
    [InlineData("SUSSUDIO_TEST_INT", "8", "8")]
    [InlineData("SUSSUDIO_TEST_INT", " 5 ", "5")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "0", "0")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "4", "4")]
    [InlineData("SUSSUDIO_TEST_DOUBLE", "0.5", "0.5")]
    [InlineData("SUSSUDIO_TEST_BOOL", "true", "1")]
    [InlineData("SUSSUDIO_TEST_BOOL", "FALSE", "0")]
    [InlineData("SUSSUDIO_TEST_BOOL", "1", "1")]
    [InlineData("SUSSUDIO_TEST_CHOICE", "pro audio", "Pro Audio")]
    public void ValidValuesAreWrittenInCanonicalForm(string name, string value, string expectedWritten)
    {
        var (store, env, _) = Create();
        var result = store.Set(name, value);
        Assert.True(result.Success, result.Error);
        Assert.Equal(expectedWritten, env.Get(name));
        Assert.Equal(expectedWritten, result.New);
    }

    [Fact]
    public void MissingValueIsRejectedAndPointsAtReset()
    {
        var (store, env, _) = Create();
        var result = store.Set(IntKnob.Name, null);
        Assert.False(result.Success);
        Assert.Equal(TuningKnobFailure.MissingValue, result.Failure);
        Assert.Contains("reset", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, env.WriteCount);
        Assert.Equal(TuningKnobFailure.MissingValue, store.Set(IntKnob.Name, "  ").Failure);
    }

    [Fact]
    public void ResetRestoresAnUnsetLaunchValueByRemovingTheVariable()
    {
        var (store, env, _) = Create();
        Assert.True(store.Set(IntKnob.Name, "7").Success);
        Assert.Equal("7", env.Get(IntKnob.Name));

        var reset = store.Reset(IntKnob.Name);
        Assert.True(reset.Success);
        Assert.Equal("7", reset.Old);
        Assert.Null(reset.New);
        Assert.False(env.Values.ContainsKey(IntKnob.Name));
    }

    [Fact]
    public void ResetRestoresANonEmptyLaunchValueAfterSeveralChanges()
    {
        var (store, env, _) = Create(e => e.Values[IntKnob.Name] = "3");
        Assert.True(store.Set(IntKnob.Name, "5").Success);
        Assert.True(store.Set(IntKnob.Name, "6").Success);
        Assert.Equal("6", env.Get(IntKnob.Name));

        var reset = store.Reset(IntKnob.Name);
        Assert.True(reset.Success);
        Assert.Equal("3", reset.New);
        Assert.Equal("3", env.Get(IntKnob.Name));

        var state = store.List(IntKnob.Name).Single();
        Assert.Equal("3", state.Value);
        Assert.Equal("3", state.Launch);
        Assert.False(state.Overridden);
    }

    [Fact]
    public void LaunchValueIsCapturedBeforeTheFirstChangeAndNeverMoves()
    {
        var (store, env, _) = Create(e => e.Values[DoubleKnob.Name] = "0.75");
        Assert.True(store.Set(DoubleKnob.Name, "1.5").Success);

        // Even if something else rewrites the variable later, the launch value stays the value seen at first touch.
        env.Values[DoubleKnob.Name] = "2";
        Assert.True(store.Set(DoubleKnob.Name, "3").Success);
        Assert.Equal("0.75", store.List(DoubleKnob.Name).Single().Launch);
        Assert.True(store.Reset(DoubleKnob.Name).Success);
        Assert.Equal("0.75", env.Get(DoubleKnob.Name));
    }

    [Fact]
    public void ResettingAnUntouchedKnobIsASuccessfulNoOpThatKeepsTheLaunchValue()
    {
        var (store, env, _) = Create(e => e.Values[BoolKnob.Name] = "0");
        var reset = store.Reset(BoolKnob.Name);
        Assert.True(reset.Success);
        Assert.Equal("0", env.Get(BoolKnob.Name));
    }

    [Fact]
    public void ApplyScopeIsReportedForEveryScope()
    {
        var (store, _, _) = Create();
        Assert.Equal("preview-restart", store.Set(IntKnob.Name, "2").Apply);
        Assert.Equal("live", store.Set(DoubleKnob.Name, "1").Apply);
        Assert.Equal("recording-start", store.Set(BoolKnob.Name, "0").Apply);
        Assert.Equal("flashback-restart", store.Set(ChoiceKnob.Name, "Playback").Apply);
        Assert.Equal("app-restart", store.Set(AppKnob.Name, "500").Apply);
    }

    [Fact]
    public void NonLiveChangesSayWhatMustRestartAndLiveChangesDoNot()
    {
        var (store, _, _) = Create();
        var live = store.Set(DoubleKnob.Name, "1");
        Assert.True(string.IsNullOrEmpty(live.Note) || live.Note!.Contains("immediately", StringComparison.OrdinalIgnoreCase));

        var preview = store.Set(IntKnob.Name, "2");
        Assert.Contains("preview", preview.Note, StringComparison.OrdinalIgnoreCase);

        var app = store.Set(AppKnob.Name, "500");
        Assert.Contains("restart", app.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EverySuccessfulChangeLogsOneLineWithOldNewAndApply()
    {
        var (store, _, log) = Create(e => e.Values[IntKnob.Name] = "3");
        store.Set(IntKnob.Name, "5");
        store.Reset(IntKnob.Name);
        store.Set(DoubleKnob.Name, "1");
        store.Reset(DoubleKnob.Name);

        Assert.Equal(
            new[]
            {
                "TUNING_KNOB_SET name=SUSSUDIO_TEST_INT old=3 new=5 apply=preview-restart",
                "TUNING_KNOB_SET name=SUSSUDIO_TEST_INT old=5 new=3 apply=preview-restart",
                "TUNING_KNOB_SET name=SUSSUDIO_TEST_DOUBLE old=<unset> new=1 apply=live",
                "TUNING_KNOB_SET name=SUSSUDIO_TEST_DOUBLE old=1 new=<unset> apply=live",
            },
            log);
    }

    [Fact]
    public void ANoOpSetOrResetSucceedsButIsNotLogged()
    {
        var (store, _, log) = Create(e => e.Values[IntKnob.Name] = "3");

        // Resetting a knob that was never set, and setting a value the knob already has, change nothing.
        var resetUnset = store.Reset(DoubleKnob.Name);
        var setSame = store.Set(IntKnob.Name, "3");
        var resetUntouched = store.Reset(IntKnob.Name);

        Assert.True(resetUnset.Success);
        Assert.Null(resetUnset.Old);
        Assert.Null(resetUnset.New);
        Assert.True(setSame.Success);
        Assert.Equal("3", setSame.New);
        Assert.True(resetUntouched.Success);
        Assert.Empty(log);

        // A real change after the no-ops is still logged once.
        store.Set(IntKnob.Name, "5");
        Assert.Equal(new[] { "TUNING_KNOB_SET name=SUSSUDIO_TEST_INT old=3 new=5 apply=preview-restart" }, log);
    }

    [Fact]
    public void ListFiltersByCaseInsensitiveNameSubstringAndOverridesListsOnlyChangedKnobs()
    {
        var (store, _, _) = Create();
        Assert.Equal(5, store.List(null).Count);
        Assert.Equal(5, store.List("").Count);
        Assert.Single(store.List("test_double"));
        Assert.Empty(store.List("zzz"));

        Assert.Empty(store.Overrides());
        store.Set(IntKnob.Name, "2");
        var overrides = store.Overrides();
        Assert.Single(overrides);
        Assert.Equal(IntKnob.Name, overrides[0].Definition.Name);
        Assert.True(overrides[0].Overridden);
        Assert.Null(overrides[0].Launch);
        Assert.Equal("2", overrides[0].Value);
    }

    [Fact]
    public void ConcurrentSetsLeaveAValidValueAndNeverThrow()
    {
        var (store, env, _) = Create();
        Parallel.For(0, 400, i =>
        {
            var result = store.Set(IntKnob.Name, ((i % 8) + 1).ToString());
            Assert.True(result.Success);
        });

        Assert.InRange(int.Parse(env.Get(IntKnob.Name)!), 1, 8);
        Assert.True(store.Reset(IntKnob.Name).Success);
        Assert.Null(env.Get(IntKnob.Name));
    }

    [Fact]
    public void TheRealCatalogIsInternallyConsistent()
    {
        var names = TuningKnobCatalog.Knobs.Select(k => k.Name).ToList();
        Assert.True(names.Count >= 40);
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("SUSSUDIO_DIAGNOSTICS_POLL_MS", names);

        foreach (var knob in TuningKnobCatalog.Knobs)
        {
            Assert.StartsWith("SUSSUDIO_", knob.Name);
            Assert.False(string.IsNullOrWhiteSpace(knob.Description), knob.Name);
            Assert.False(string.IsNullOrWhiteSpace(knob.Owner), knob.Name);
            switch (knob.Type)
            {
                case TuningKnobType.Int:
                case TuningKnobType.Double:
                    Assert.NotNull(knob.Min);
                    Assert.NotNull(knob.Max);
                    Assert.True(knob.Min <= knob.Max, knob.Name);
                    break;
                case TuningKnobType.Choice:
                    Assert.NotEmpty(knob.Choices!);
                    break;
            }
        }

        foreach (var excluded in TuningKnobCatalog.Excluded)
        {
            Assert.DoesNotContain(excluded.Name, names);
            Assert.False(string.IsNullOrWhiteSpace(excluded.Reason), excluded.Name);
        }

        // The automation token is a secret; it must never be catalogued as a readable knob.
        Assert.DoesNotContain("SUSSUDIO_AUTOMATION_TOKEN", names);
    }
}
