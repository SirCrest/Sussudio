using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task AudioDeviceSelectionPolicy_LivesInFocusedHelper()
    {
        var adapterText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioDeviceDiscovery.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/AudioDeviceSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class AudioDeviceSelectionPolicy");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectStartup(");
        AssertContains(policyText, "internal static AudioDeviceSelection SelectRefresh(");
        AssertContains(policyText, "internal static IReadOnlyList<AudioInputDevice> FilterOutCaptureCardAudio(");
        AssertContains(policyText, "SelectByPreviousSavedOrFirst(availableDevices, previousAudioId, savedAudioId)");
        AssertContains(policyText, "SelectByPreviousOrFirst(availableDevices, previousAudioId)");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectStartup(");
        AssertContains(adapterText, "AudioDeviceSelectionPolicy.SelectRefresh(");
        AssertContains(adapterText, "ReplaceCollection(AudioInputDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);");
        AssertContains(adapterText, "Logger.Log($\"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.\");");
        AssertContains(adapterText, "Logger.Log($\"Audio device list refreshed ({AudioInputDevices.Count} devices).\");");
        AssertDoesNotContain(policyText, "ReplaceCollection(");
        AssertDoesNotContain(policyText, "Logger.Log(");
        AssertDoesNotContain(policyText, "_pendingSaved");

        return Task.CompletedTask;
    }

    private static Task AudioDeviceSelectionPolicy_StartupFiltersCaptureCardAndUsesSavedFallbacks()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("CAPTURE-AUDIO"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.CaptureDevice",
            CreateAudioDeviceSelectionPolicyCapture("video-first", "other-capture"),
            CreateAudioDeviceSelectionPolicyCapture("video-previous", "capture-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "video-previous",
            "missing-audio",
            "saved-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Startup audio list filters the capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Startup first filtered audio id");
        AssertEqual("saved-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup saved audio fallback");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup saved audio found");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup saved microphone found");

        return Task.CompletedTask;
    }

    private static Task AudioDeviceSelectionPolicy_StartupPreservesPreviousSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-audio"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-mic"));
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");

        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Startup preserves previous audio");
        AssertEqual("previous-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Startup preserves previous microphone");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Startup keeps existing saved-audio fallback log decision");
        AssertEqual(true, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Startup keeps existing saved-microphone fallback log decision");

        return Task.CompletedTask;
    }

    private static Task AudioDeviceSelectionPolicy_RefreshPreservesPreviousAudioAndSavedMicrophoneFallback()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList(
            "Sussudio.Models.AudioInputDevice",
            CreateAudioDeviceSelectionPolicyAudio("capture-audio"),
            CreateAudioDeviceSelectionPolicyAudio("first-audio"),
            CreateAudioDeviceSelectionPolicyAudio("saved-mic"),
            CreateAudioDeviceSelectionPolicyAudio("previous-audio"));

        var selection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            "CAPTURE-AUDIO",
            "previous-audio",
            "missing-mic",
            "saved-mic");

        var availableIds = GetAudioDeviceSelectionAvailableIds(selection);
        AssertEqual(3, availableIds.Length, "Refresh audio list filters selected capture-card endpoint");
        AssertEqual("first-audio", availableIds[0], "Refresh first filtered audio id");
        AssertEqual("previous-audio", GetAudioDeviceSelectionId(selection, "SelectedAudioInputDevice"), "Refresh preserves previous audio");
        AssertEqual("saved-mic", GetAudioDeviceSelectionId(selection, "SelectedMicrophoneDevice"), "Refresh saved microphone fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedAudioFallback"), "Refresh does not log saved audio fallback");
        AssertEqual(false, GetBoolProperty(selection, "ShouldLogSavedMicrophoneFallback"), "Refresh does not log saved microphone fallback");

        return Task.CompletedTask;
    }

    private static Task AudioDeviceSelectionPolicy_EmptyListsReturnNullSelections()
    {
        var audioDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.AudioInputDevice");
        var videoDevices = CreateAudioDeviceSelectionPolicyList("Sussudio.Models.CaptureDevice");

        var startupSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectStartup",
            audioDevices,
            videoDevices,
            "missing-video",
            "previous-audio",
            "saved-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(startupSelection).Length, "Startup empty audio list");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedAudioInputDevice"), "Startup empty audio selection");
        AssertEqual(null, GetPropertyValue(startupSelection, "SelectedMicrophoneDevice"), "Startup empty microphone selection");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedAudioFallback"), "Startup empty saved audio fallback log decision");
        AssertEqual(true, GetBoolProperty(startupSelection, "ShouldLogSavedMicrophoneFallback"), "Startup empty saved microphone fallback log decision");

        var refreshSelection = InvokeAudioDeviceSelectionPolicy(
            "SelectRefresh",
            audioDevices,
            null,
            "previous-audio",
            "previous-mic",
            "saved-mic");
        AssertEqual(0, GetAudioDeviceSelectionAvailableIds(refreshSelection).Length, "Refresh empty audio list");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedAudioInputDevice"), "Refresh empty audio selection");
        AssertEqual(null, GetPropertyValue(refreshSelection, "SelectedMicrophoneDevice"), "Refresh empty microphone selection");
        AssertEqual(false, GetBoolProperty(refreshSelection, "ShouldLogSavedMicrophoneFallback"), "Refresh empty saved microphone log decision");

        return Task.CompletedTask;
    }

    private static object InvokeAudioDeviceSelectionPolicy(string methodName, params object?[] arguments)
    {
        var policyType = RequireType("Sussudio.ViewModels.AudioDeviceSelectionPolicy");
        var method = policyType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing AudioDeviceSelectionPolicy.{methodName}.");
        return method.Invoke(null, arguments)
               ?? throw new InvalidOperationException($"AudioDeviceSelectionPolicy.{methodName} returned null.");
    }

    private static object CreateAudioDeviceSelectionPolicyAudio(string id)
    {
        var audioType = RequireType("Sussudio.Models.AudioInputDevice");
        var audio = Activator.CreateInstance(audioType)
            ?? throw new InvalidOperationException("Failed to create AudioInputDevice.");
        SetPropertyOrBackingField(audio, "Id", id);
        SetPropertyOrBackingField(audio, "Name", id);
        return audio;
    }

    private static object CreateAudioDeviceSelectionPolicyCapture(string id, string? audioDeviceId)
    {
        var captureType = RequireType("Sussudio.Models.CaptureDevice");
        var capture = Activator.CreateInstance(captureType)
            ?? throw new InvalidOperationException("Failed to create CaptureDevice.");
        SetPropertyOrBackingField(capture, "Id", id);
        SetPropertyOrBackingField(capture, "Name", id);
        SetPropertyOrBackingField(capture, "AudioDeviceId", audioDeviceId);
        return capture;
    }

    private static object CreateAudioDeviceSelectionPolicyList(string elementTypeName, params object[] items)
    {
        var elementType = RequireType(elementTypeName);
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(elementType))
            ?? throw new InvalidOperationException($"Failed to create list for {elementTypeName}."));
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static string? GetAudioDeviceSelectionId(object selection, string propertyName)
    {
        var device = GetPropertyValue(selection, propertyName);
        return device != null ? GetStringProperty(device, "Id") : null;
    }

    private static string[] GetAudioDeviceSelectionAvailableIds(object selection)
    {
        var devices = (IEnumerable)(GetPropertyValue(selection, "AvailableDevices")
            ?? throw new InvalidOperationException("AudioDeviceSelection.AvailableDevices was null."));
        return devices.Cast<object>().Select(device => GetStringProperty(device, "Id")).ToArray();
    }
}
