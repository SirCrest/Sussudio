using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutator for recording container/codec selection.
/// </summary>
public partial class MainViewModel
{
    public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        var recordingFormat = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableRecordingFormats.FirstOrDefault(value =>
                string.Equals(value, format, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Recording format '{format}' is not available.");
            }
            if (IsHdrEnabled && !RecordingFormatSelectionPolicy.IsHdrCompatible(matched))
            {
                throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
            }

            _suppressFlashbackFormatCycle = true;
            try
            {
                SelectedRecordingFormat = matched;
            }
            finally
            {
                _suppressFlashbackFormatCycle = false;
            }

            return matched switch
            {
                "HEVC" => RecordingFormat.HevcMp4,
                "AV1" => RecordingFormat.Av1Mp4,
                _ => RecordingFormat.H264Mp4
            };
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)
            .ConfigureAwait(false);
    }
}
