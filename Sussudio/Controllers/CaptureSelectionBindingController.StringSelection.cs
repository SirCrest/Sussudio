using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    private static void ApplyStringComboBoxSelection(
        ComboBox comboBox,
        ObservableCollection<string> items,
        Func<string?> getVmProp,
        Action<string> setVmProp)
    {
        if (items.Count == 0)
        {
            comboBox.SelectedItem = null;
            return;
        }

        var vmValue = getVmProp();
        var match = CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);
        if (match == null)
        {
            return;
        }

        if (!string.Equals(match, vmValue, StringComparison.OrdinalIgnoreCase))
        {
            setVmProp(match);
        }

        if (!string.Equals(comboBox.SelectedItem as string, match, StringComparison.OrdinalIgnoreCase))
        {
            comboBox.SelectedItem = match;
        }
    }
}
