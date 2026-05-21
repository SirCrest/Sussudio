using System;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private ID3D11Texture2D? _inputTexture;
    private ID3D11Texture2D? _stagingTexture;
    private ID3D11VideoProcessorInputView? _inputView;

    private void EnsureInputResources(int width, int height, bool isHdr)
    {
        if (_device == null || _videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for input texture creation.");
        }

        var targetFormat = isHdr ? Format.P010 : Format.NV12;
        if (_inputTexture != null &&
            _stagingTexture != null &&
            _inputView != null &&
            _configuredInputWidth == width &&
            _configuredInputHeight == height &&
            _configuredInputFormat == targetFormat)
        {
            return;
        }

        _inputView?.Dispose();
        _inputView = null;
        _inputTexture?.Dispose();
        _inputTexture = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        var inputDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _inputTexture = _device.CreateTexture2D(inputDescription);
        _stagingTexture = _device.CreateTexture2D(stagingDescription);

        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView { MipSlice = 0, ArraySlice = 0 }
        };

        _inputView = _videoDevice.CreateVideoProcessorInputView(_inputTexture, _videoProcessorEnumerator, inputViewDescription);
        _configuredInputFormat = targetFormat;
    }

    private void DisposeProcessorInputResources()
    {
        _inputView?.Dispose();
        _inputView = null;
    }

    private void DisposeInputTextureResources()
    {
        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _inputTexture?.Dispose();
        _inputTexture = null;
    }
}
