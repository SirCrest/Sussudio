using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

// Lightweight reflection-based slice for the small pure helpers under
// Sussudio.Services.Runtime. Same load-from-staged-dll
// approach as XUnit.RecordingContractsTests.cs — the test project targets net8.0
// so we resolve types through the Sussudio.dll built for net8.0-windows.
public class RuntimeHelpersTests
{
    private const string AtomicMaxType = "Sussudio.Services.Runtime.AtomicMax";
    private const string TelemetryAgeHelperType = "Sussudio.Services.Runtime.TelemetryAgeHelper";
    private const string EnvironmentHelpersType = "Sussudio.Services.Runtime.EnvironmentHelpers";
    private const string RingBufferHelpersType = "Sussudio.Services.Runtime.RingBufferHelpers";

    [Fact]
    public void AtomicMax_Int_UpdatesWhenCandidateIsGreater()
    {
        var method = ResolveAtomicMaxInt();
        var args = new object[] { 3, 7 };
        method.Invoke(null, args);
        Assert.Equal(7, (int)args[0]);
    }

    [Fact]
    public void AtomicMax_Int_NoOpWhenCandidateIsLessOrEqual()
    {
        var method = ResolveAtomicMaxInt();

        var lessArgs = new object[] { 10, 3 };
        method.Invoke(null, lessArgs);
        Assert.Equal(10, (int)lessArgs[0]);

        var equalArgs = new object[] { 10, 10 };
        method.Invoke(null, equalArgs);
        Assert.Equal(10, (int)equalArgs[0]);
    }

    [Fact]
    public void AtomicMax_Long_UpdatesWhenCandidateIsGreater()
    {
        var method = ResolveAtomicMaxLong();
        var args = new object[] { 3L, 7L };
        method.Invoke(null, args);
        Assert.Equal(7L, (long)args[0]);

        var noOpArgs = new object[] { 100L, 50L };
        method.Invoke(null, noOpArgs);
        Assert.Equal(100L, (long)noOpArgs[0]);
    }

    [Fact]
    public void TelemetryAgeHelper_ReturnsNullForNullTimestamp()
    {
        var method = ResolveTimestampOverload();
        var now = DateTimeOffset.UtcNow;
        var result = method.Invoke(null, new object?[] { null, now });
        Assert.Null(result);
    }

