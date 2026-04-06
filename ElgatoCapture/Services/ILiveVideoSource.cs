namespace ElgatoCapture.Services;

/// <summary>
/// Abstraction over the live video capture source, consumed by
/// FlashbackPlaybackController to suppress/resume the live preview
/// feed and to obtain the shared D3D device for GPU-accelerated decode.
/// Breaks the bidirectional dependency between Capture and Flashback.
/// </summary>
internal interface ILiveVideoSource
{
    void SuppressPreviewSubmission();
    void ResumePreviewSubmission();
    SharedD3DDeviceManager? D3DManager { get; }
}
