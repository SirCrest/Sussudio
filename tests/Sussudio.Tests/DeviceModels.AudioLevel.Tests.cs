using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AudioLevelEventArgs_ExposesPeakRmsAndClippedState()
    {
        var argsType = RequireType("Sussudio.Models.AudioLevelEventArgs");
        if (!typeof(EventArgs).IsAssignableFrom(argsType))
        {
            throw new InvalidOperationException("AudioLevelEventArgs must derive from EventArgs.");
        }

        var peakProperty = RequirePublicProperty(argsType, "Peak", typeof(double), SetterExpectation.Forbidden);
        var rmsProperty = RequirePublicProperty(argsType, "Rms", typeof(double), SetterExpectation.Forbidden);
        var clippedProperty = RequirePublicProperty(argsType, "Clipped", typeof(bool), SetterExpectation.Forbidden);
        var constructor = argsType.GetConstructor(new[] { typeof(double), typeof(double), typeof(bool) })
            ?? throw new InvalidOperationException("AudioLevelEventArgs(double, double, bool) constructor not found.");

        var clippedArgs = constructor.Invoke(new object[] { 0.75d, 0.25d, true })
            ?? throw new InvalidOperationException("Failed to create AudioLevelEventArgs.");
        AssertEqual(0.75d, peakProperty.GetValue(clippedArgs), "AudioLevelEventArgs.Peak");
        AssertEqual(0.25d, rmsProperty.GetValue(clippedArgs), "AudioLevelEventArgs.Rms");
        AssertEqual(true, clippedProperty.GetValue(clippedArgs), "AudioLevelEventArgs.Clipped true");

        var unclippedArgs = constructor.Invoke(new object[] { 0.1d, 0.05d, false })
            ?? throw new InvalidOperationException("Failed to create unclipped AudioLevelEventArgs.");
        AssertEqual(0.1d, peakProperty.GetValue(unclippedArgs), "AudioLevelEventArgs unclipped Peak");
        AssertEqual(0.05d, rmsProperty.GetValue(unclippedArgs), "AudioLevelEventArgs unclipped Rms");
        AssertEqual(false, clippedProperty.GetValue(unclippedArgs), "AudioLevelEventArgs.Clipped false");

        return Task.CompletedTask;
    }
}
