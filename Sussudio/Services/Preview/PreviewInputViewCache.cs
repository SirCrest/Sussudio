using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Sussudio.Services.Preview;

// Owns up to eight texture/view pairs, accessed only by the render thread.
// New resources transfer to the cache only after native creation succeeds.
internal sealed class PreviewInputViewCache<TResource> where TResource : class, IDisposable
{
    internal const int Capacity = 8;
    private readonly Entry[] _entries = new Entry[Capacity];
    private long _accessOrder;

    public long HitCount { get; private set; }
    public long MissCount { get; private set; }
    public long EvictionCount { get; private set; }
    public int Count { get; private set; }

    private struct Entry
    {
        public IntPtr Texture;
        public int Subresource;
        public TResource? Resource;
        public long LastAccess;
    }

    // Texture descriptions are immutable. Reuse the retained texture's description
    // when another array slice arrives, instead of making another driver call.
    public bool TryGetTextureResource(IntPtr texture, [NotNullWhen(true)] out TResource? resource)
    {
        for (var i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Texture == texture && _entries[i].Resource is { } cachedResource)
            {
                resource = cachedResource;
                return true;
            }
        }

        resource = null;
        return false;
    }

    public bool TryGet(IntPtr texture, int subresource, [NotNullWhen(true)] out TResource? resource)
    {
        for (var i = 0; i < _entries.Length; i++)
        {
            ref var entry = ref _entries[i];
            if (entry.Texture == texture && entry.Subresource == subresource && entry.Resource is { } cachedResource)
            {
                entry.LastAccess = ++_accessOrder;
                resource = cachedResource;
                HitCount++;
                return true;
            }
        }

        resource = null;
        MissCount++;
        return false;
    }

    public void Add(IntPtr texture, int subresource, TResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var slot = 0;
        for (var i = 0; i < _entries.Length; i++)
        {
            ref var candidate = ref _entries[i];
            if (candidate.Resource == null || (candidate.Texture == texture && candidate.Subresource == subresource))
            {
                slot = i;
                break;
            }

            if (candidate.LastAccess < _entries[slot].LastAccess)
            {
                slot = i;
            }
        }

        ref var entry = ref _entries[slot];
        if (entry.Resource != null)
        {
            var previous = entry.Resource;
            entry = default;
            Count--;
            previous.Dispose();
            EvictionCount++;
        }

        entry = new Entry
        {
            Texture = texture,
            Subresource = subresource,
            Resource = resource,
            LastAccess = ++_accessOrder
        };
        Count++;
    }

    public void Clear()
    {
        ExceptionDispatchInfo? failure = null;
        for (var i = 0; i < _entries.Length; i++)
        {
            var resource = _entries[i].Resource;
            _entries[i] = default;
            try
            {
                resource?.Dispose();
            }
            catch (Exception ex)
            {
                failure ??= ExceptionDispatchInfo.Capture(ex);
            }
        }

        Count = 0;
        failure?.Throw();
    }

    public static int NormalizeSubresource(int subresource, int mipLevels, int arraySize)
    {
        mipLevels = Math.Max(1, mipLevels);
        arraySize = Math.Max(1, arraySize);
        subresource = Math.Max(0, subresource);
        var arraySlice = Math.Min(subresource / mipLevels, arraySize - 1);
        return arraySlice * mipLevels + subresource % mipLevels;
    }
}
