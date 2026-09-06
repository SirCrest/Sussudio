using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;

namespace Sussudio.Controllers;

// The outer dock reserves its final layout size once. Only the inner surface moves,
// so showing stats does not resize the preview on every animation frame.
internal sealed class StatsDockMotionController : IDisposable
{
    private readonly Border _layoutHost;
    private readonly FrameworkElement _motionSurface;
    private readonly Func<bool> _animationsEnabled;
    private readonly UISettings? _uiSettings;
    private readonly StatsDockMotionState _state = new();
    private Visual? _surfaceVisual;
    private Visual? _hostVisual;
    private InsetClip? _clip;
    private CompositionScopedBatch? _batch;
    private TypedEventHandler<object, CompositionBatchCompletedEventArgs>? _completedHandler;
    private bool _settingsEventAttached;
    private bool _disposed;

    public StatsDockMotionController(
        Border layoutHost,
        FrameworkElement motionSurface,
        Func<bool>? animationsEnabled = null)
    {
        _layoutHost = layoutHost ?? throw new ArgumentNullException(nameof(layoutHost));
        _motionSurface = motionSurface ?? throw new ArgumentNullException(nameof(motionSurface));
        if (animationsEnabled == null)
        {
            _uiSettings = new UISettings();
            _animationsEnabled = () => _uiSettings.AnimationsEnabled;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) &&
                ApiInformation.IsEventPresent("Windows.UI.ViewManagement.UISettings", "AnimationsEnabledChanged"))
            {
                _uiSettings.AnimationsEnabledChanged += AnimationsEnabledChanged;
                _settingsEventAttached = true;
            }
        }
        else
        {
            _animationsEnabled = animationsEnabled;
        }
    }

    public void Show(bool immediate = false) => SetVisible(true, immediate);

    public void Hide(bool immediate = false) => SetVisible(false, immediate);

    private void SetVisible(bool visible, bool immediate)
    {
        if (_disposed)
        {
            return;
        }

        var wasVisible = _layoutHost.Visibility == Visibility.Visible;
        var animate = !immediate && _animationsEnabled() && (visible || wasVisible);
        if (!immediate && _state.Visible == visible &&
            (_state.Animating || wasVisible == visible))
        {
            return;
        }

        var revision = _state.Request(visible, animate);
        StopAnimation();
        EnsureVisuals();
        if (!animate)
        {
            ApplyFinalState(visible);
            _state.TryComplete(revision);
            return;
        }

        _layoutHost.Visibility = Visibility.Visible;
        _layoutHost.IsHitTestVisible = visible;
        if (visible && !wasVisible)
        {
            _surfaceVisual!.Properties.InsertVector3("Translation", new Vector3(24, 0, 0));
            _surfaceVisual.Opacity = 0;
        }

        var visual = _surfaceVisual!;
        var compositor = visual.Compositor;
        using var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0), new Vector2(0, 1));
        using var translation = compositor.CreateVector3KeyFrameAnimation();
        using var opacity = compositor.CreateScalarKeyFrameAnimation();
        // Reversing a toggle continues from the compositor's current values.
        translation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
        opacity.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
        translation.InsertExpressionKeyFrame(0, "this.StartingValue");
        opacity.InsertExpressionKeyFrame(0, "this.StartingValue");
        translation.InsertKeyFrame(1, visible ? Vector3.Zero : new Vector3(24, 0, 0), easing);
        opacity.InsertKeyFrame(1, visible ? 1 : 0, easing);
        translation.Duration = opacity.Duration = TimeSpan.FromMilliseconds(visible ? 240 : 180);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        _batch = batch;
        _completedHandler = (sender, _) => _layoutHost.DispatcherQueue.TryEnqueue(() =>
        {
            if (_disposed || !ReferenceEquals(_batch, sender) || !_state.TryComplete(revision))
            {
                return;
            }

            StopAnimation();
            ApplyFinalState(visible);
        });
        batch.Completed += _completedHandler;
        visual.StartAnimation("Translation", translation);
        visual.StartAnimation("Opacity", opacity);
        batch.End();
    }

    private void EnsureVisuals()
    {
        if (_surfaceVisual != null)
        {
            return;
        }

        ElementCompositionPreview.SetIsTranslationEnabled(_motionSurface, true);
        _surfaceVisual = ElementCompositionPreview.GetElementVisual(_motionSurface);
        _hostVisual = ElementCompositionPreview.GetElementVisual(_layoutHost);
        _clip = _hostVisual.Compositor.CreateInsetClip();
        _hostVisual.Clip = _clip;
    }

    private void ApplyFinalState(bool visible)
    {
        _layoutHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        _layoutHost.IsHitTestVisible = visible;
        _surfaceVisual!.Properties.InsertVector3("Translation", Vector3.Zero);
        _surfaceVisual.Opacity = 1;
    }

    private void StopAnimation()
    {
        if (_batch != null)
        {
            if (_completedHandler != null)
            {
                _batch.Completed -= _completedHandler;
            }
            _batch.Dispose();
            _batch = null;
            _completedHandler = null;
        }
        _surfaceVisual?.StopAnimation("Translation");
        _surfaceVisual?.StopAnimation("Opacity");
    }

    private void AnimationsEnabledChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args)
    {
        _layoutHost.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_disposed && !_animationsEnabled())
            {
                SetVisible(_state.Visible, immediate: true);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _state.Cancel();
        StopAnimation();
        if (_settingsEventAttached && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
        {
            _uiSettings!.AnimationsEnabledChanged -= AnimationsEnabledChanged;
            _settingsEventAttached = false;
        }
        if (_hostVisual != null)
        {
            _hostVisual.Clip = null;
        }
        _clip?.Dispose();
        _clip = null;
        _surfaceVisual = null;
        _hostVisual = null;
    }
}

// A completion from an earlier show/hide request must not overwrite the latest state.
internal sealed class StatsDockMotionState
{
    private long _revision;
    public bool Visible { get; private set; }
    public bool Animating { get; private set; }

    public long Request(bool visible, bool animate)
    {
        Visible = visible;
        Animating = animate;
        return ++_revision;
    }

    public bool TryComplete(long revision)
    {
        if (revision != _revision)
        {
            return false;
        }
        Animating = false;
        return true;
    }

    public void Cancel()
    {
        ++_revision;
        Animating = false;
    }
}
