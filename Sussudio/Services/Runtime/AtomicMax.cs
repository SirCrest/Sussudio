using System.Threading;

namespace Sussudio.Services.Runtime;

// Lock-free max-update helpers. Several diagnostic counters across capture,
// recording, and flashback need to track high-water marks under contention; the
// CAS-loop pattern was open-coded in four files before this consolidation.
internal static class AtomicMax
{
    public static void Update(ref int target, int candidate)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }

    public static void Update(ref long target, long candidate)
    {
        while (true)
        {
            var current = Interlocked.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }
}
