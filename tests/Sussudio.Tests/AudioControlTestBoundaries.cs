using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

// Passive boundaries for the complete linked AudioControlBindingController.cs.
// Setters deliver synchronous events; timers and storyboards advance only when
// tests explicitly raise them. These types do not emulate layout, interpolation,
// dispatcher affinity, audio routing, or any controller decision.
namespace Windows.Foundation
{
    public readonly record struct Size(double Width, double Height);
    public readonly record struct Rect(double X, double Y, double Width, double Height);
}

namespace Windows.UI
{
    public readonly record struct Color(byte A, byte R, byte G, byte B)
    {
        public static Color FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);
    }
}

namespace Microsoft.UI.Xaml
{
    public sealed class RoutedEvent { }

    public class UIElement
    {
        public static RoutedEvent PointerPressedEvent { get; } = new();
        private Input.PointerEventHandler? _handledPointerPressed;
        public void AddHandler(RoutedEvent routedEvent, object handler, bool handledEventsToo)
        {
            if (routedEvent == PointerPressedEvent && handledEventsToo)
                _handledPointerPressed += (Input.PointerEventHandler)handler;
        }
        protected void RaiseHandledPointerPressed()
            => _handledPointerPressed?.Invoke(this, new Input.PointerRoutedEventArgs());
        public double Opacity { get; set; } = 1;
        public Visibility Visibility { get; set; } = Visibility.Visible;
    }

    public class FrameworkElement : UIElement
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double ActualWidth { get; set; }
        public double ActualHeight { get; set; }
        public event EventHandler<SizeChangedEventArgs>? SizeChanged;

        public void RaiseSizeChanged(double width, double height)
        {
            ActualWidth = width;
            ActualHeight = height;
            SizeChanged?.Invoke(this, new SizeChangedEventArgs(new Windows.Foundation.Size(width, height)));
        }
    }

    public sealed class SizeChangedEventArgs(Windows.Foundation.Size newSize) : EventArgs
    {
        public Windows.Foundation.Size NewSize { get; } = newSize;
    }

    public readonly record struct Duration(TimeSpan TimeSpan)
    {
        public static implicit operator Duration(TimeSpan value) => new(value);
    }
}

namespace Microsoft.UI.Xaml.Input
{
    public sealed class PointerRoutedEventArgs : EventArgs { }
    public delegate void PointerEventHandler(object sender, PointerRoutedEventArgs args);
}

namespace Microsoft.UI.Xaml.Controls.Primitives
{
    public class ToggleButton : Microsoft.UI.Xaml.Controls.Control
    {
        private bool? _isChecked = false;
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                if (value == true) Checked?.Invoke(this, EventArgs.Empty);
                else if (value == false) Unchecked?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? Checked;
        public event EventHandler? Unchecked;
        public event EventHandler? Click;
        public void RaiseClick() => Click?.Invoke(this, EventArgs.Empty);
    }
}

namespace Microsoft.UI.Xaml.Controls
{
    public class Control : FrameworkElement
    {
        public bool IsEnabled { get; set; } = true;
    }

    public sealed class CheckBox : Primitives.ToggleButton { }

