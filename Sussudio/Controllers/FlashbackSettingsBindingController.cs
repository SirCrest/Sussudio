using System;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class FlashbackSettingsBindingControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleSwitch FlashbackEnabledToggle { get; init; }
    public required ToggleSwitch FlashbackGpuDecodeToggle { get; init; }
    public required ComboBox FlashbackBufferDurationCombo { get; init; }
    public required Action ApplyFlashbackTimelineLockout { get; init; }
}

internal sealed class FlashbackSettingsBindingController
{
    private readonly FlashbackSettingsBindingControllerContext _context;

    public FlashbackSettingsBindingController(FlashbackSettingsBindingControllerContext context)
    {
        _context = context;
    }

    public void ApplyInitialSettings()
    {
        _context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;
        _context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;
        _context.ApplyFlashbackTimelineLockout();
        SyncBufferDurationSelection();
    }

    public void AttachBindings()
    {
        _context.FlashbackGpuDecodeToggle.Toggled += (s, e) =>
            _context.ViewModel.FlashbackGpuDecode = _context.FlashbackGpuDecodeToggle.IsOn;
    }

    public void SyncGpuDecodeToggle()
    {
        if (_context.FlashbackGpuDecodeToggle.IsOn != _context.ViewModel.FlashbackGpuDecode)
        {
            _context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;
        }
    }

    public void SyncBufferDurationSelection()
    {
        var selectedMinutes = _context.ViewModel.FlashbackBufferMinutes.ToString();
        if (_context.FlashbackBufferDurationCombo.SelectedItem is ComboBoxItem current &&
            current.Tag is string currentTag &&
            currentTag == selectedMinutes)
        {
            return;
        }

        foreach (ComboBoxItem item in _context.FlashbackBufferDurationCombo.Items)
        {
            if (item.Tag is string tag && tag == selectedMinutes)
            {
                _context.FlashbackBufferDurationCombo.SelectedItem = item;
                break;
            }
        }
    }

    public void HandleBufferDurationSelectionChanged()
    {
        if (_context.FlashbackBufferDurationCombo.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag &&
            int.TryParse(tag, out var minutes))
        {
            _context.ViewModel.FlashbackBufferMinutes = minutes;
            Sussudio.Logger.Log($"FLASHBACK_UI_BUFFER_DURATION_CHANGED minutes={minutes}");
        }
    }
}
