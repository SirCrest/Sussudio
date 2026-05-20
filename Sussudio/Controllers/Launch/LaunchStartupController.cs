using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Sussudio.Controllers;

internal sealed class LaunchStartupControllerContext
{
    public required FrameworkElement MainContent { get; init; }
    public required RoutedEventHandler LoadedHandler { get; init; }
    public required Action ScheduleNativeShellRevealAfterFirstFrame { get; init; }
    public required Func<Func<Task>, string, Task> RunUiEventHandlerAsync { get; init; }
    public required Func<Task> InitializeViewModelAsync { get; init; }
    public required Action PrimePreviewAudioFadeIn { get; init; }
    public required Func<Task> RefreshDevicesAsync { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsPreviewFirstVisualConfirmed { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
    public required Action StartAutomationHost { get; init; }
    public required Action PlaySplashAndEntrance { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed class LaunchStartupController
{
    private readonly LaunchStartupControllerContext _context;

    public LaunchStartupController(LaunchStartupControllerContext context)
    {
        _context = context;
    }

    public void HandleLoaded(string operationName)
    {
        _context.MainContent.Loaded -= _context.LoadedHandler;
        _context.ScheduleNativeShellRevealAfterFirstFrame();

        _ = _context.RunUiEventHandlerAsync(async () =>
        {
            _context.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            try
            {
                await _context.InitializeViewModelAsync();
                // LoadSettings just pushed saved volume to CaptureService; re-prime it
                // so WASAPI playback starts silent and fades in only after live frames render.
                _context.PrimePreviewAudioFadeIn();
                await _context.RefreshDevicesAsync();
                if (!_context.IsPreviewing() && !_context.IsPreviewFirstVisualConfirmed())
                {
                    _context.RevealPreviewUnavailablePlaceholder();
                }
            }
            finally
            {
                _context.StartAutomationHost();
            }
        }, operationName);

        _context.PlaySplashAndEntrance();
    }
}
