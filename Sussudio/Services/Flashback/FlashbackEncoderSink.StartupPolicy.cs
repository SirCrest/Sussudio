using System;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private static int ResolveVideoQueueCapacity(FlashbackSessionContext context, bool useHardwareFrames)
        => !useHardwareFrames && IsHighResolutionFrame(context)
            ? HighResolutionCpuVideoQueueCapacity
            : DefaultVideoQueueCapacity;

    private static bool IsHighResolutionFrame(FlashbackSessionContext context)
        => context.Width >= 2560 || context.Height >= 1440;

    private static double ResolveSessionFrameRate(double frameRate)
    {
        if (!double.IsFinite(frameRate) || frameRate <= 0)
        {
            return FallbackSessionFrameRate;
        }

        return Math.Min(frameRate, MaxSessionFrameRate);
    }

    private static void ValidateSessionContext(FlashbackSessionContext context)
    {
        if (context.Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.Width, "Flashback session width must be positive.");
        }

        if (context.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(context), context.Height, "Flashback session height must be positive.");
        }

        if (string.IsNullOrWhiteSpace(context.CodecName))
        {
            throw new ArgumentException("Flashback session codec name is required.", nameof(context));
        }
    }
}
