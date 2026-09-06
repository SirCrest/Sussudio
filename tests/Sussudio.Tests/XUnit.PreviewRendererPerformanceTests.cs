using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class PreviewRendererPerformanceTests
{
    [Fact]
    public void InputViews_DistinguishTextureAndSubresource()
    {
        var cache = CreateCache();
        var first = new OwnedResource();
        var secondSlice = new OwnedResource();
        var secondTexture = new OwnedResource();
        cache.Add(new IntPtr(1), 0, first);
        cache.Add(new IntPtr(1), 1, secondSlice);
        cache.Add(new IntPtr(2), 0, secondTexture);

        Assert.True(cache.TryGet(new IntPtr(1), 0, out var found));
        Assert.Same(first, found);
        Assert.True(cache.TryGet(new IntPtr(1), 1, out found));
        Assert.Same(secondSlice, found);
        Assert.True(cache.TryGet(new IntPtr(2), 0, out found));
        Assert.Same(secondTexture, found);
        Assert.False(cache.TryGet(new IntPtr(3), 0, out _));
        Assert.Equal(3, cache.Count());
        cache.Clear();
    }

    [Fact]
    public void InputViews_StayBoundedAndEvictLeastRecentlyUsedPair()
    {
        var cache = CreateCache();
        var resources = new OwnedResource[9];
        for (var i = 0; i < 8; i++)
        {
            resources[i] = new OwnedResource();
            cache.Add(new IntPtr(i + 1), 0, resources[i]);
        }

        Assert.True(cache.TryGet(new IntPtr(1), 0, out _));
        resources[8] = new OwnedResource();
        cache.Add(new IntPtr(9), 0, resources[8]);

        Assert.Equal(8, cache.Count());
        Assert.Equal(0, resources[0].DisposeCount);
        Assert.Equal(1, resources[1].DisposeCount);
        Assert.False(cache.TryGet(new IntPtr(2), 0, out _));
        Assert.True(cache.TryGet(new IntPtr(9), 0, out var added));
        Assert.Same(resources[8], added);
        cache.Clear();
        Assert.All(resources, resource => Assert.Equal(1, resource.DisposeCount));
    }

    [Fact]
    public void InputViews_ClearReleasesEveryOwnerOnceAndAllowsPointerReuse()
    {
        var cache = CreateCache();
        var previous = new OwnedResource();
        cache.Add(new IntPtr(42), 0, previous);
        cache.Clear();
        cache.Clear();
        Assert.Equal(1, previous.DisposeCount);
        Assert.Equal(0, cache.Count());
        Assert.False(cache.TryGet(new IntPtr(42), 0, out _));

        var replacement = new OwnedResource();
        cache.Add(new IntPtr(42), 0, replacement);
        Assert.True(cache.TryGet(new IntPtr(42), 0, out var found));
        Assert.Same(replacement, found);
        cache.Clear();
        Assert.Equal(1, replacement.DisposeCount);
    }

    [Theory]
    [InlineData(-1, 1, 1, 0)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(5, 3, 2, 5)]
    [InlineData(99, 3, 2, 3)]
    [InlineData(100, 3, 2, 4)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
    public void InputViews_NormalizeSubresourceLikeTheNativeViewDescription(int index, int mipLevels, int arraySize, int expected)
    {
        var type = CacheType();
        var normalize = type.GetMethod("NormalizeSubresource", BindingFlags.Public | BindingFlags.Static)!;
        Assert.Equal(expected, normalize.Invoke(null, new object[] { index, mipLevels, arraySize }));
    }

    [Fact]
    public void InputViews_WarmedLookupDoesNotAllocate()
    {
        var cache = CreateCache();
        var resource = new OwnedResource();
        cache.Add(new IntPtr(17), 2, resource);
        for (var i = 0; i < 1000; i++) cache.TryGet(new IntPtr(17), 2, out _);

        var before = GC.GetAllocatedBytesForCurrentThread();
        var allMatched = true;
        for (var i = 0; i < 10000; i++)
        {
            allMatched &= cache.TryGet(new IntPtr(17), 2, out var found) && ReferenceEquals(resource, found);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allMatched);
        Assert.Equal(0, allocated);
        cache.Clear();
    }

    private static Type CacheType()
        => SussudioAssembly.Load().GetType("Sussudio.Services.Preview.PreviewInputViewCache`1", throwOnError: true)!
            .MakeGenericType(typeof(OwnedResource));

    private static CacheCalls CreateCache()
    {
        var type = CacheType();
        var instance = Activator.CreateInstance(type, nonPublic: true)!;
        return new CacheCalls(
            type.GetMethod("Add")!.CreateDelegate<Action<IntPtr, int, OwnedResource>>(instance),
            type.GetMethod("TryGet")!.CreateDelegate<TryGetResource>(instance),
            type.GetMethod("Clear")!.CreateDelegate<Action>(instance),
            type.GetProperty("Count")!.GetMethod!.CreateDelegate<Func<int>>(instance));
    }

    private delegate bool TryGetResource(IntPtr texture, int subresource, out OwnedResource? resource);
    private sealed record CacheCalls(Action<IntPtr, int, OwnedResource> Add, TryGetResource TryGet, Action Clear, Func<int> Count);

    private sealed class OwnedResource : IDisposable
    {
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }
}
