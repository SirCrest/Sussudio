using System;
using System.Collections.Generic;
using Sussudio.Services.Preview;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production <see cref="PreviewInputViewCache{TResource}"/> source directly.
/// The cache owns native view lifetimes on the render thread, so these tests pin the
/// disposal and eviction contract behaviorally rather than by source-text assertion.
/// </summary>
public sealed class PreviewInputViewCacheTests
{
    private sealed class TrackedResource : IDisposable
    {
        private readonly Action? _onDispose;

        public TrackedResource(string name, Action? onDispose = null)
        {
            Name = name;
            _onDispose = onDispose;
        }

        public string Name { get; }

        public int DisposeCount { get; private set; }

        public bool IsDisposed => DisposeCount > 0;

        public void Dispose()
        {
            DisposeCount++;
            _onDispose?.Invoke();
        }

        public override string ToString() => Name;
    }

    private static IntPtr Texture(int id) => new(id);

    // ---- TryGet / Add --------------------------------------------------------

    [Fact]
    public void TryGet_MissesOnAnEmptyCacheAndCountsTheMiss()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();

        Assert.False(cache.TryGet(Texture(1), 0, out var resource));
        Assert.Null(resource);
        Assert.Equal(0, cache.HitCount);
        Assert.Equal(1, cache.MissCount);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void TryGet_ReturnsTheAddedResourceAndCountsTheHit()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        var view = new TrackedResource("a");
        cache.Add(Texture(1), 0, view);

