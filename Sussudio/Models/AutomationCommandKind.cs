namespace Sussudio.Models;

// Numeric automation command identifiers shared by the app, ssctl, MCP, and the
// generic AutomationClient. The pipe protocol serializes the numeric value, so
// these IDs are part of the on-wire contract.
//
// MAINTAINERS — STRICT ORDERING RULES:
//   1. APPEND new members at the end with the next sequential explicit value.
//      Do NOT insert a new member in the middle, even if "the next value is
//      free". Inserting reorders the source but keeps explicit values stable;
//      that's safe, but appending is the convention so reviewers don't have to
//      manually verify every value.
//   2. NEVER renumber an existing member. A stale ssctl/MCP/StreamDeck client
//      will silently misroute commands (e.g. SetRecordingFormat -> SetQuality)
//      and corrupt user recording profiles.
//   3. NEVER reuse a value freed by a deleted member. Reserve the gap or
//      replace the deleted entry with a sentinel — old clients still encode
//      the old value, and reuse would route them to the wrong handler.
//   4. When you add/remove/rename ANY member, bump
//      AutomationPipeProtocol.CommandManifestRevision by exactly +1. The
//      server uses the revision to reject mismatched clients before they can
//      dispatch a command.
public enum AutomationCommandKind
{
    Authenticate = 0,
    GetSnapshot = 1,
    GetDiagnostics = 2,
    RefreshDevices = 3,
    SelectDevice = 4,
    SelectAudioInputDevice = 5,
    SetCustomAudioInput = 6,
    SetResolution = 7,
    SetFrameRate = 8,
    SetRecordingFormat = 9,
    SetQuality = 10,
    SetCustomBitrate = 11,
    SetHdrEnabled = 12,
    SetAudioEnabled = 13,
    SetAudioPreviewEnabled = 14,
    SetOutputPath = 15,
    SetPreviewEnabled = 16,
    SetRecordingEnabled = 17,
    ArmClose = 18,
    WindowAction = 19,
    WaitForCondition = 20,
    VerifyLastRecording = 21,
    AssertSnapshot = 22,
    SetTrueHdrPreviewEnabled = 23,
    ProbeVideoSource = 24,
    ProbePreviewColor = 25,
    CapturePreviewFrame = 26,
    CaptureWindowScreenshot = 27,
    SetVideoFormat = 28,
    GetCaptureOptions = 29,
    SetPreset = 30,
    SetSplitEncodeMode = 31,
    SetMjpegDecoderCount = 32,
    SetShowAllCaptureOptions = 33,
    SetPreviewVolume = 34,
    SetStatsVisible = 35,
    SetDeviceAudioMode = 36,
    GetPerformanceTimeline = 37,
    SetStatsSectionVisible = 38,
    SetAnalogAudioGain = 39,
    SetSettingsVisible = 40,
    FlashbackAction = 41,
    FlashbackExport = 42,
    FlashbackGetSegments = 43,
    VerifyFile = 44,
    RestartFlashback = 45,
    SetMicrophoneEnabled = 46,
    SetFlashbackEnabled = 47,
    GetAudioRampTrace = 48,
    SetFrameTimeOverlayVisible = 49,
    SetFlashbackTimelineVisible = 50,
    GetAutomationManifest = 51,
    SetFullScreenEnabled = 52,
    OpenRecordingsFolder = 53
}
