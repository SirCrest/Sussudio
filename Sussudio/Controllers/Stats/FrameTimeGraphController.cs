using System;
using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Sussudio.Services.Preview;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Sussudio.Controllers;

internal delegate PreviewFrameTimeHistoryRead CopyFrameTimeSamples(
    PreviewFrameTimeCursor cursor, Span<PreviewFrameTimeSample> destination);

internal sealed class FrameTimeGraphControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required FrameworkElement FpsHost { get; init; }
    public required FrameworkElement FrameTimeHost { get; init; }
    public required CopyFrameTimeSamples CopySamples { get; init; }
    public Action<string>? SetHistoryStatus { get; init; }
    public Action<string, string>? SetScaleLabels { get; init; }
    public Action<string>? Log { get; init; }
}

// Geometry changes only when measurements arrive or the scale/layout changes.
// One compositor clock scrolls both panels between those updates.
internal sealed class FrameTimeGraphController : IDisposable
{
    private const int DrainBatchSize = 512;
    private const int MaximumDrainBatches = PreviewFrameTimeHistory.DefaultCapacity / DrainBatchSize;
    private const double RebaseSeconds = 60;
    private readonly FrameTimeGraphControllerContext _context;
    private readonly DispatcherQueueTimer _timer;
    private readonly PreviewFrameTimeHistory _history = new();
    private readonly PreviewFrameTimeSample[] _drainBuffer = new PreviewFrameTimeSample[DrainBatchSize];
    private readonly PreviewFrameTimeSample[] _rebuildBuffer = new PreviewFrameTimeSample[PreviewFrameTimeHistory.DefaultCapacity];
    private readonly UISettings _uiSettings = new();
    private readonly bool _hasAnimationSettingEvent;
    private PreviewFrameTimeCursor _cursor;
    private PreviewFrameTimeSample? _previous;
    private FrameTimeGraphScale _scale = FrameTimeGraphScale.FromExpectedFps(60);
    private GraphSurface? _fps;
    private GraphSurface? _frameTime;
    private CompositionPropertySet? _clock;
    private ScalarKeyFrameAnimation? _scrollAnimation;
    private long _originQpc;
    private long _lastGapQpc;
    private long _pausedQpc;
    private bool _active;
    private bool _animationsEnabled;
    private bool _disposed;

    public FrameTimeGraphController(FrameTimeGraphControllerContext context)
    {
        _context = context;
        _timer = context.DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.IsRepeating = true;
        _timer.Tick += OnTick;
        context.FpsHost.SizeChanged += OnSizeChanged;
        context.FrameTimeHost.SizeChanged += OnSizeChanged;
        context.FpsHost.Loaded += OnLoaded;
        context.FrameTimeHost.Loaded += OnLoaded;
        context.FpsHost.Unloaded += OnUnloaded;
        context.FrameTimeHost.Unloaded += OnUnloaded;
        _animationsEnabled = _uiSettings.AnimationsEnabled;
        _hasAnimationSettingEvent = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) &&
            ApiInformation.IsEventPresent("Windows.UI.ViewManagement.UISettings", "AnimationsEnabledChanged");
        if (_hasAnimationSettingEvent && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            _uiSettings.AnimationsEnabledChanged += OnAnimationsEnabledChanged;
        PublishScaleLabels();
        _context.SetHistoryStatus?.Invoke(HistoryStatus);
    }

    public string HistoryStatus { get; private set; } = "No preview frames · 10 s history";

    public void SetActive(bool active)
    {
        if (_disposed) return;
        if (_active == active)
        {
            if (!active) EnsureSurfaces();
            return;
        }
        _active = active;
        if (!active)
        {
            _pausedQpc = Stopwatch.GetTimestamp();
            _timer.Stop();
            FreezeClock();
            PublishStatus(_fps?.ActiveCount > 0
                ? "Preview inactive · history paused" : "No preview frames · 10 s history");
            return;
        }

        _pausedQpc = 0;
        _animationsEnabled = _uiSettings.AnimationsEnabled;
        if (EnsureSurfaces())
        {
            Rebuild(Stopwatch.GetTimestamp());
            Drain();
        }
        _timer.Start();
    }