    [Fact]
    public void TelemetryAgeHelper_FloorsPositiveAge()
    {
        var method = ResolveTimestampOverload();
        var now = new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero);
        var past = now.AddSeconds(-12.7);
        var result = (int?)method.Invoke(null, new object?[] { past, now });
        Assert.Equal(12, result);
    }

    [Fact]
    public void TelemetryAgeHelper_ClampsNegativeAgeToZero()
    {
        var method = ResolveTimestampOverload();
        var now = new DateTimeOffset(2026, 5, 11, 12, 0, 0, TimeSpan.Zero);
        var future = now.AddSeconds(5);
        var result = (int?)method.Invoke(null, new object?[] { future, now });
        Assert.Equal(0, result);
    }

    [Fact]
    public void TelemetryAgeHelper_ReportedAgeShortCircuitsAndClamps()
    {
        var method = ResolveReportedOverload();
        var now = DateTimeOffset.UtcNow;
        var result = (int?)method.Invoke(null, new object?[] { 42, now.AddSeconds(-1), now });
        Assert.Equal(42, result);

        var negativeReported = (int?)method.Invoke(null, new object?[] { -5, null, now });
        Assert.Equal(0, negativeReported);

        var fallthrough = (int?)method.Invoke(null, new object?[] { null, now.AddSeconds(-3), now });
        Assert.Equal(3, fallthrough);
    }

    [Fact]
    public void EnvironmentHelpers_GetIntFromEnv_ReturnsDefaultWhenUnset()
    {
        var method = ResolveGetIntFromEnv();
        var name = NewEnvVarName("INT_UNSET");
        using var _ = EnvVarScope.Push(name, null);
        var result = method.Invoke(null, new object[] { name, 50, 0, 100 });
        Assert.Equal(50, (int)result!);
    }

    [Fact]
    public void EnvironmentHelpers_GetIntFromEnv_ClampsToRange()
    {
        var method = ResolveGetIntFromEnv();
        var name = NewEnvVarName("INT_CLAMP");

        using (EnvVarScope.Push(name, "200"))
        {
            Assert.Equal(100, (int)method.Invoke(null, new object[] { name, 50, 0, 100 })!);
        }

        using (EnvVarScope.Push(name, "-50"))
        {
            Assert.Equal(0, (int)method.Invoke(null, new object[] { name, 50, 0, 100 })!);
        }

        using (EnvVarScope.Push(name, "75"))
        {
            Assert.Equal(75, (int)method.Invoke(null, new object[] { name, 50, 0, 100 })!);
        }
    }

    [Fact]
    public void EnvironmentHelpers_TryGetBoolFromEnv_RecognizesTextAndIntegerForms()
    {
        var method = ResolveTryGetBoolFromEnv();
        var name = NewEnvVarName("BOOL");

        AssertBoolEnv(method, name, "true", true, true);
        AssertBoolEnv(method, name, "False", true, false);
        AssertBoolEnv(method, name, "1", true, true);
        AssertBoolEnv(method, name, "0", true, false);
        AssertBoolEnv(method, name, "not-a-bool", false, false);
        AssertBoolEnv(method, name, null, false, false);
    }

    [Fact]
    public void RingBufferHelpers_Copy_ReturnsLatestSamplesInOrder()
    {
        var method = ResolveCopyDouble();
        // Window of size 4 with three samples added: [1.0, 2.0, 3.0, _]
        // count=3, index=3 (next write position).
        var window = new[] { 1.0, 2.0, 3.0, 0.0 };
        var result = (double[])method.Invoke(null, new object?[] { window, 3, 3, null })!;
        Assert.Equal(new[] { 1.0, 2.0, 3.0 }, result);
    }

    [Fact]
    public void RingBufferHelpers_Copy_HandlesRingWraparound()
    {
        var method = ResolveCopyDouble();
        // Window of size 4 fully populated, then overwritten at slot 0 with 5.0.
        // Logical order: [2.0, 3.0, 4.0, 5.0]. count=4, index=1 (next write).
        var window = new[] { 5.0, 2.0, 3.0, 4.0 };
        var result = (double[])method.Invoke(null, new object?[] { window, 4, 1, null })!;
        Assert.Equal(new[] { 2.0, 3.0, 4.0, 5.0 }, result);
    }

    [Fact]
    public void RingBufferHelpers_Copy_MaxCountCapsResult()
    {
        var method = ResolveCopyDouble();
        var window = new[] { 1.0, 2.0, 3.0, 4.0 };
        var result = (double[])method.Invoke(null, new object?[] { window, 4, 0, 2 })!;
        Assert.Equal(new[] { 3.0, 4.0 }, result);

        var empty = (double[])method.Invoke(null, new object?[] { window, 4, 0, 0 })!;
        Assert.Empty(empty);
    }

    private static MethodInfo ResolveStatic(string typeName, string methodName, Type[] signature)
    {
        var type = SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;
        return type.GetMethod(methodName, ReflectionFlags.Static, signature)!;
    }

    private static MethodInfo ResolveAtomicMaxInt()
        => ResolveStatic(AtomicMaxType, "Update", new[] { typeof(int).MakeByRefType(), typeof(int) });

    private static MethodInfo ResolveAtomicMaxLong()
        => ResolveStatic(AtomicMaxType, "Update", new[] { typeof(long).MakeByRefType(), typeof(long) });

    private static MethodInfo ResolveTimestampOverload()
        => ResolveStatic(TelemetryAgeHelperType, "ComputeAgeSeconds", new[] { typeof(DateTimeOffset?), typeof(DateTimeOffset) });

    private static MethodInfo ResolveReportedOverload()
        => ResolveStatic(TelemetryAgeHelperType, "ComputeAgeSeconds", new[] { typeof(int?), typeof(DateTimeOffset?), typeof(DateTimeOffset) });

    private static MethodInfo ResolveGetIntFromEnv()
        => ResolveStatic(EnvironmentHelpersType, "GetIntFromEnv", new[] { typeof(string), typeof(int), typeof(int), typeof(int) });

    private static MethodInfo ResolveTryGetBoolFromEnv()
        => ResolveStatic(EnvironmentHelpersType, "TryGetBoolFromEnv", new[] { typeof(string), typeof(bool).MakeByRefType() });

    private static MethodInfo ResolveCopyDouble()
        => ResolveStatic(RingBufferHelpersType, "Copy", new[] { typeof(double[]), typeof(int), typeof(int), typeof(int?) });

    private static void AssertBoolEnv(MethodInfo method, string name, string? raw, bool expectedReturn, bool expectedValue)
    {
        using var _ = EnvVarScope.Push(name, raw);
        var args = new object?[] { name, false };
        var ok = (bool)method.Invoke(null, args)!;
        Assert.Equal(expectedReturn, ok);
        Assert.Equal(expectedValue, (bool)args[1]!);
    }

    private static string NewEnvVarName(string suffix)
        => $"SUSSUDIO_TEST_{suffix}_{Guid.NewGuid():N}";
}
