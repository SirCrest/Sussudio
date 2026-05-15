using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AudioInputDevice_DisplayName_UsesNameOrUnknownFallback()
    {
        var deviceType = RequireType("Sussudio.Models.AudioInputDevice");
        var idProperty = RequirePublicProperty(deviceType, "Id", typeof(string), SetterExpectation.Required);
        var nameProperty = RequirePublicProperty(deviceType, "Name", typeof(string), SetterExpectation.Required);
        var displayNameProperty = RequirePublicProperty(deviceType, "DisplayName", typeof(string), SetterExpectation.Forbidden);
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("Failed to create AudioInputDevice.");

        AssertEqual(string.Empty, idProperty.GetValue(device), "AudioInputDevice.Id default");
        AssertEqual(string.Empty, nameProperty.GetValue(device), "AudioInputDevice.Name default");
        AssertEqual("Unknown Audio Device", displayNameProperty.GetValue(device), "AudioInputDevice default DisplayName");
        AssertEqual("Unknown Audio Device", device.ToString(), "AudioInputDevice default ToString");

        nameProperty.SetValue(device, "   ");
        AssertEqual("Unknown Audio Device", displayNameProperty.GetValue(device), "AudioInputDevice whitespace DisplayName");
        AssertEqual("Unknown Audio Device", device.ToString(), "AudioInputDevice whitespace ToString");

        idProperty.SetValue(device, "audio-1");
        nameProperty.SetValue(device, "Wave Link Microphone");
        AssertEqual("audio-1", idProperty.GetValue(device), "AudioInputDevice.Id round-trip");
        AssertEqual("Wave Link Microphone", displayNameProperty.GetValue(device), "AudioInputDevice named DisplayName");
        AssertEqual("Wave Link Microphone", device.ToString(), "AudioInputDevice named ToString");

        return Task.CompletedTask;
    }
}
