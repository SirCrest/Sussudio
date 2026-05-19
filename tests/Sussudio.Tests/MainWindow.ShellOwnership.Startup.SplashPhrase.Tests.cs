using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task SplashLoadingPhrases_LiveInController()
    {
        var launchEntranceSplashText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var launchAdapterText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs").Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseCatalog.cs").Replace("\r\n", "\n");
        var pacingPolicyText = ReadRepoFile("Sussudio/Controllers/Launch/Splash/SplashLoadingPhrasePacingPolicy.cs").Replace("\r\n", "\n");

        AssertContains(launchAdapterText, "private SplashLoadingPhraseController _splashLoadingPhraseController = null!;");
        AssertContains(launchAdapterText, "private void InitializeSplashLoadingPhraseController()");
        AssertContains(launchAdapterText, "SplashLoadingTextA = SplashLoadingTextA,");
        AssertContains(launchAdapterText, "SplashLoadingTransformB = SplashLoadingTransformB,");
        AssertContains(launchAdapterText, "=> _splashLoadingPhraseController.Start();");
        AssertContains(launchAdapterText, "=> _splashLoadingPhraseController.Stop();");
        AssertContains(mainWindowText, "InitializeSplashLoadingPhraseController();");
        AssertContains(launchEntranceSplashText, "_context.StartSplashLoadingPhrases();");
        AssertContains(launchEntranceSplashText, "_context.StopSplashLoadingPhrases();");
        AssertContains(controllerText, "internal sealed class SplashLoadingPhraseController");
        AssertContains(controllerText, "private DispatcherTimer? _splashPhraseTimer;");
        AssertContains(controllerText, "SplashLoadingPhraseCatalog.Load()");
        AssertContains(controllerText, "private readonly SplashLoadingPhrasePacingPolicy _pacingPolicy = new();");
        AssertContains(controllerText, "_pacingPolicy.Reset();");
        AssertContains(controllerText, "Interval = _pacingPolicy.NextInterval()");
        AssertContains(controllerText, "private void CyclePhrase()");
        AssertContains(controllerText, "storyboard.Begin();");
        AssertContains(pacingPolicyText, "internal sealed class SplashLoadingPhrasePacingPolicy");
        AssertContains(pacingPolicyText, "internal enum SplashLoadingPhrasePaceMode");
        AssertContains(pacingPolicyText, "public TimeSpan NextInterval()");
        AssertContains(pacingPolicyText, "internal TimeSpan NextInterval(Func<double> nextDouble, Func<int, int, int> nextInt)");
        AssertContains(catalogText, "internal static class SplashLoadingPhraseCatalog");
        AssertContains(catalogText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertContains(catalogText, "public static string[] Load()");
        AssertContains(catalogText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");
        AssertContains(catalogText, "if (line.StartsWith(\"##\"))");
        AssertContains(catalogText, "if (line.StartsWith('#')) continue;");
        AssertContains(catalogText, "if (line.StartsWith(\"<!--\")) continue;");
        AssertContains(catalogText, "line = line[2..].Trim();");
        AssertContains(catalogText, "while (line.EndsWith('.'))");
        AssertContains(catalogText, "_cachedSplashPhrases = DefaultSplashLoadingPhrases;");
        AssertDoesNotContain(controllerText, "private static readonly string[] DefaultSplashLoadingPhrases");
        AssertDoesNotContain(controllerText, "Path.Combine(AppContext.BaseDirectory, \"SplashPhrases.md\")");
        AssertDoesNotContain(controllerText, "private TimeSpan NextSplashPhraseInterval()");
        AssertDoesNotContain(controllerText, "Random.Shared.NextDouble()");
        AssertDoesNotContain(controllerText, "SplashLoadingPhrasePaceMode");
        AssertDoesNotContain(controllerText, "280, 420");
        AssertDoesNotContain(controllerText, "380, 900");
        AssertDoesNotContain(controllerText, "900, 1500");
        AssertDoesNotContain(controllerText, "1500, 2500");
        AssertDoesNotContain(controllerText, "nextInt(280, 420)");
        AssertDoesNotContain(controllerText, "nextInt(380, 900)");
        AssertDoesNotContain(controllerText, "nextInt(900, 1500)");
        AssertDoesNotContain(controllerText, "nextInt(1500, 2500)");

        return Task.CompletedTask;
    }

    private static Task SplashLoadingPhrasePacingPolicy_PreservesIntervalBands()
    {
        var policyType = RequireType("Sussudio.Controllers.SplashLoadingPhrasePacingPolicy");
        var policy = Activator.CreateInstance(policyType, nonPublic: true)
            ?? throw new InvalidOperationException("Failed to create SplashLoadingPhrasePacingPolicy.");
        var nextInterval = policyType.GetMethod(
                "NextInterval",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Func<double>), typeof(Func<int, int, int>) },
                modifiers: null)
            ?? throw new InvalidOperationException("SplashLoadingPhrasePacingPolicy.NextInterval test seam was not found.");
        var reset = policyType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("SplashLoadingPhrasePacingPolicy.Reset was not found.");

        AssertEqual(
            TimeSpan.FromMilliseconds(319),
            InvokePolicy(policy, nextInterval, new[] { 0.10d }, (2, 6, 2), (280, 420, 319)),
            "burst first interval uses burst tick and interval ranges");
        AssertEqual(
            TimeSpan.FromMilliseconds(318),
            InvokePolicy(policy, nextInterval, Array.Empty<double>(), (280, 420, 318)),
            "burst keeps current mode while tick budget remains");
        AssertEqual(
            TimeSpan.FromMilliseconds(700),
            InvokePolicy(policy, nextInterval, new[] { 0.20d }, (1, 4, 1), (380, 900, 700)),
            "normal lower boundary uses normal ranges");
        AssertEqual(
            TimeSpan.FromMilliseconds(1200),
            InvokePolicy(policy, nextInterval, new[] { 0.70d }, (900, 1500, 1200)),
            "stuck lower boundary uses stuck interval range");
        AssertEqual(
            TimeSpan.FromMilliseconds(2000),
            InvokePolicy(policy, nextInterval, new[] { 0.90d }, (1500, 2500, 2000)),
            "long-stuck lower boundary uses long-stuck interval range");

        _ = InvokePolicy(policy, nextInterval, new[] { 0.05d }, (2, 6, 5), (280, 420, 300));
        reset.Invoke(policy, null);
        AssertEqual(
            TimeSpan.FromMilliseconds(1800),
            InvokePolicy(policy, nextInterval, new[] { 0.95d }, (1500, 2500, 1800)),
            "reset forces the next interval to choose a fresh mode");

        return Task.CompletedTask;
    }

    private static TimeSpan InvokePolicy(
        object policy,
        MethodInfo nextInterval,
        double[] rolls,
        params (int Min, int Max, int Value)[] integerResponses)
    {
        var rollQueue = new Queue<double>(rolls);
        var integerQueue = new Queue<(int Min, int Max, int Value)>(integerResponses);

        Func<double> nextDouble = () =>
        {
            if (rollQueue.Count == 0)
            {
                throw new InvalidOperationException("Policy requested an unexpected random roll.");
            }

            return rollQueue.Dequeue();
        };
        Func<int, int, int> nextInt = (min, max) =>
        {
            if (integerQueue.Count == 0)
            {
                throw new InvalidOperationException($"Policy requested unexpected integer range {min}..{max}.");
            }

            var expected = integerQueue.Dequeue();
            AssertEqual(expected.Min, min, "policy integer range minimum");
            AssertEqual(expected.Max, max, "policy integer range maximum");
            return expected.Value;
        };

        var result = (TimeSpan)(nextInterval.Invoke(policy, new object[] { nextDouble, nextInt })
                                ?? throw new InvalidOperationException("Policy returned null interval."));
        AssertEqual(0, rollQueue.Count, "unused policy random rolls");
        AssertEqual(0, integerQueue.Count, "unused policy integer responses");
        return result;
    }
}
