using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public void EnsureResolutionSelection()
    {
        if (_context.ViewModel.AvailableResolutions.Count == 0)
        {
            if (_context.ViewModel.SelectedDevice == null || !_context.ViewModel.IsPreviewing)
            {
                _context.ResolutionComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingResolution = CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(
            _context.ViewModel.AvailableResolutions,
            _context.ViewModel.SelectedResolution);
        if (matchingResolution == null)
        {
            return;
        }

        if (!string.Equals(matchingResolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
        {
            _context.ViewModel.SelectedResolution = matchingResolution.Value;
        }

        if (_context.ResolutionComboBox.SelectedItem is not ResolutionOption selectedResolutionOption ||
            !string.Equals(selectedResolutionOption.Value, matchingResolution.Value, StringComparison.OrdinalIgnoreCase))
        {
            _context.ResolutionComboBox.SelectedItem = matchingResolution;
        }
    }

    public void EnsureFrameRateSelection()
    {
        if (_context.ViewModel.AvailableFrameRates.Count == 0)
        {
            if (_context.ViewModel.SelectedDevice == null || !_context.ViewModel.IsPreviewing)
            {
                _context.FrameRateComboBox.SelectedItem = null;
            }

            return;
        }

        if (_context.ViewModel.IsAutoFrameRateSelected)
        {
            var autoOption = CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(
                _context.ViewModel.AvailableFrameRates,
                _context.ViewModel.SelectedFrameRate,
                isAutoFrameRateSelected: true);
            if (autoOption != null && CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(autoOption))
            {
                if (!ReferenceEquals(_context.FrameRateComboBox.SelectedItem, autoOption))
                {
                    _context.FrameRateComboBox.SelectedItem = autoOption;
                }

                return;
            }
        }

        var matchingRate = CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(
            _context.ViewModel.AvailableFrameRates,
            _context.ViewModel.SelectedFrameRate,
            isAutoFrameRateSelected: false);
        if (matchingRate == null)
        {
            return;
        }

        if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(matchingRate.Value, _context.ViewModel.SelectedFrameRate))
        {
            _context.ViewModel.SelectedFrameRate = matchingRate.Value;
        }

        if (_context.FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
            !CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(currentFps.Value, matchingRate.Value))
        {
            _context.FrameRateComboBox.SelectedItem = matchingRate;
        }
    }
}
