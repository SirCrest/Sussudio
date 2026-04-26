using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture;

public sealed partial class MainWindow
{
    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            RefreshButton.Content = new Microsoft.UI.Xaml.Controls.ProgressRing { Width = 16, Height = 16, IsActive = true };
            RefreshButton.IsEnabled = false;
            try
            {
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                RefreshButton.Content = new Microsoft.UI.Xaml.Controls.FontIcon { Glyph = "\uE72C", FontSize = 14 };
                RefreshButton.IsEnabled = true;
            }
        }, nameof(RefreshButton_Click));
    }
    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            if (ViewModel.IsPreviewReinitializing && !ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                ViewModel.CancelPendingPreviewRestart();
                Logger.Log($"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_previewStartupAttemptId ?? "none"}");
                return;
            }

            if (ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                StopPreviewFadeInTimer();
                await AnimatePreviewOutAsync();
                try
                {
                    await ViewModel.StopPreviewAsync(userInitiated: true);
                }
                finally
                {
                    _isPreviewReinitAnimating = false;
                    ResetPreviewContentTransform();
                }
            }
            else
            {
                _previewStopRequestedByUser = false;
                await ViewModel.StartPreviewAsync(userInitiated: true);
            }
        }, nameof(PreviewButton_Click));
    }
    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            await ViewModel.ToggleRecordingAsync();

            if (ViewModel.IsRecording)
            {
                var gpuActive = _d3dRenderer != null && PreviewSwapChainPanel.Visibility == Visibility.Visible;
                var cpuActive = _previewSource != null && PreviewImage.Visibility == Visibility.Visible;
                var rendererActive = gpuActive || cpuActive;
                var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
                Logger.Log(
                    $"PreviewStateDuringRecording: rendererActive={rendererActive}, " +
                    $"gpuActive={gpuActive}, cpuActive={cpuActive}, " +
                    $"placeholderVisible={placeholderVisible}");

                if (!rendererActive || placeholderVisible)
                {
                    Logger.Log("WARNING: preview renderer appears inactive while recording.");
                }
            }
        }, nameof(RecordButton_Click));
    }
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.BrowseOutputPathAsync(), nameof(BrowseButton_Click));
    }
    private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)
    {
        var path = ViewModel.OutputPath;
        if (!string.IsNullOrWhiteSpace(path) && System.IO.Directory.Exists(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
    }
    private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            if (!ViewModel.IsPreviewing)
            {
                ViewModel.StatusText = "Start preview before capturing a screenshot";
                return;
            }

            var outputDir = ViewModel.OutputPath;
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ElgatoCapture");

            Directory.CreateDirectory(outputDir);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filePath = Path.Combine(outputDir, $"Screenshot_{timestamp}.png");

            ScreenshotButton.IsEnabled = false;
            try
            {
                var result = await ViewModel.CapturePreviewFrameAsync(filePath);
                if (result.Succeeded)
                {
                    ViewModel.StatusText = $"Screenshot saved: {Path.GetFileName(filePath)}";
                    Logger.Log($"SCREENSHOT_SAVED path={filePath} width={result.CapturedWidth} height={result.CapturedHeight}");
                }
                else
                {
                    ViewModel.StatusText = $"Screenshot failed: {result.Message}";
                    Logger.Log($"SCREENSHOT_FAILED reason={result.Message}");
                }
            }
            finally
            {
                ScreenshotButton.IsEnabled = true;
            }
        }, nameof(ScreenshotButton_Click));
    }
    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSettingsShelfAnimating)
        {
            return;
        }

        if (SettingsOverlayPanel.Visibility == Visibility.Visible)
        {
            HideSettingsShelf();
        }
        else
        {
            ShowSettingsShelf();
        }
    }
}
