using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class AtomicCounterTests
{
    private delegate bool Subtract(ref int target, int amount);
    private delegate bool Decrement(ref int target);

    [Theory]
    [InlineData(7, 3, 4, true)]
    [InlineData(3, 3, 0, true)]
    [InlineData(2, 3, 0, false)]
    [InlineData(0, 3, 0, false)]
    [InlineData(int.MaxValue, int.MaxValue, 0, true)]
    [InlineData(int.MaxValue - 1, int.MaxValue, 0, false)]
    public void SubtractionSaturatesAndReportsWhetherTheFullAmountWasAvailable(int initial, int amount, int remaining, bool expected)
    {
        var subtract = Resolve("TrySubtract").CreateDelegate<Subtract>();
        var target = initial;
        Assert.Equal(expected, subtract(ref target, amount));
        Assert.Equal(remaining, target);
    }

    [Fact]
    public void DecrementSucceedsExactlyUntilTheCounterIsEmpty()
    {
        var decrement = Resolve("TryDecrement").CreateDelegate<Decrement>();
        var target = 1;
        Assert.True(decrement(ref target));
        Assert.Equal(0, target);
        Assert.False(decrement(ref target));
        Assert.Equal(0, target);
    }

    [Fact]
    public void ConcurrentSubtractionsDoNotCountAPartialFinalSubtractionAsSuccess()
    {
        var subtract = Resolve("TrySubtract").CreateDelegate<Subtract>();
        var target = 1001;
        var successes = 0;
        Parallel.For(0, 1000, _ =>
        {
            if (subtract(ref target, 3)) Interlocked.Increment(ref successes);
        });

        Assert.Equal(0, target);
        Assert.Equal(333, successes);
    }

    private static MethodInfo Resolve(string method)
        => SussudioAssembly.Load().GetType("Sussudio.Services.Runtime.AtomicCounter", throwOnError: true)!
            .GetMethod(method, BindingFlags.Static | BindingFlags.Public)!;
}
