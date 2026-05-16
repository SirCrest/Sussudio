using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private sealed class RecordingStartRollbackState
    {
        public LibAvRecordingSink? LibAvSink { get; set; }

        public IRecordingSink? RecordingSink { get; set; }

        public WasapiAudioCapture? OwnedWasapiAudioCapture { get; set; }

        public UnifiedVideoCapture? OwnedUnifiedVideoCapture { get; set; }

        public RecordingContext? RecordingContext { get; set; }

        public UnifiedVideoCapture? RecordingVideoCapture { get; set; }

        public FlashbackEncoderSink? FlashbackRecordingStartedSink { get; set; }

        public bool FlashbackRecordingBackendLeaseHeld { get; set; }

        public bool SinkAttachedForAudioOnly { get; set; }
    }
}
