using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

// Read-only automation probes and preview-frame capture helpers. These methods
// report the active pipeline without mutating capture lifecycle state.
public partial class CaptureService
{
    private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;
    private const int PreviewFrameCaptureRendererPollMs = 50;

    public VideoSourceProbeResult ProbeVideoSource()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture == null)
        {
            return new VideoSourceProbeResult
            {
                SessionActive = false,
                MemoryPreference = "Unknown"
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        var fps = Math.Round(unifiedVideoCapture.Fps, 3);
        return new VideoSourceProbeResult
        {
            SessionActive = true,
            MemoryPreference = unifiedVideoCapture.IsP010 ? "Auto" : "Cpu",
            CurrentSubtype = subtype,
            CurrentWidth = unifiedVideoCapture.Width,
            CurrentHeight = unifiedVideoCapture.Height,
            CurrentFrameRate = fps,
            P010Available = unifiedVideoCapture.IsP010,
            Nv12Available = !unifiedVideoCapture.IsP010,
            SupportedSubtypes = new[] { subtype },
            TotalFormatCount = 1,
            Formats = new[]
            {
                new VideoSourceFormatEntry
                {
                    Subtype = subtype,
                    Width = unifiedVideoCapture.Width,
                    Height = unifiedVideoCapture.Height,
                    FrameRate = fps,
                    Summary = $"{subtype} {unifiedVideoCapture.Width}x{unifiedVideoCapture.Height}@{fps:0.###}"
                }
            }
        };
    }

    public PreviewColorProbeResult ProbePreviewColor()
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        var d3dSink = _videoPipeline.PreviewFrameSink as D3D11PreviewRenderer;
        var d3dInputColor = d3dSink?.InputColorSpaceLabel ?? "None";
        var d3dOutputColor = d3dSink?.OutputColorSpaceLabel ?? "None";
        if (unifiedVideoCapture == null)
        {
            return new PreviewColorProbeResult
            {
                SessionActive = false,
                D3DInputColorSpace = d3dInputColor,
                D3DOutputColorSpace = d3dOutputColor
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        return new PreviewColorProbeResult
        {
            SessionActive = true,
            RendererMode = d3dSink?.RendererMode ?? "None",
            NegotiatedSubtype = subtype,
            SourceWidth = unifiedVideoCapture.Width,
            SourceHeight = unifiedVideoCapture.Height,
            SourceFrameRate = Math.Round(unifiedVideoCapture.Fps, 3),
            D3DInputColorSpace = d3dInputColor,
            D3DOutputColorSpace = d3dOutputColor
        };
    }

    public async Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var waitStartedAt = Stopwatch.GetTimestamp();
        while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)
        {
            var d3dSink = _videoPipeline.PreviewFrameSink as D3D11PreviewRenderer;
            if (d3dSink is { IsRendering: true })
            {
                return await d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);
            }

            if (Stopwatch.GetElapsedTime(waitStartedAt).TotalMilliseconds >= PreviewFrameCaptureRendererWaitTimeoutMs)
            {
                break;
            }

            try
            {
                await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        return new PreviewFrameCaptureResult
        {
            Succeeded = false,
            Message = "No active preview renderer."
        };
    }
}