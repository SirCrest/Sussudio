using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private ID3D11Texture2D EnsureFrameCaptureStagingTexture(Texture2DDescription backBufferDescription, int width, int height)
    {
        if (_captureStagingTexture == null ||
            _captureStagingWidth != width ||
            _captureStagingHeight != height)
        {
            _captureStagingTexture?.Dispose();
            _captureStagingTexture = _device!.CreateTexture2D(new Texture2DDescription(
                backBufferDescription.Format,
                (uint)width,
                (uint)height,
                1,
                1,
                BindFlags.None,
                ResourceUsage.Staging,
                CpuAccessFlags.Read,
                1,
                0,
                ResourceOptionFlags.None));
            _captureStagingWidth = width;
            _captureStagingHeight = height;
        }

        return _captureStagingTexture!;
    }
}