        Assert.True(cache.TryGet(Texture(1), 0, out var resource));
        Assert.Same(view, resource);
        Assert.Equal(1, cache.HitCount);
        Assert.Equal(0, cache.MissCount);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void TryGet_TreatsSubresourceAsPartOfTheKey()
    {
        // Two array slices of the same texture are different views; returning one
        // for the other would present the wrong slice.
        var cache = new PreviewInputViewCache<TrackedResource>();
        var slice0 = new TrackedResource("slice0");
        var slice1 = new TrackedResource("slice1");
        cache.Add(Texture(1), 0, slice0);
        cache.Add(Texture(1), 1, slice1);

        Assert.True(cache.TryGet(Texture(1), 0, out var first));
        Assert.True(cache.TryGet(Texture(1), 1, out var second));
        Assert.Same(slice0, first);
        Assert.Same(slice1, second);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Add_RejectsANullResourceRatherThanCachingAHole()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();

        Assert.Throws<ArgumentNullException>(() => cache.Add(Texture(1), 0, null!));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Add_ReplacingTheSameKeyDisposesTheOldViewAndCountsAnEviction()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        var original = new TrackedResource("original");
        var replacement = new TrackedResource("replacement");

        cache.Add(Texture(1), 0, original);
        cache.Add(Texture(1), 0, replacement);

        Assert.True(original.IsDisposed);
        Assert.False(replacement.IsDisposed);
        Assert.Equal(1, cache.EvictionCount);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet(Texture(1), 0, out var current));
        Assert.Same(replacement, current);
    }

    // ---- Capacity and eviction ----------------------------------------------

    [Fact]
    public void Add_FillsEverySlotBeforeEvictingAnything()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        var views = new List<TrackedResource>();

        for (var i = 0; i < PreviewInputViewCache<TrackedResource>.Capacity; i++)
        {
            var view = new TrackedResource($"v{i}");
            views.Add(view);
            cache.Add(Texture(i + 1), 0, view);
        }

        Assert.Equal(PreviewInputViewCache<TrackedResource>.Capacity, cache.Count);
        Assert.Equal(0, cache.EvictionCount);
        Assert.All(views, view => Assert.False(view.IsDisposed));
    }

    [Fact]
    public void Add_BeyondCapacityEvictsTheLeastRecentlyUsedEntryAndDisposesIt()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        var capacity = PreviewInputViewCache<TrackedResource>.Capacity;
        var views = new List<TrackedResource>();

        for (var i = 0; i < capacity; i++)
        {
            var view = new TrackedResource($"v{i}");
            views.Add(view);
            cache.Add(Texture(i + 1), 0, view);
        }

        // Touch every entry except the first, making it the least recently used.
        for (var i = 1; i < capacity; i++)
        {
            Assert.True(cache.TryGet(Texture(i + 1), 0, out _));
        }

        var newcomer = new TrackedResource("newcomer");
        cache.Add(Texture(999), 0, newcomer);

        Assert.True(views[0].IsDisposed, "the least recently used view should have been evicted");
        Assert.Equal(1, cache.EvictionCount);
        Assert.Equal(capacity, cache.Count);
        Assert.False(cache.TryGet(Texture(1), 0, out _));
        for (var i = 1; i < capacity; i++)
        {
            Assert.False(views[i].IsDisposed, $"v{i} should have survived");
        }
    }

    [Fact]
    public void Add_NeverExceedsCapacityAcrossALongChurn()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        var capacity = PreviewInputViewCache<TrackedResource>.Capacity;
        var live = new List<TrackedResource>();

        for (var i = 0; i < 500; i++)
        {
            var view = new TrackedResource($"v{i}");
            live.Add(view);
            cache.Add(Texture(i + 1), 0, view);

            Assert.InRange(cache.Count, 0, capacity);
        }

        Assert.Equal(capacity, cache.Count);
        Assert.Equal(500 - capacity, cache.EvictionCount);

        // Every view the cache dropped must have been disposed exactly once, and no
        // retained view may have been disposed while still reachable.
        var disposed = live.FindAll(view => view.IsDisposed);
        Assert.Equal(500 - capacity, disposed.Count);
        Assert.All(live, view => Assert.InRange(view.DisposeCount, 0, 1));
    }

    // ---- TryGetTextureResource ----------------------------------------------

    [Fact]
    public void TryGetTextureResource_MatchesAnySubresourceOfTheSameTexture()
    {
        // The description of a retained texture is reused for other array slices,
        // so this lookup deliberately ignores the subresource.
        var cache = new PreviewInputViewCache<TrackedResource>();
        var slice3 = new TrackedResource("slice3");
        cache.Add(Texture(7), 3, slice3);

        Assert.True(cache.TryGetTextureResource(Texture(7), out var resource));
        Assert.Same(slice3, resource);
    }

    [Fact]
    public void TryGetTextureResource_DoesNotDisturbHitOrMissAccounting()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        cache.Add(Texture(7), 0, new TrackedResource("a"));

        cache.TryGetTextureResource(Texture(7), out _);
        cache.TryGetTextureResource(Texture(8), out _);

        Assert.Equal(0, cache.HitCount);
        Assert.Equal(0, cache.MissCount);
    }

    [Fact]
    public void TryGetTextureResource_MissesForAnUnknownTexture()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        cache.Add(Texture(7), 0, new TrackedResource("a"));

        Assert.False(cache.TryGetTextureResource(Texture(8), out var resource));
        Assert.Null(resource);
    }

    // ---- Clear ---------------------------------------------------------------

    [Fact]
    public void Clear_DisposesEveryRetainedViewAndEmptiesTheCache()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        var views = new List<TrackedResource>();
        for (var i = 0; i < 5; i++)
        {
            var view = new TrackedResource($"v{i}");
            views.Add(view);
            cache.Add(Texture(i + 1), 0, view);
        }

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.All(views, view => Assert.Equal(1, view.DisposeCount));
        Assert.False(cache.TryGet(Texture(1), 0, out _));
    }

    [Fact]
    public void Clear_OnAnEmptyCacheIsHarmless()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();

        cache.Clear();

        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Clear_DisposesEveryViewEvenWhenOneThrowsThenRethrowsTheFirstFailure()
    {
        // A failing native release must not strand the remaining views; the cache
        // still clears completely and surfaces the original failure.
        var cache = new PreviewInputViewCache<TrackedResource>();
        var before = new TrackedResource("before");
        var failing = new TrackedResource("failing", () => throw new InvalidOperationException("release failed"));
        var after = new TrackedResource("after");

        cache.Add(Texture(1), 0, before);
        cache.Add(Texture(2), 0, failing);
        cache.Add(Texture(3), 0, after);

        var error = Assert.Throws<InvalidOperationException>(cache.Clear);

        Assert.Equal("release failed", error.Message);
        Assert.True(before.IsDisposed);
        Assert.True(after.IsDisposed, "a failure mid-clear must not skip the remaining views");
        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet(Texture(1), 0, out _));
    }

    [Fact]
    public void Clear_RethrowsOnlyTheFirstFailureWhenSeveralViewsThrow()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        cache.Add(Texture(1), 0, new TrackedResource("first", () => throw new InvalidOperationException("first failure")));
        cache.Add(Texture(2), 0, new TrackedResource("second", () => throw new InvalidOperationException("second failure")));

        var error = Assert.Throws<InvalidOperationException>(cache.Clear);

        Assert.Equal("first failure", error.Message);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Clear_LeavesTheCacheUsableAfterAFailedRelease()
    {
        var cache = new PreviewInputViewCache<TrackedResource>();
        cache.Add(Texture(1), 0, new TrackedResource("boom", () => throw new InvalidOperationException("boom")));

        Assert.Throws<InvalidOperationException>(cache.Clear);

        var fresh = new TrackedResource("fresh");
        cache.Add(Texture(2), 0, fresh);

        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet(Texture(2), 0, out var resource));
        Assert.Same(fresh, resource);
    }

    // ---- NormalizeSubresource ------------------------------------------------

    [Theory]
    [InlineData(0, 1, 1, 0)]
    [InlineData(5, 1, 1, 0)]
    [InlineData(3, 4, 1, 3)]
    [InlineData(4, 4, 2, 4)]
    [InlineData(7, 4, 2, 7)]
    public void NormalizeSubresource_MapsIntoTheDeclaredMipAndArrayExtent(
        int subresource, int mipLevels, int arraySize, int expected)
    {
        Assert.Equal(
            expected,
            PreviewInputViewCache<TrackedResource>.NormalizeSubresource(subresource, mipLevels, arraySize));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NormalizeSubresource_ClampsANegativeSubresourceToZero(int subresource)
    {
        Assert.Equal(0, PreviewInputViewCache<TrackedResource>.NormalizeSubresource(subresource, 4, 2));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-3, -3)]
    public void NormalizeSubresource_TreatsUnreportedMipOrArrayExtentsAsOne(int mipLevels, int arraySize)
    {
        // A driver that reports zero mips or a zero array size must not produce a
        // divide-by-zero or a negative index.
        Assert.Equal(0, PreviewInputViewCache<TrackedResource>.NormalizeSubresource(9, mipLevels, arraySize));
    }

    [Fact]
    public void NormalizeSubresource_NeverLeavesTheAddressableRangeAcrossASweep()
    {
        for (var mipLevels = 1; mipLevels <= 6; mipLevels++)
        {
            for (var arraySize = 1; arraySize <= 6; arraySize++)
            {
                var limit = mipLevels * arraySize;
                for (var subresource = -4; subresource < limit + 32; subresource++)
                {
                    var normalized = PreviewInputViewCache<TrackedResource>.NormalizeSubresource(
                        subresource, mipLevels, arraySize);

                    Assert.InRange(normalized, 0, limit - 1);
                }
            }
        }
    }

    [Fact]
    public void NormalizeSubresource_IsIdempotentForAlreadyValidIndices()
    {
        for (var mipLevels = 1; mipLevels <= 6; mipLevels++)
        {
            for (var arraySize = 1; arraySize <= 6; arraySize++)
            {
                for (var subresource = 0; subresource < mipLevels * arraySize; subresource++)
                {
                    var once = PreviewInputViewCache<TrackedResource>.NormalizeSubresource(
                        subresource, mipLevels, arraySize);
                    var twice = PreviewInputViewCache<TrackedResource>.NormalizeSubresource(
                        once, mipLevels, arraySize);

                    Assert.Equal(once, twice);
                }
            }
        }
    }
}
