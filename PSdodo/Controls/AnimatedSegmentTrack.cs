using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace PsExporter.Controls;

/// <summary>
/// 分段控件的统一轨道。业务选择仍由原 Button.Command 执行；选中块只在绘制层移动。
/// </summary>
public sealed class AnimatedSegmentTrack : ContinuousBorder
{
    public static readonly StyledProperty<object?> SelectionValueProperty =
        AvaloniaProperty.Register<AnimatedSegmentTrack, object?>(nameof(SelectionValue));
    public static readonly AttachedProperty<object?> SelectionAliasProperty =
        AvaloniaProperty.RegisterAttached<AnimatedSegmentTrack, Button, object?>("SelectionAlias");
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty =
        AvaloniaProperty.Register<AnimatedSegmentTrack, IBrush?>(nameof(IndicatorBrush));
    public static readonly StyledProperty<IBrush?> IndicatorBorderBrushProperty =
        AvaloniaProperty.Register<AnimatedSegmentTrack, IBrush?>(nameof(IndicatorBorderBrush));
    public static readonly StyledProperty<IBrush?> HoverBrushProperty =
        AvaloniaProperty.Register<AnimatedSegmentTrack, IBrush?>(nameof(HoverBrush));

    public object? SelectionValue { get => GetValue(SelectionValueProperty); set => SetValue(SelectionValueProperty, value); }
    public IBrush? IndicatorBrush { get => GetValue(IndicatorBrushProperty); set => SetValue(IndicatorBrushProperty, value); }
    public IBrush? IndicatorBorderBrush { get => GetValue(IndicatorBorderBrushProperty); set => SetValue(IndicatorBorderBrushProperty, value); }
    public IBrush? HoverBrush { get => GetValue(HoverBrushProperty); set => SetValue(HoverBrushProperty, value); }
    public static object? GetSelectionAlias(Button button) => button.GetValue(SelectionAliasProperty);
    public static void SetSelectionAlias(Button button, object? value) => button.SetValue(SelectionAliasProperty, value);

    private readonly ContinuousCornerRenderer _indicatorRenderer = new();
    private readonly ContinuousCornerRenderer _hoverRenderer = new();
    private readonly WheelStepAccumulator _wheel = new();
    private readonly Stopwatch _motion = new();
    private Rect _from;
    private Rect _current;
    private Rect _target;
    private Size _layoutSize;
    private bool _hasSelection;
    private bool _animating;
    private bool _framePending;
    private int _generation;
    private double _compression;
    private int _hoverIndex = -1;
    private const double DurationMs = 240;

    public AnimatedSegmentTrack()
    {
        AddHandler(PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
        AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble);
        PointerExited += (_, _) =>
        {
            _wheel.Reset();
            _hoverIndex = -1;
            InvalidateVisual();
        };
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        var options = Options;
        var index = Array.FindIndex(options, b => b.IsEffectivelyEnabled
            && b.TranslatePoint(default, this) is { } origin && new Rect(origin, b.Bounds.Size).Contains(position));
        if (_hoverIndex == index) return;
        _hoverIndex = index;
        InvalidateVisual();
    }

    private Button[] Options => Child is Panel panel ? panel.Children.OfType<Button>().ToArray() : [];

    private int SelectedIndex(Button[] options)
        => Array.FindIndex(options, b => SameValue(SelectionValue, b.CommandParameter)
            || (GetSelectionAlias(b) is { } alias && SameValue(SelectionValue, alias)));