    public sealed class Slider : Control
    {
        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value)) return;
                var previous = _value;
                _value = value;
                ValueChanged?.Invoke(this, new ValueChangedEventArgs(previous, value));
            }
        }

        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
        public void RaisePointerPressed() => RaiseHandledPointerPressed();
        public event EventHandler? PointerCaptureLost;
        public void RaisePointerCaptureLost() => PointerCaptureLost?.Invoke(this, EventArgs.Empty);
    }

    public sealed class ValueChangedEventArgs(double oldValue, double newValue) : EventArgs
    {
        public double OldValue { get; } = oldValue;
        public double NewValue { get; } = newValue;
    }

    public sealed class ComboBox : Control
    {
        // Collection identity only: no native ItemsSource selection side effects.
        public object? ItemsSource { get; set; }
        public List<object> Items { get; } = new();
        private object? _selectedItem;
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (ReferenceEquals(_selectedItem, value)) return;
                _selectedItem = value;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? SelectionChanged;
        public void RaiseSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed class ToggleSwitch : Control
    {
        private bool _isOn;
        public bool IsOn
        {
            get => _isOn;
            set
            {
                if (_isOn == value) return;
                _isOn = value;
                Toggled?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? Toggled;
        public void RaiseToggled() => Toggled?.Invoke(this, EventArgs.Empty);
    }

    public sealed class TextBlock : FrameworkElement
    {
        public string Text { get; set; } = string.Empty;
    }

    public sealed class Grid : FrameworkElement { }

    public sealed class Border : FrameworkElement
    {
        public Media.Brush Background { get; set; } = new Media.LinearGradientBrush();
    }
}

namespace Microsoft.UI.Xaml.Media
{
    public abstract class Brush { }
    public sealed class LinearGradientBrush : Brush { }
    public sealed class SolidColorBrush(Windows.UI.Color color) : Brush
    {
        public Windows.UI.Color Color { get; } = color;
    }
    public sealed class RectangleGeometry
    {
        public Windows.Foundation.Rect Rect { get; set; }
    }
    public sealed class TranslateTransform
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}

namespace Microsoft.UI.Xaml.Media.Animation
{
    public enum EasingMode { EaseIn, EaseOut, EaseInOut }
    public abstract class EasingFunctionBase { }
    public sealed class CubicEase : EasingFunctionBase
    {
        public EasingMode EasingMode { get; set; }
    }

    public sealed class DoubleAnimation
    {
        public double? To { get; set; }
        public Duration Duration { get; set; }
        public EasingFunctionBase? EasingFunction { get; set; }
        public bool EnableDependentAnimation { get; set; }
        public object? Target { get; internal set; }
        public string? TargetProperty { get; internal set; }
    }

    public sealed class Storyboard
    {
        // Tests in the audio controller class own this recorder's lifetime.
        public static List<Storyboard> Created { get; } = new();
        public Storyboard() => Created.Add(this);
        public List<DoubleAnimation> Children { get; } = new();
        public int BeginCount { get; private set; }
        public int StopCount { get; private set; }
        public event EventHandler? Completed;
        public void Begin() => BeginCount++;
        public void Stop() => StopCount++;
        public void Complete() => Completed?.Invoke(this, EventArgs.Empty);
        public static void SetTarget(DoubleAnimation animation, object target) => animation.Target = target;
        public static void SetTargetProperty(DoubleAnimation animation, string property) => animation.TargetProperty = property;
    }
}

namespace Microsoft.UI.Xaml.Hosting
{
    public static class ElementCompositionPreview
    {
        private static readonly ConditionalWeakTable<UIElement, Visual> Visuals = new();
        public static Visual GetElementVisual(UIElement element) => Visuals.GetValue(element, _ => new Visual());
    }

    public sealed class Visual
    {
        public Compositor Compositor { get; } = new();
        public GeometricClip? Clip { get; set; }
    }
    public sealed class Compositor
    {
        public RoundedRectangleGeometry CreateRoundedRectangleGeometry() => new();
        public GeometricClip CreateGeometricClip(RoundedRectangleGeometry geometry) => new(geometry);
    }
    public sealed class RoundedRectangleGeometry
    {
        public Vector2 CornerRadius { get; set; }
        public Vector2 Size { get; set; }
    }
    public sealed class GeometricClip(RoundedRectangleGeometry geometry)
    {
        public RoundedRectangleGeometry Geometry { get; } = geometry;
    }
}

namespace Microsoft.UI.Dispatching
{
    public sealed partial class DispatcherQueue
    {
        public DispatcherQueueTimer? LastCreatedTimer { get; private set; }
        public DispatcherQueueTimer CreateTimer() => LastCreatedTimer = new DispatcherQueueTimer();
    }
    public sealed class DispatcherQueueTimer
    {
        public TimeSpan Interval { get; set; }
        public bool IsRepeating { get; set; }
        public bool IsRunning { get; private set; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public event EventHandler? Tick;
        public void Start() { StartCount++; IsRunning = true; }
        public void Stop() { StopCount++; IsRunning = false; }
        public void FireTick() => Tick?.Invoke(this, EventArgs.Empty);
    }
}

namespace Sussudio.Models
{
    public sealed class AudioInputDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
    public static class DeviceAudioMode
    {
        public const string Hdmi = "HDMI";
        public const string Analog = "Analog";
    }
}

namespace Sussudio.ViewModels
{
    public sealed partial class MainViewModel
    {
        public bool IsAudioEnabled { get; set; }
        public bool IsAudioPreviewEnabled { get; set; }
        public bool IsAudioPreviewActive { get; set; }
        public bool IsCustomAudioInputEnabled { get; set; }
        public bool IsMicrophoneEnabled { get; set; }
        public bool IsRecording { get; set; }
        public double AudioMeterTarget { get; set; }
        public double MicrophoneMeterTarget { get; set; }
        private double _previewVolume;
        private double _microphoneVolume;
        private double _analogAudioGainPercent;
        private Models.AudioInputDevice? _selectedAudioInputDevice;
        private Models.AudioInputDevice? _selectedMicrophoneDevice;
        private string _selectedDeviceAudioMode = Models.DeviceAudioMode.Hdmi;
        public int PreviewVolumeSetCount { get; private set; }
        public int MicrophoneVolumeSetCount { get; private set; }
        public int AnalogAudioGainPercentSetCount { get; private set; }
        public int SelectedAudioInputDeviceSetCount { get; private set; }
        public int SelectedMicrophoneDeviceSetCount { get; private set; }
        public int SelectedDeviceAudioModeSetCount { get; private set; }
        public double PreviewVolume { get => _previewVolume; set { PreviewVolumeSetCount++; _previewVolume = value; } }
        public double MicrophoneVolume { get => _microphoneVolume; set { MicrophoneVolumeSetCount++; _microphoneVolume = value; } }
        public double AnalogAudioGainPercent { get => _analogAudioGainPercent; set { AnalogAudioGainPercentSetCount++; _analogAudioGainPercent = value; } }
        public Models.AudioInputDevice? SelectedAudioInputDevice { get => _selectedAudioInputDevice; set { SelectedAudioInputDeviceSetCount++; _selectedAudioInputDevice = value; } }
        public Models.AudioInputDevice? SelectedMicrophoneDevice { get => _selectedMicrophoneDevice; set { SelectedMicrophoneDeviceSetCount++; _selectedMicrophoneDevice = value; } }
        public string SelectedDeviceAudioMode { get => _selectedDeviceAudioMode; set { SelectedDeviceAudioModeSetCount++; _selectedDeviceAudioMode = value; } }
        public int SavePreviewVolumeCount { get; private set; }
        public int SaveMicrophoneVolumeCount { get; private set; }
        public int ResetAudioMeterTimerFlagCount { get; private set; }
        public Action? OnSavePreviewVolume { get; set; }
        public Action<double>? OnUserPreviewVolume { get; set; }
        public void SetPreviewVolumeFromUser(double value)
        {
            PreviewVolume = value;
            OnUserPreviewVolume?.Invoke(value);
        }
        public void SavePreviewVolume() { SavePreviewVolumeCount++; OnSavePreviewVolume?.Invoke(); }
        public void SaveMicrophoneVolume() => SaveMicrophoneVolumeCount++;
        public void ResetAudioMeterTimerFlag() => ResetAudioMeterTimerFlagCount++;
        public event Action? AudioMeterActivated;
        public event Action? MicrophoneMeterActivated;
        public void RaiseAudioMeterActivated() => AudioMeterActivated?.Invoke();
        public void RaiseMicrophoneMeterActivated() => MicrophoneMeterActivated?.Invoke();
    }
}
