namespace Sussudio.Services.Preview;

/// <summary>
/// Abstraction over the live video capture source, consumed by flashback playback
/// to suppress/resume the live preview feed and to obtain the shared D3D device.
/// </summary>
internal interface ILiveVideoSource
{
    void SuppressPreviewSubmission();
    void ResumePreviewSubmission();
    SharedD3DDeviceManager? D3DManager { get; }
}
