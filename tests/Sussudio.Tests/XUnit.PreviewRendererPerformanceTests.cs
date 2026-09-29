using System;
using Xunit;

namespace Sussudio.Tests;

public sealed class PreviewRendererPerformanceTests
{
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
