using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// Monotonic event counter. The 128-byte explicit layout puts the value in the middle of the object so neighbouring
/// hot counters allocated back to back do not share a cache line (best effort; the GC only guarantees 8-byte
/// alignment). One interlocked add per event, no allocation, no lock.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 128)]
public sealed class PerfCounter
{
    [FieldOffset(56)]
    private long _value;

    public void Increment() => Interlocked.Increment(ref _value);

    public void Add(long amount) => Interlocked.Add(ref _value, amount);

    public long Read() => Volatile.Read(ref _value);
}