    public void SetExpectedFrameRate(double expectedFps)
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(expectedFps);
        if (Math.Abs(scale.ExpectedFps - _scale.ExpectedFps) < 0.001) return;
        _scale = scale;
        PublishScaleLabels();
        if (EnsureSurfaces()) Rebuild(Stopwatch.GetTimestamp());
    }

    private bool EnsureSurfaces()
    {
        if (_disposed || !_context.FpsHost.IsLoaded || !_context.FrameTimeHost.IsLoaded ||
            _context.FpsHost.ActualWidth <= 1 || _context.FrameTimeHost.ActualWidth <= 1)
            return false;
        if (_fps != null) return true;

        var compositor = ElementCompositionPreview.GetElementVisual(_context.FpsHost).Compositor;
        _clock = compositor.CreatePropertySet();
        _clock.InsertScalar("Seconds", 0);
        _scrollAnimation = compositor.CreateScalarKeyFrameAnimation();
        _scrollAnimation.InsertKeyFrame(0, 0);
        _scrollAnimation.InsertKeyFrame(1, (float)(RebaseSeconds * 2), compositor.CreateLinearEasingFunction());
        _scrollAnimation.Duration = TimeSpan.FromSeconds(RebaseSeconds * 2);
        _fps = new GraphSurface(_context.FpsHost, compositor, _clock, FrameTimeGraphPanel.FramesPerSecond);
        _frameTime = new GraphSurface(_context.FrameTimeHost, compositor, _clock, FrameTimeGraphPanel.FrameTime);
        Rebuild(Stopwatch.GetTimestamp());
        return true;
    }

    private void OnTick(DispatcherQueueTimer sender, object args)
    {
        if (!_active || _disposed) return;
        try
        {
            if (EnsureSurfaces()) Drain();
        }
        catch (Exception ex)
        {
            SetActive(false);
            _context.Log?.Invoke($"FRAME_TIME_GRAPH_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void Drain()
    {
        var now = Stopwatch.GetTimestamp();
        for (var batch = 0; batch < MaximumDrainBatches; batch++)
        {
            var read = _context.CopySamples(_cursor, _drainBuffer);
            if (read.Cursor.Epoch != _cursor.Epoch)
            {
                _history.Reset();
                _previous = null;
                _lastGapQpc = 0;
                _fps!.Clear();
                _frameTime!.Clear();
            }
            _cursor = read.Cursor;
            for (var i = 0; i < read.Count; i++)
            {
                var incoming = _drainBuffer[i];
                var flags = incoming.Flags;
                if (i == 0 && read.GapBeforeFirst) flags |= PreviewFrameTimeSampleFlags.GapBefore;
                var sample = _history.Append(incoming.TimestampQpc, incoming.IntervalMs, flags);
                if ((flags & PreviewFrameTimeSampleFlags.GapBefore) != 0)
                    _lastGapQpc = sample.TimestampQpc;
                AddSample(sample, now);
            }
            if (read.Count < _drainBuffer.Length) break;
        }

        if ((now - _originQpc) / (double)Stopwatch.Frequency >= RebaseSeconds)
            Rebuild(now);
        else if (!_animationsEnabled)
            _clock!.InsertScalar("Seconds", (float)((now - _originQpc) / (double)Stopwatch.Frequency));

        var oldestVisible = now - (long)(Stopwatch.Frequency * PreviewFrameTimeHistory.WindowSeconds);
        _fps!.RemoveBefore(oldestVisible);
        _frameTime!.RemoveBefore(oldestVisible);
        UpdateStatus(oldestVisible);
    }

    private void AddSample(PreviewFrameTimeSample sample, long now)
    {
        if (FrameTimeGraphGeometry.TryProjectSegment(_previous, sample, _originQpc, Stopwatch.Frequency,
            _fps!.Width, _fps.Height, _scale, FrameTimeGraphPanel.FramesPerSecond, out var fpsSegment))
            _fps.Add(fpsSegment, sample.TimestampQpc, now);
        if (FrameTimeGraphGeometry.TryProjectSegment(_previous, sample, _originQpc, Stopwatch.Frequency,
            _frameTime!.Width, _frameTime.Height, _scale, FrameTimeGraphPanel.FrameTime, out var timeSegment))
            _frameTime.Add(timeSegment, sample.TimestampQpc, now);
        _previous = sample.IsCadenceSample ? sample : null;
    }

    private void Rebuild(long now)
    {
        if (_fps == null || _frameTime == null || _clock == null) return;
        if (!_active && _pausedQpc > 0) now = _pausedQpc;
        _clock.StopAnimation("Seconds");
        _clock.InsertScalar("Seconds", 0);
        _originQpc = now;
        _fps.Resize(_scale);
        _frameTime.Resize(_scale);
        _fps.Clear();
        _frameTime.Clear();
        _previous = null;
        var read = _history.CopyAfter(default, _rebuildBuffer);
        var oldestVisible = now - (long)(Stopwatch.Frequency * PreviewFrameTimeHistory.WindowSeconds);
        for (var i = 0; i < read.Count; i++)
        {
            if (_rebuildBuffer[i].TimestampQpc < oldestVisible) continue;
            AddSample(_rebuildBuffer[i], now);
        }
        if (_active && _animationsEnabled)
        {
            // Geometry rebuilds can take time. Start at the elapsed QPC offset so
            // the moving right edge still represents now after a resize/rebase.
            var elapsed = (float)((Stopwatch.GetTimestamp() - _originQpc) / (double)Stopwatch.Frequency);
            _scrollAnimation!.InsertKeyFrame(0, elapsed);
            using var easing = _clock.Compositor.CreateLinearEasingFunction();
            _scrollAnimation.InsertKeyFrame(1, elapsed + (float)(RebaseSeconds * 2), easing);
            _clock.StartAnimation("Seconds", _scrollAnimation);
        }
        UpdateStatus(oldestVisible);
    }

    private void UpdateStatus(long oldestVisible)
    {
        var status = _fps!.ActiveCount == 0 ? "No preview frames · 10 s history"
            : !_active ? "Preview inactive · history paused"
            : _fps.OutOfRangeCount + _frameTime!.OutOfRangeCount > 0
                ? "10 s history · amber marks values beyond scale"
                : _lastGapQpc >= oldestVisible && _lastGapQpc > 0
                    ? "10 s history · gaps are not interpolated"
                    : "10 s history · present-call cadence";
        PublishStatus(status);
    }

    private void PublishStatus(string status)
    {
        if (status == HistoryStatus) return;
        HistoryStatus = status;
        _context.SetHistoryStatus?.Invoke(status);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (EnsureSurfaces()) Rebuild(Stopwatch.GetTimestamp());
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (!EnsureSurfaces()) return;
        Rebuild(Stopwatch.GetTimestamp());
        if (_active) _timer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        _timer.Stop();
        FreezeClock();
    }

    private void OnAnimationsEnabledChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args)
    {
        _context.DispatcherQueue.TryEnqueue(() =>
        {
            if (_disposed) return;
            _animationsEnabled = _uiSettings.AnimationsEnabled;
            if (EnsureSurfaces()) Rebuild(Stopwatch.GetTimestamp());
        });
    }

    private void FreezeClock()
    {
        if (_clock == null) return;
        var elapsed = (float)((Stopwatch.GetTimestamp() - _originQpc) / (double)Stopwatch.Frequency);
        _clock.StopAnimation("Seconds");
        _clock.InsertScalar("Seconds", elapsed);
    }

    private void PublishScaleLabels()
        => _context.SetScaleLabels?.Invoke($"{_scale.ExpectedFps:0.##} fps target",
            $"{_scale.FrameBudgetMs:0.##} / {_scale.FrameBudgetMs * 2:0.##} / {_scale.FrameBudgetMs * 3:0.##} ms");

    public void Dispose()
    {
        if (_disposed) return;
        SetActive(false);
        _disposed = true;
        _timer.Tick -= OnTick;
        _context.FpsHost.SizeChanged -= OnSizeChanged;
        _context.FrameTimeHost.SizeChanged -= OnSizeChanged;
        _context.FpsHost.Loaded -= OnLoaded;
        _context.FrameTimeHost.Loaded -= OnLoaded;
        _context.FpsHost.Unloaded -= OnUnloaded;
        _context.FrameTimeHost.Unloaded -= OnUnloaded;
        if (_hasAnimationSettingEvent && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            _uiSettings.AnimationsEnabledChanged -= OnAnimationsEnabledChanged;
        _fps?.Dispose();
        _frameTime?.Dispose();
        _scrollAnimation?.Dispose();
        _clock?.Dispose();
    }

    private sealed class GraphSurface : IDisposable
    {
        private readonly FrameworkElement _host;
        private readonly Compositor _compositor;
        private readonly ContainerVisual _root;
        private readonly ShapeVisual _data;
        private readonly ShapeVisual _grid;
        private readonly ExpressionAnimation _scroll;
        private readonly CompositionColorBrush _lineBrush;
        private readonly CompositionColorBrush _warningBrush;
        private readonly CompositionColorBrush _gridBrush;
        private readonly FrameTimeGraphPanel _panel;
        private readonly LineSlot[] _slots = new LineSlot[PreviewFrameTimeHistory.DefaultCapacity];
        private readonly int[] _activeSlots = new int[PreviewFrameTimeHistory.DefaultCapacity];
        private readonly int[] _freeSlots = new int[PreviewFrameTimeHistory.DefaultCapacity];
        private readonly CompositionLineGeometry[] _budgetLines = new CompositionLineGeometry[3];
        private readonly CompositionSpriteShape[] _budgetShapes = new CompositionSpriteShape[3];
        private int _createdCount;
        private int _freeCount;
        private int _queueStart;

        public GraphSurface(FrameworkElement host, Compositor compositor, CompositionPropertySet clock,
            FrameTimeGraphPanel panel)
        {
            _host = host;
            _compositor = compositor;
            _panel = panel;
            _root = compositor.CreateContainerVisual();
            _root.Clip = compositor.CreateInsetClip();
            _grid = compositor.CreateShapeVisual();
            _data = compositor.CreateShapeVisual();
            _root.Children.InsertAtTop(_grid);
            _root.Children.InsertAtTop(_data);
            _lineBrush = compositor.CreateColorBrush(panel == FrameTimeGraphPanel.FrameTime
                ? Color.FromArgb(255, 85, 214, 255) : Color.FromArgb(255, 255, 213, 79));
            _warningBrush = compositor.CreateColorBrush(Color.FromArgb(255, 255, 183, 77));
            _gridBrush = compositor.CreateColorBrush(Color.FromArgb(75, 255, 255, 255));
            for (var i = 0; i < _budgetLines.Length; i++)
            {
                var geometry = compositor.CreateLineGeometry();
                _budgetLines[i] = geometry;
                var shape = compositor.CreateSpriteShape(geometry);
                _budgetShapes[i] = shape;
                shape.StrokeBrush = _gridBrush;
                shape.StrokeThickness = 1;
                _grid.Shapes.Add(shape);
            }
            _scroll = compositor.CreateExpressionAnimation("-clock.Seconds * pixelsPerSecond");
            _scroll.SetReferenceParameter("clock", clock);
            _scroll.SetScalarParameter("pixelsPerSecond", 1);
            _data.StartAnimation("Offset.X", _scroll);
            ElementCompositionPreview.SetElementChildVisual(host, _root);
        }

        public float Width { get; private set; }
        public float Height { get; private set; }
        public int ActiveCount { get; private set; }
        public int OutOfRangeCount { get; private set; }

        public void Resize(FrameTimeGraphScale scale)
        {
            Width = (float)Math.Max(1, _host.ActualWidth);
            Height = (float)Math.Max(1, _host.ActualHeight);
            _root.Size = _data.Size = _grid.Size = new(Width, Height);
            _scroll.SetScalarParameter("pixelsPerSecond", Width / (float)PreviewFrameTimeHistory.WindowSeconds);
            _data.StartAnimation("Offset.X", _scroll);
            for (var i = 0; i < _budgetLines.Length; i++)
            {
                var y = FrameTimeGraphGeometry.ProjectY(scale.FrameBudgetMs * (i + 1), scale, _panel, Height, out _);
                _budgetLines[i].Start = new(0, y);
                _budgetLines[i].End = new(Width, y);
            }
        }

        public void Add(FrameTimeGraphSegment segment, long timestamp, long now)
        {
            RemoveBefore(now - (long)(Stopwatch.Frequency * PreviewFrameTimeHistory.WindowSeconds));
            if (ActiveCount == _activeSlots.Length) RemoveFirst();
            var slotIndex = _freeCount > 0 ? _freeSlots[--_freeCount] : _createdCount++;
            ref var slot = ref _slots[slotIndex];
            if (slot.Geometry == null)
            {
                slot.Geometry = _compositor.CreateLineGeometry();
                slot.Shape = _compositor.CreateSpriteShape(slot.Geometry);
                _data.Shapes.Add(slot.Shape);
            }
            slot.Geometry.Start = segment.Start;
            slot.Geometry.End = segment.End;
            slot.Shape!.StrokeThickness = 1.5f;
            slot.Shape.StrokeBrush = segment.OutOfRange ? _warningBrush : _lineBrush;
            slot.Timestamp = timestamp;
            slot.OutOfRange = segment.OutOfRange;
            if (segment.OutOfRange) OutOfRangeCount++;
            _activeSlots[(_queueStart + ActiveCount) % _activeSlots.Length] = slotIndex;
            ActiveCount++;
        }

        public void RemoveBefore(long timestamp)
        {
            while (ActiveCount > 0 && _slots[_activeSlots[_queueStart]].Timestamp < timestamp)
                RemoveFirst();
        }

        private void RemoveFirst()
        {
            var slotIndex = _activeSlots[_queueStart];
            ref var slot = ref _slots[slotIndex];
            slot.Shape!.StrokeThickness = 0;
            if (slot.OutOfRange) OutOfRangeCount--;
            _freeSlots[_freeCount++] = slotIndex;
            _queueStart = (_queueStart + 1) % _activeSlots.Length;
            ActiveCount--;
        }

        public void Clear()
        {
            while (ActiveCount > 0) RemoveFirst();
        }

        public void Dispose()
        {
            ElementCompositionPreview.SetElementChildVisual(_host, null);
            _data.StopAnimation("Offset.X");
            _data.Shapes.Clear();
            _grid.Shapes.Clear();
            for (var i = 0; i < _createdCount; i++)
            {
                _slots[i].Shape?.Dispose();
                _slots[i].Geometry?.Dispose();
            }
            foreach (var geometry in _budgetLines) geometry.Dispose();
            foreach (var shape in _budgetShapes) shape.Dispose();
            _scroll.Dispose();
            _lineBrush.Dispose();
            _warningBrush.Dispose();
            _gridBrush.Dispose();
            _data.Dispose();
            _grid.Dispose();
            _root.Clip?.Dispose();
            _root.Dispose();
        }

        private struct LineSlot
        {
            public CompositionLineGeometry? Geometry;
            public CompositionSpriteShape? Shape;
            public long Timestamp;
            public bool OutOfRange;
        }
    }
}
