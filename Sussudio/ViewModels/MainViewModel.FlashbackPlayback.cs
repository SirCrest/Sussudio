using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback playback snapshot access plus rejection status projection.
/// </summary>
public partial class MainViewModel
{
    internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()
        => _sessionCoordinator.GetFlashbackPlaybackSnapshot();

    public void ReportFlashbackPlaybackRejection(string action, string logToken)
    {
        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        var lastFailure = string.IsNullOrWhiteSpace(playback.LastCommandFailure)
            ? "none"
            : playback.LastCommandFailure;
        var message =
            $"Flashback {action} rejected (state={playback.State}, " +
            $"threadAlive={playback.ThreadAlive}, pending={playback.PendingCommands}, " +
            $"lastFailure={lastFailure}).";

        Logger.Log(
            $"{logToken} state={playback.State} threadAlive={playback.ThreadAlive} " +
            $"pending={playback.PendingCommands} lastFailure='{lastFailure}' " +
            $"failureUtc={playback.LastCommandFailureUtcUnixMs}");
        StatusText = message;
    }
}
