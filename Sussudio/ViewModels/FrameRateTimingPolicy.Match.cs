using System;

namespace Sussudio.ViewModels;

// Kept in a small linked source file so CaptureBindingControllers' linked tests
// execute the same frame-rate comparison policy as the application.
internal static partial class FrameRateTimingPolicy
{
    internal static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;
}