    private static bool SameValue(object? left, object? right)
        => left is not null && right is not null
            && string.Equals(Convert.ToString(left, CultureInfo.InvariantCulture),
                Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!IsEffectivelyEnabled) return;
        var options = Options;
        if (!options.Any(CanSelect)) return;
        // 轨道边界也消费滚轮，防止选到头以后突然滚动整个设置页。
        e.Handled = true;
        var steps = _wheel.Add(e.Delta);
        if (steps == 0) return;
        var selected = SelectedIndex(options);
        if (selected < 0) return;
        var next = selected;
        var direction = Math.Sign(steps);
        for (var step = 0; step < Math.Abs(steps); step++)
        {
            var candidate = next + direction;
            while (candidate >= 0 && candidate < options.Length && !CanSelect(options[candidate]))
                candidate += direction;
            if (candidate < 0 || candidate >= options.Length) break;
            next = candidate;
        }
        if (next != selected)
        {
            var button = options[next];
            button.Command!.Execute(button.CommandParameter);
        }
        else MoveToSelection(feedback: true);
    }

    private static bool CanSelect(Button button)
        => button.IsVisible && button.IsEffectivelyEnabled
            && button.Command?.CanExecute(button.CommandParameter) == true;

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        // Click 路由可能早于 Command 执行；下一次 UI 调度再读取真实选择。
        Dispatcher.UIThread.Post(() =>
        {
            if (IsEffectivelyVisible) MoveToSelection(feedback: true);
        });
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectionValueProperty) MoveToSelection();
        if (change.Property == IndicatorBrushProperty || change.Property == IndicatorBorderBrushProperty
            || change.Property == HoverBrushProperty)
            InvalidateVisual();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        var resized = _layoutSize != finalSize;
        _layoutSize = finalSize;
        MoveToSelection(immediate: resized || !IsEffectivelyVisible);
        return result;
    }

    private void MoveToSelection(bool feedback = false, bool immediate = false)
    {
        var options = Options;
        var index = SelectedIndex(options);
        if (index < 0 || options[index].Bounds.Width <= 0) return;
        var button = options[index];
        if (button.TranslatePoint(default, this) is not { } origin) return;
        var target = new Rect(origin, button.Bounds.Size);
        if (_hasSelection && target == _target && !feedback && !immediate) return;
        _target = target;
        if (!_hasSelection || immediate || !IsEffectivelyVisible || TopLevel.GetTopLevel(this) is null)
        {
            _current = target;
            _hasSelection = true;
            _animating = false;
            _compression = 0;
            InvalidateVisual();
            return;
        }
        // 快速点击或反向滚轮直接从当前绘制位置转向，不等待上一段动画。
        _from = _current;
        _motion.Restart();
        _animating = true;
        RequestFrame();
    }

    private void RequestFrame()
    {
        if (_framePending || TopLevel.GetTopLevel(this) is not { } topLevel) return;
        _framePending = true;
        var generation = _generation;
        topLevel.RequestAnimationFrame(_ =>
        {
            if (generation != _generation) return;
            _framePending = false;
            if (!_animating) return;
            if (!IsEffectivelyVisible)
            {
                _current = _target;
                _animating = false;
                _compression = 0;
                return;
            }
            var t = Math.Clamp(_motion.Elapsed.TotalMilliseconds / DurationMs, 0, 1);
            var x = t - 1;
            var eased = 1 + 2.15 * x * x * x + 1.15 * x * x;
            _current = new Rect(
                Lerp(_from.X, _target.X, eased), Lerp(_from.Y, _target.Y, eased),
                Math.Max(1, Lerp(_from.Width, _target.Width, eased)),
                Math.Max(1, Lerp(_from.Height, _target.Height, eased)));
            _compression = 0.045 * Math.Sin(Math.PI * t);
            if (t >= 1)
            {
                _current = _target;
                _animating = false;
                _compression = 0;
                _motion.Stop();
            }
            InvalidateVisual();
            if (_animating) RequestFrame();
        });
    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var options = Options;
        if (_hoverIndex >= 0 && _hoverIndex < options.Length
            && options[_hoverIndex].TranslatePoint(default, this) is { } origin)
        {
            using (context.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
                _hoverRenderer.Render(context, options[_hoverIndex].Bounds.Size, new CornerRadius(10),
                    default, HoverBrush, null);
        }
        if (!_hasSelection) return;
        var height = _current.Height * (1 - _compression);
        using (context.PushTransform(Matrix.CreateTranslation(_current.X, _current.Y + (_current.Height - height) / 2)))
            _indicatorRenderer.Render(context, new Size(_current.Width, height), new CornerRadius(10),
                new Thickness(1), IndicatorBrush, IndicatorBorderBrush);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _generation++;
        _framePending = false;
        _animating = false;
        _hasSelection = false;
        _motion.Reset();
        _wheel.Reset();
        base.OnDetachedFromVisualTree(e);
    }
}
