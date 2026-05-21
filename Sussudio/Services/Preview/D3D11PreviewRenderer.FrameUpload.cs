using System;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)
    {
        inputView = null;
        disposeInputView = false;

        if (frame.D3DTexture != null)
        {
            inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);
            disposeInputView = true;
            return true;
        }

        if (_deviceContext == null || _inputTexture == null || _stagingTexture == null)
        {
            return false;
        }

        if (frame.RawData != null)
        {
            if (!UploadRawFrameToTexture(frame.RawData, frame.RawDataLength, frame.Width, frame.Height, frame.IsHdr, _stagingTexture, _inputTexture))
            {
                return false;
            }
        }
        else if (frame.FrameLease != null)
        {
            if (!UploadRawFrameToTexture(frame.FrameLease.Memory.Span, frame.Width, frame.Height, frame.IsHdr, _stagingTexture, _inputTexture))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        inputView = _inputView;
        return inputView != null;
    }

    private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)
    {
        if (_videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 preview pipeline is not ready for external texture input.");
        }

        var textureDescription = texture.Description;
        var mipLevels = Math.Max(1, (int)textureDescription.MipLevels);
        var arraySize = Math.Max(1, (int)textureDescription.ArraySize);
        var safeSubresource = Math.Max(0, subresourceIndex);
        var arraySlice = Math.Clamp(safeSubresource / mipLevels, 0, arraySize - 1);
        var mipSlice = Math.Clamp(safeSubresource % mipLevels, 0, mipLevels - 1);
        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView
            {
                MipSlice = (uint)mipSlice,
                ArraySlice = (uint)arraySlice
            }
        };

        return _videoDevice.CreateVideoProcessorInputView(texture, _videoProcessorEnumerator, inputViewDescription);
    }
}
