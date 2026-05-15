using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureDevice_DisplayNameAndDefaults_PreserveDeviceMetadata()
    {
        var deviceType = RequireType("Sussudio.Models.CaptureDevice");
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var supportedFormatsType = typeof(ObservableCollection<>).MakeGenericType(mediaFormatType);
        var idProperty = RequirePublicProperty(deviceType, "Id", typeof(string), SetterExpectation.Required);
        var nameProperty = RequirePublicProperty(deviceType, "Name", typeof(string), SetterExpectation.Required);
        var nativeXuProperty = RequirePublicProperty(deviceType, "NativeXuInterfacePath", typeof(string), SetterExpectation.Required);
        var audioDeviceIdProperty = RequirePublicProperty(deviceType, "AudioDeviceId", typeof(string), SetterExpectation.Required);
        var audioDeviceNameProperty = RequirePublicProperty(deviceType, "AudioDeviceName", typeof(string), SetterExpectation.Required);
        var isHdrCapableProperty = RequirePublicProperty(deviceType, "IsHdrCapable", typeof(bool), SetterExpectation.Required);
        var supportedFormatsProperty = RequirePublicProperty(deviceType, "SupportedFormats", supportedFormatsType, SetterExpectation.Required);
        var displayNameProperty = RequirePublicProperty(deviceType, "DisplayName", typeof(string), SetterExpectation.Forbidden);
        var device = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("Failed to create CaptureDevice.");

        AssertEqual(string.Empty, idProperty.GetValue(device), "CaptureDevice.Id default");
        AssertEqual(string.Empty, nameProperty.GetValue(device), "CaptureDevice.Name default");
        AssertEqual(null, nativeXuProperty.GetValue(device), "CaptureDevice.NativeXuInterfacePath default");
        AssertEqual(null, audioDeviceIdProperty.GetValue(device), "CaptureDevice.AudioDeviceId default");
        AssertEqual(null, audioDeviceNameProperty.GetValue(device), "CaptureDevice.AudioDeviceName default");
        AssertEqual(false, isHdrCapableProperty.GetValue(device), "CaptureDevice.IsHdrCapable default");
        AssertEqual("Unknown Device", displayNameProperty.GetValue(device), "CaptureDevice default DisplayName");
        AssertEqual("Unknown Device", device.ToString(), "CaptureDevice default ToString");

        nameProperty.SetValue(device, "   ");
        AssertEqual("Unknown Device", displayNameProperty.GetValue(device), "CaptureDevice whitespace DisplayName");
        AssertEqual("Unknown Device", device.ToString(), "CaptureDevice whitespace ToString");

        var supportedFormats = supportedFormatsProperty.GetValue(device);
        AssertNotNull(supportedFormats, "CaptureDevice.SupportedFormats default");
        AssertEqual(supportedFormatsType, supportedFormats!.GetType(), "CaptureDevice.SupportedFormats runtime type");
        AssertEqual(0, GetCountProperty(supportedFormats!), "CaptureDevice.SupportedFormats default count");

        var secondDevice = Activator.CreateInstance(deviceType)
            ?? throw new InvalidOperationException("Failed to create second CaptureDevice.");
        var secondSupportedFormats = supportedFormatsProperty.GetValue(secondDevice);
        if (ReferenceEquals(supportedFormats, secondSupportedFormats))
        {
            throw new InvalidOperationException("CaptureDevice.SupportedFormats collections must not be shared between instances.");
        }

        var format = Activator.CreateInstance(mediaFormatType)
            ?? throw new InvalidOperationException("Failed to create MediaFormat.");
        supportedFormatsType.GetMethod("Add", new[] { mediaFormatType })!.Invoke(supportedFormats, new[] { format });
        AssertEqual(1, GetCountProperty(supportedFormats), "CaptureDevice.SupportedFormats add count");
        AssertEqual(0, GetCountProperty(secondSupportedFormats!), "Second CaptureDevice.SupportedFormats count");
        var replacementFormats = Activator.CreateInstance(supportedFormatsType)
            ?? throw new InvalidOperationException("Failed to create replacement SupportedFormats collection.");
        var replacementFormat = Activator.CreateInstance(mediaFormatType)
            ?? throw new InvalidOperationException("Failed to create replacement MediaFormat.");
        supportedFormatsType.GetMethod("Add", new[] { mediaFormatType })!.Invoke(replacementFormats, new[] { replacementFormat });
        supportedFormatsProperty.SetValue(device, replacementFormats);
        AssertEqual(replacementFormats, supportedFormatsProperty.GetValue(device), "CaptureDevice.SupportedFormats replacement round-trip");
        AssertEqual(1, GetCountProperty(replacementFormats), "CaptureDevice.SupportedFormats replacement count");

        idProperty.SetValue(device, "device-1");
        nameProperty.SetValue(device, "Game Capture 4K X");
        nativeXuProperty.SetValue(device, @"\\?\hid#vid_0fd9");
        audioDeviceIdProperty.SetValue(device, "audio-1");
        audioDeviceNameProperty.SetValue(device, "4K X Audio");
        isHdrCapableProperty.SetValue(device, true);

        AssertEqual("device-1", idProperty.GetValue(device), "CaptureDevice.Id round-trip");
        AssertEqual("Game Capture 4K X", displayNameProperty.GetValue(device), "CaptureDevice named DisplayName");
        AssertEqual("Game Capture 4K X", device.ToString(), "CaptureDevice named ToString");
        AssertEqual(@"\\?\hid#vid_0fd9", nativeXuProperty.GetValue(device), "CaptureDevice.NativeXuInterfacePath round-trip");
        AssertEqual("audio-1", audioDeviceIdProperty.GetValue(device), "CaptureDevice.AudioDeviceId round-trip");
        AssertEqual("4K X Audio", audioDeviceNameProperty.GetValue(device), "CaptureDevice.AudioDeviceName round-trip");
        AssertEqual(true, isHdrCapableProperty.GetValue(device), "CaptureDevice.IsHdrCapable round-trip");

        return Task.CompletedTask;
    }
}
