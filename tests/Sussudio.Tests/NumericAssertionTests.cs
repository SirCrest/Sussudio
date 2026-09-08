using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class NumericAssertionTests
{
    private static readonly Action<double, double, double, string> AssertNearlyEqual =
        (typeof(global::Program).GetMethod("AssertNearlyEqual", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Shared approximate-equality assertion was not found."))
        .CreateDelegate<Action<double, double, double, string>>();

    [Theory]
    [InlineData(10.0, 10.0, 0.0)]
    [InlineData(10.0, 10.5, 0.5)]
    [InlineData(10.0, 9.5, 0.5)]
    public void ValuesAtOrInsideTheToleranceAreAccepted(double expected, double actual, double tolerance)
        => AssertNearlyEqual(expected, actual, tolerance, "numeric boundary");

    [Theory]
    [InlineData(10.0, 10.75, 0.5)]
    [InlineData(0.0, double.NaN, 0.1)]
    [InlineData(double.NaN, 0.0, 0.1)]
    [InlineData(double.NaN, double.NaN, 0.1)]
    [InlineData(0.0, double.PositiveInfinity, 0.1)]
    [InlineData(0.0, double.NegativeInfinity, 0.1)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 0.1)]
    [InlineData(0.0, 0.0, double.NaN)]
    public void MismatchesAndNonfiniteDistancesAreRejected(double expected, double actual, double tolerance)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => AssertNearlyEqual(expected, actual, tolerance, "numeric failure"));
        Assert.Contains("numeric failure", error.Message);
    }
}
