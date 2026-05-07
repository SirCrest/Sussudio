using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;

// Tests for capture/audio device display and option models.
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

    private enum SetterExpectation
    {
        Required,
        Forbidden
    }

    private static PropertyInfo RequirePublicProperty(
        Type type,
        string propertyName,
        Type propertyType,
        SetterExpectation setterExpectation)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(property, $"{type.Name}.{propertyName}");
        AssertEqual(propertyType, property!.PropertyType, $"{type.Name}.{propertyName} property type");
        if (property.GetMethod == null || !property.GetMethod.IsPublic)
        {
            throw new InvalidOperationException($"{type.Name}.{propertyName} must expose a public getter.");
        }

        if (setterExpectation == SetterExpectation.Required &&
            (property.SetMethod == null || !property.SetMethod.IsPublic))
        {
            throw new InvalidOperationException($"{type.Name}.{propertyName} must expose a public setter.");
        }

        if (setterExpectation == SetterExpectation.Forbidden && property.SetMethod != null)
        {
            throw new InvalidOperationException($"{type.Name}.{propertyName} must not expose a setter.");
        }

        return property;
    }
}
