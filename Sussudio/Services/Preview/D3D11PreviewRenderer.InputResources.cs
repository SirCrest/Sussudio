using System;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
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

    private void EnsureHdrInputResources(int width, int height)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for HDR shader input texture creation.");
        }

        if (_hdrPlaneViewsUnavailable)
        {
            return;
        }

        if (_hdrInputTexture != null &&
            _hdrStagingTexture != null &&
            _hdrYPlaneSRV != null &&
            _hdrUVPlaneSRV != null &&
            _hdrInputConfiguredWidth == width &&
            _hdrInputConfiguredHeight == height)
        {
            return;
        }

        _hdrYPlaneSRV?.Dispose();
        _hdrYPlaneSRV = null;
        _hdrUVPlaneSRV?.Dispose();
        _hdrUVPlaneSRV = null;
        _hdrInputTexture?.Dispose();
        _hdrInputTexture = null;
        _hdrStagingTexture?.Dispose();
        _hdrStagingTexture = null;

        var inputDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.ShaderResource,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            Format.P010,
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

        _hdrInputTexture = _device.CreateTexture2D(inputDescription);
        _hdrStagingTexture = _device.CreateTexture2D(stagingDescription);

        _hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);
        _hdrUVPlaneSRV = CreateHdrPlaneView(Format.R16G16_UNorm, planeSlice: 1);

        if (_hdrYPlaneSRV == null && _hdrUVPlaneSRV == null)
        {
            _hdrInputTexture.Dispose();
            _hdrInputTexture = null;
            _hdrStagingTexture.Dispose();
            _hdrStagingTexture = null;
            _hdrPlaneViewsUnavailable = true;
            return;
        }

        _hdrInputConfiguredWidth = width;
        _hdrInputConfiguredHeight = height;
    }

    private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)
    {
        if (_device == null || _hdrInputTexture == null)
        {
            throw new InvalidOperationException("HDR shader input texture has not been created.");
        }

        if (_device3 != null)
        {
            var srvDesc = new ShaderResourceViewDescription1(
                _hdrInputTexture,
                ShaderResourceViewDimension.Texture2D,
                format,
                0,
                1,
                0,
                1,
                planeSlice);

            return _device3.CreateShaderResourceView1(_hdrInputTexture, srvDesc);
        }

        Logger.Log("D3D11_RENDERER_WARN Device3 not available for P010 plane views — HDR shader path disabled, falling back to VideoProcessor");
        return null;
    }
}
