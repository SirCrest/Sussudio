using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Sussudio
{
    // UI display helpers for compact source-signal labels and number formatting.
    internal static class DisplayFormatters
    {
        public static string FormatSourceHdr(bool? isHdr, string? colorimetry)
        {
            return isHdr switch
            {
                true when !string.IsNullOrWhiteSpace(colorimetry) => $"On ({colorimetry})",
                true => "On",
                false => "Off",
                _ => "\u2014"
            };
        }

        // numericFormat applies to the scaled value, e.g. "0.##" for "1.5 GB" or "0"
        // for the recording size readout where a whole-number look is preferred.
        public static string FormatBytes(long bytes, string numericFormat = "0.##")
        {
            if (bytes < 0)
            {
                bytes = 0;
            }

            const double scale = 1024;
            double value = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            var unit = 0;
            while (value >= scale && unit < units.Length - 1)
            {
                value /= scale;
                unit++;
            }

            return $"{value.ToString(numericFormat, System.Globalization.CultureInfo.InvariantCulture)} {units[unit]}";
        }

        public static string FormatBitrate(double bitsPerSecond)
        {
            if (bitsPerSecond <= 0)
            {
                return "0 bps";
            }

            string[] units = { "bps", "Kbps", "Mbps", "Gbps" };
            var unit = 0;
            while (bitsPerSecond >= 1000 && unit < units.Length - 1)
            {
                bitsPerSecond /= 1000;
                unit++;
            }

            return $"{Math.Round(bitsPerSecond):0} {units[unit]}";
        }
    }
}

namespace Sussudio.Converters
{
    // Small XAML converter set used by the hand-bound WinUI controls.
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return value;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }

            return false;
        }
    }

    public class BoolToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }

            return true;
        }
    }
}
