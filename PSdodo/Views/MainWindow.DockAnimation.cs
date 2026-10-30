using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using Avalonia.Rendering.Composition;
using PsExporter.Services;
using PsExporter.ViewModels;
using SkiaSharp;

namespace PsExporter.Views;

public partial class MainWindow
{
    private readonly DockAnimationMotion _edgeMotion = new();
    private EdgeSnapHelper.GenieMeshData? _animationTopology;
    private int _edgeMotionRevision;
    private bool _edgeCanReverseOnPointerReturn;
    private readonly Stopwatch _edgePlaybackClock = new();
    private double _edgeCaptureMs;
    private double _edgeReadyMs;
    private double _edgePrepareMs;
    private double _edgeMaxUpdateMs;
    private int _edgeLongIntervals;
    private bool _edgeFirstDrawConfirmed;
    private string _edgeCaptureSource = "fresh";
    private bool _edgeLiveWindowPrepared;
    private bool? _edgeSavedHitTestVisible;
    private bool _dockRevealBitmapStale = true;
    private bool _edgeVisualSettling;
    private int _edgeSceneRevision;
    private int _edgeCapturedSceneRevision;
    private int _retractSnapshotSceneRevision;
    private readonly Stopwatch _edgeGeometryClock = new();
    private PixelPoint _edgeCanvasPosition;
    private Vector _edgeMeshOrigin;
    private Vector _edgeSourceOffset;
    private double _edgeCanvasWidth;
    private double _edgeCanvasHeight;
    private double _edgeLivePrepareMs;
    private SKBitmap? _retractSnapshot;
    private double _retractSnapshotScale;
    private double _retractSnapshotWidth;
    private double _retractSnapshotHeight;
    private double _retractSnapshotCaptureMs;
    private int _retractSnapshotRevision;
    private bool _retractSnapshotQueued;

    private void InvalidateRetractSnapshot()
    {
        ++_retractSnapshotRevision;
        _retractSnapshot?.Dispose();
        _retractSnapshot = null;
    }

    private void QueueRetractSnapshot()
    {
        if (_retractSnapshot is not null || _retractSnapshotQueued) return;
        var delay = DataContext is MainViewModel timingModel ? timingModel.DockRetractDelayMs : 420;
        if (_pointerOutsideDockSince == DateTime.MinValue || _qualityFeedbackClock.IsRunning
            || (DateTime.UtcNow - _pointerOutsideDockSince).TotalMilliseconds < Math.Max(0, delay - 100)) return;
        var revision = _retractSnapshotRevision;
        _retractSnapshotQueued = true;
        // 只在鼠标已经离开、还在等待收回时准备；不增加后台刷新计时器。
        Dispatcher.UIThread.Post(() =>
        {
            _retractSnapshotQueued = false;
            if (revision != _retractSnapshotRevision || _isClosing || _isHiddenToTray
                || !_isDocked || !_isDockRevealed || _isSnapAnimating
                || DataContext is MainViewModel { IsExporting: true } or MainViewModel { IsRefreshingStatus: true }
                || !GetCursorPos(out var pointer) || IsPointInsideWindow(pointer, _dockExpandedPos, _dockScaling)
                || IsPointInsideDockIcon(pointer)) return;
            var capture = Stopwatch.StartNew();
            var snapshot = CaptureWindowBitmap();
            if (snapshot is null) return;
            if (revision != _retractSnapshotRevision || _isClosing)
            {
                snapshot.Dispose();
                return;
            }
            _retractSnapshot = snapshot;
            _retractSnapshotSceneRevision = _edgeSceneRevision;
            _retractSnapshotScale = _captureScale;
            _retractSnapshotWidth = Bounds.Width;
            _retractSnapshotHeight = Bounds.Height;
            _retractSnapshotCaptureMs = capture.Elapsed.TotalMilliseconds;
        }, DispatcherPriority.Background);
    }

    private SKBitmap? TakeRetractSnapshot()
    {
        if (_retractSnapshot is null) return null;
        if (!NearlyEqual(_retractSnapshotWidth, Bounds.Width) || !NearlyEqual(_retractSnapshotHeight, Bounds.Height)
            || !NearlyEqual(_retractSnapshotScale, CalculateCaptureScale(Bounds.Width, Bounds.Height, _dockScaling)))
        {
            InvalidateRetractSnapshot();
            return null;
        }
        var snapshot = _retractSnapshot;
        _retractSnapshot = null;
        _captureScale = _retractSnapshotScale;
        _edgeCapturedSceneRevision = _retractSnapshotSceneRevision;
        _edgeCaptureMs = _retractSnapshotCaptureMs;
        _edgeCaptureSource = "retract-cache";
        return snapshot;
    }

    private async void StartEdgeAnimation(PixelPoint target, int durationMs)
    {
        if (_isClosing || !_isDocked) return;
        var hiding = target == _dockHiddenPos;
        _snapTargetPos = target;
        _isDockRevealed = !hiding;
        _pointerOutsideDockSince = DateTime.MinValue;
        if (_isSnapAnimating)
        {
            // 准备或交接阶段只记住最后一次意图；网格已在播放时直接续接当前位置和速度。
            if (!_isEdgeFinishing && _snapClock.IsRunning && _edgeMotion.IsHiding != hiding)
                RetargetEdgeAnimation(hiding);
            return;
        }

        _isSnapAnimating = true;
        _edgeVisualSettling = true;
        _qualityFeedbackScale.ScaleY = 1;
        _qualityFeedbackClock.Reset();
        var sourcePosition = hiding ? Position : _dockExpandedPos;
        _edgeCanvasPosition = new PixelPoint(Math.Min(sourcePosition.X, _dockExpandedPos.X), Math.Min(sourcePosition.Y, _dockExpandedPos.Y));
        var geometryScale = Math.Max(_dockScaling, 0.1);
        _edgeMeshOrigin = new Vector((_dockExpandedPos.X - _edgeCanvasPosition.X) / geometryScale, (_dockExpandedPos.Y - _edgeCanvasPosition.Y) / geometryScale);
        _edgeSourceOffset = new Vector((sourcePosition.X - _dockExpandedPos.X) / geometryScale, (sourcePosition.Y - _dockExpandedPos.Y) / geometryScale);
        _edgeCanvasWidth = Bounds.Width + Math.Abs(_edgeSourceOffset.X);
        _edgeCanvasHeight = Bounds.Height + Math.Abs(_edgeSourceOffset.Y);
        _edgeGeometryClock.Reset();
        _isEdgeFinishing = false;
        _snapClock.Reset();
        _edgePlaybackClock.Reset();
        _frameCount = 0;
        _maxFrameGapMs = 0;
        _windowSizeSaveTimer.Stop();
        _animationViewModel?.SetWindowAnimationActive(true);
        _edgeHoverTimer.Stop();
        UpdateStatusAnimationState(restartActiveClocks: false);
        var generation = ++_edgeAnimationGeneration;
        var preparing = Stopwatch.StartNew();
        _edgeCaptureMs = _edgeReadyMs = _edgePrepareMs = _edgeMaxUpdateMs = 0;
        _edgeLongIntervals = 0;
        _edgeFirstDrawConfirmed = false;
        _edgeCaptureSource = "fresh";
        _edgeLivePrepareMs = 0;
        _edgeCanReverseOnPointerReturn = hiding && GetCursorPos(out var cursor)
            && !IsPointInsideWindow(cursor, _dockExpandedPos, _dockScaling);
        try
        {
            if (WindowState == global::Avalonia.Controls.WindowState.Minimized)
                WindowState = global::Avalonia.Controls.WindowState.Normal;
            if (hiding)
            {
                ClearDockRevealBitmap();
                _genieCapturedBitmap = TakeRetractSnapshot();
                if (_genieCapturedBitmap is null)
                {
                    var capturing = Stopwatch.StartNew();
                    _edgeCapturedSceneRevision = _edgeSceneRevision;
                    _genieCapturedBitmap = CaptureWindowBitmap();
                    _edgeCaptureMs = capturing.Elapsed.TotalMilliseconds;
                }
            }
            else
            {
                PrepareLiveWindowForReveal();
                if (_dockRevealBitmapStale)
                {
                    ClearDockRevealBitmap();
                    await WaitForRenderedBatchAsync(this);
                    if (generation != _edgeAnimationGeneration || _isClosing) return;
                }
                _edgeCaptureSource = _dockRevealBitmap is null ? "fresh" : "reveal-cache";
                var capturing = Stopwatch.StartNew();
                if (_dockRevealBitmap is null) _edgeCapturedSceneRevision = _edgeSceneRevision;
                _genieCapturedBitmap = _dockRevealBitmap ?? CaptureWindowBitmap();
                _edgeCaptureMs = _dockRevealBitmap is null ? capturing.Elapsed.TotalMilliseconds : 0;
                _dockRevealBitmap = null;
            }
            if (_genieCapturedBitmap is null)
            {
                await FinishEdgeAnimationAsync(generation, hiding);
                return;
            }
            // 图标从动画开始就位，但只逐渐显出图像；点击热区不跟着缩放。
            ShowDockIcon(hiding ? 0 : 1);
            if (_genieAnimationWindow is null)
            {
                _genieAnimationWindow = new GenieAnimationWindow(_edgeCanvasPosition, _edgeCanvasWidth, _edgeCanvasHeight,
                    _genieCapturedBitmap, _captureScale);
                _genieAnimationWindow.Show();
            }
            else
            {
                _genieAnimationWindow.Position = _edgeCanvasPosition;
                _genieAnimationWindow.Width = _edgeCanvasWidth;
                _genieAnimationWindow.Height = _edgeCanvasHeight;
                _genieAnimationWindow.SetBitmap(_genieCapturedBitmap, _captureScale);
                if (!_genieAnimationWindow.IsVisible) _genieAnimationWindow.Show();
                WindowTaskSwitcher.Configure(_genieAnimationWindow, excluded: true, noActivate: true);
            }
            SetAnimationMesh(hiding ? 0 : 1);
            _genieAnimationWindow.Opacity = 1;
            var waiting = Stopwatch.StartNew();
            _edgeFirstDrawConfirmed = await _genieAnimationWindow.WaitForFirstDrawAsync();
            _edgeReadyMs = waiting.Elapsed.TotalMilliseconds;
            if (generation != _edgeAnimationGeneration || _isClosing) return;
            Opacity = 0;
            if (hiding)
            {
                Position = _offscreenParkPos;
                WindowTaskSwitcher.Configure(this, excluded: true);
            }
            else
            {
                // 在图标仍可见、网格尚未起步时完成 HWND/位置恢复，避免末帧才 Hide/Show。
                if (!_edgeLiveWindowPrepared) PrepareLiveWindowForReveal();
                WindowTaskSwitcher.RaiseWithoutActivation(_genieAnimationWindow);
            }
            _edgeGeometryClock.Restart();
            _edgeMotion.Start(hiding, durationMs);
            _edgePrepareMs = preparing.Elapsed.TotalMilliseconds;
            _snapClock.Restart();
            _edgePlaybackClock.Restart();
            _genieAnimationWindow.BeginFrameTiming();
            _frameCount = 0;
            _lastFrameMs = 0;
            _maxFrameGapMs = 0;
            ScheduleEdgeAnimationFallback(generation);
            // 捕获/等首帧期间也允许改主意，不会丢掉这次操作。
            if ((_snapTargetPos == _dockHiddenPos) != hiding)
                RetargetEdgeAnimation(!hiding);
            QueueEdgeFrame(generation);
        }
        catch (Exception error)
        {
            Debug.WriteLine("收纳动画失败: " + error);
            if (DataContext is MainViewModel diagnostics) diagnostics.LogDockAnimation("[dock] prepare failed: " + error, failure: true);
            await FinishEdgeAnimationAsync(generation, hiding);
        }
    }

    private void PrepareLiveWindowForReveal()
    {
        var preparing = Stopwatch.StartNew();
        _edgeSavedHitTestVisible ??= IsHitTestVisible;
        IsHitTestVisible = false; // 尾段交接前不让真实控件接收点击。
        Opacity = 0;
        WindowTaskSwitcher.Configure(this, excluded: false);
        Position = _dockExpandedPos;
        _edgeLiveWindowPrepared = true;
        if (_genieAnimationWindow is not null)
            WindowTaskSwitcher.RaiseWithoutActivation(_genieAnimationWindow);
        _edgeLivePrepareMs += preparing.Elapsed.TotalMilliseconds;
    }

    private static async Task<bool> WaitForRenderedBatchAsync(Visual visual)
    {
        var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;
        if (compositor is null) return false;
        // Rendered 与 UI 帧计数不同：等待本批改动在渲染线程画出；不宣称 DWM 已呈现。
        var rendered = compositor.RequestCompositionBatchCommitAsync().Rendered;
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = rendered.ContinueWith(t => ready.TrySetResult(t.IsCompletedSuccessfully), TaskScheduler.Default);
        return await Task.WhenAny(ready.Task, Task.Delay(200)) == ready.Task && await ready.Task;
    }

    private void ResetLiveWindowPreparation()
    {
        _edgeLiveWindowPrepared = false;
        _edgeVisualSettling = false;
        if (_edgeSavedHitTestVisible is { } enabled)
        {
            IsHitTestVisible = enabled;
            _edgeSavedHitTestVisible = null;
        }
    }

    private async Task SavePlacementAfterDockMotionAsync(int generation)
    {
        // 图标还有 220ms 余韵，不在它的首帧插入同步写盘。
        await Task.Delay(250);
        if (!_isClosing && generation == _edgeAnimationGeneration && !_isSnapAnimating)
            PersistWindowPlacement();
    }

    private void RetargetEdgeAnimation(bool hiding)
    {
        _edgeMotion.Retarget(hiding, _snapClock.Elapsed.TotalMilliseconds);
        _snapClock.Restart();
        _pointerOutsideDockSince = DateTime.MinValue;
        ScheduleEdgeAnimationFallback(_edgeAnimationGeneration);
        // 保留同一张纹理、同一承载窗和已经排队的逐帧回调，不再截图或切换 HWND。
    }

    private void SetAnimationMesh(double collapse)
    {
        var scale = Math.Max(_dockScaling, 0.1);
        // Window.Position 是物理像素，网格是 DIP；使用实际图标中心，兼容四边与 DPI。
        var anchor = new SKPoint(
            (float)((_dockIconPlacement.Position.X + _dockIconPlacement.SizePx / 2d - _dockExpandedPos.X) / scale),
            (float)((_dockIconPlacement.Position.Y + _dockIconPlacement.SizePx / 2d - _dockExpandedPos.Y) / scale));
        _animationTopology = EdgeSnapHelper.CalculateGenieMeshData(_dockedEdge,
            collapse, isHiding: true, (float)Bounds.Width, (float)Bounds.Height,
            cols: 16, rows: 16, peekPx: 0, topology: _animationTopology,
            dockAnchor: anchor, dockSpan: (float)(_dockIconPlacement.SizePx / scale));
        // 首次收纳从真实拖动位置起步，平顺归位；反向仍延续同一条位置轨迹。
        var alignment = Math.Clamp(_edgeGeometryClock.Elapsed.TotalMilliseconds / 100, 0, 1);
        alignment = alignment * alignment * (3 - 2 * alignment);
        var offset = _edgeMeshOrigin + _edgeSourceOffset * (1 - alignment);
        if (offset.X != 0 || offset.Y != 0)
            for (var i = 0; i < _animationTopology.Vertices.Length; ++i)
            {
                var point = _animationTopology.Vertices[i];
                _animationTopology.Vertices[i] = new SKPoint(point.X + (float)offset.X, point.Y + (float)offset.Y);
            }
        _genieAnimationWindow?.SetMesh(_animationTopology);
        _genieAnimationWindow?.SetContentOpacity(DockAnimationMotion.MeshVisibility(collapse));
        _dockIconWindow?.SetTransition(collapse);
    }

    private void QueueEdgeFrame(int generation)
        => _genieAnimationWindow?.RequestAnimationFrame(_ => AdvanceEdgeFrame(generation));

    private void ScheduleEdgeAnimationFallback(int generation)
        => _ = EnsureEdgeAnimationCompletesAsync(generation, ++_edgeMotionRevision, _edgeMotion.DurationMs);

    private async Task EnsureEdgeAnimationCompletesAsync(int generation, int revision, double durationMs)
    {
        // 每个方向段仅一次超时兜底；旧方向的超时不能提前结束刚反向的动画。
        await Task.Delay((int)Math.Ceiling(durationMs) + 200);
        if (generation == _edgeAnimationGeneration && revision == _edgeMotionRevision
            && _isSnapAnimating && !_isEdgeFinishing && !_isClosing)
            await FinishEdgeAnimationAsync(generation, _edgeMotion.IsHiding);
    }

    private void UpdateEdgeAnimationIntentFromPointer()
    {
        if (!GetCursorPos(out var cursor)) return;
        var inside = IsPointInsideWindow(cursor, _dockExpandedPos, _dockScaling) || IsPointInsideDockIcon(cursor);
        if (_edgeMotion.IsHiding)
        {
            if (!inside) _edgeCanReverseOnPointerReturn = true;
            else if (_edgeCanReverseOnPointerReturn)
                StartEdgeAnimation(_dockExpandedPos, DockAnimationMotion.RevealDurationMs);
        }
        else if (inside)
        {
            _pointerOutsideDockSince = DateTime.MinValue;
        }
        else
        {
            if (_pointerOutsideDockSince == DateTime.MinValue) _pointerOutsideDockSince = DateTime.UtcNow;
            var delay = DataContext is MainViewModel model ? model.DockRetractDelayMs : 420;
            if ((DateTime.UtcNow - _pointerOutsideDockSince).TotalMilliseconds >= delay)
                StartEdgeAnimation(_dockHiddenPos, DockAnimationMotion.HideDurationMs);
        }
    }

    private bool IsPointInsideDockIcon(NativePoint cursor)
    {
        var padding = (int)Math.Ceiling(DockIconWindow.TriggerPaddingDip * _dockScaling);
        return new PixelRect(_dockIconPlacement.Position.X - padding, _dockIconPlacement.Position.Y - padding,
            _dockIconPlacement.SizePx + padding * 2, _dockIconPlacement.SizePx + padding * 2)
            .Contains(new PixelPoint(cursor.X, cursor.Y));
    }

    private async void AdvanceEdgeFrame(int generation)
    {
        if (generation != _edgeAnimationGeneration || !_isSnapAnimating || _isEdgeFinishing || _isClosing) return;
        try
        {
            UpdateEdgeAnimationIntentFromPointer();
            var updating = Stopwatch.StartNew();
            var elapsed = _snapClock.Elapsed.TotalMilliseconds;
            var playback = _edgePlaybackClock.Elapsed.TotalMilliseconds;
            var gap = playback - _lastFrameMs;
            _maxFrameGapMs = Math.Max(_maxFrameGapMs, gap);
            if (gap > 25) ++_edgeLongIntervals;
            _lastFrameMs = playback;
            _frameCount++;
            var frame = _edgeMotion.Sample(elapsed);
            if (frame.Complete && (_edgeSourceOffset == default || _edgeGeometryClock.ElapsedMilliseconds >= 100))
            {
                await FinishEdgeAnimationAsync(generation, _edgeMotion.IsHiding);
                return;
            }
            SetAnimationMesh(frame.Collapse);
            _edgeMaxUpdateMs = Math.Max(_edgeMaxUpdateMs, updating.Elapsed.TotalMilliseconds);
            QueueEdgeFrame(generation);
        }
        catch (Exception error)
        {
            Debug.WriteLine("收纳帧失败: " + error);
            if (DataContext is MainViewModel diagnostics) diagnostics.LogDockAnimation("[dock] frame failed: " + error, failure: true);
            await FinishEdgeAnimationAsync(generation, _edgeMotion.IsHiding);
        }
    }

    private async Task FinishEdgeAnimationAsync(int generation, bool hiding)
    {
        if (generation != _edgeAnimationGeneration || _isClosing || _isEdgeFinishing) return;
        _isEdgeFinishing = true;
        var handoff = Stopwatch.StartNew();
        _snapClock.Stop();
        _edgePlaybackClock.Stop();
        // 展开末帧必须是完全未变形的窗口，不能提前把几何不同的两层叠在一起。
        _edgeGeometryClock.Stop();
        if (!hiding) _edgeSourceOffset = default;
        SetAnimationMesh(hiding ? 1 : 0);
        var endFrameDrawn = true;
        if (!hiding && _genieAnimationWindow is not null)
            endFrameDrawn = await WaitForRenderedBatchAsync(_genieAnimationWindow);
        if (generation != _edgeAnimationGeneration || _isClosing) return;
        // 两个方向都保留最后一张纹理；下次准备新图或退出收纳时再释放，避免末帧析构。
        ClearDockRevealBitmap();
        _dockRevealBitmap = _genieCapturedBitmap;
        _dockRevealBitmapStale = _edgeCapturedSceneRevision != _edgeSceneRevision;
        _genieCapturedBitmap = null;
        if (hiding)
        {
            if (_dockIconWindow is not { Opacity: > 0 }) ShowDockIcon();
            else _dockIconWindow.SetTransition(1);
            _dockIconWindow?.PulseArrival(_dockedEdge);
            Opacity = 0;
            Position = _offscreenParkPos;
            WindowTaskSwitcher.Configure(this, excluded: true);
        }
        else
        {
            if (!_edgeLiveWindowPrepared)
            {
                // 收回中途改为展开、或没有纹理的降级路径，仍需补齐窗口状态。
                WindowTaskSwitcher.Configure(this, excluded: false);
                Position = _dockExpandedPos;
            }
            Opacity = 1;
        }
        // 原尺寸末帧已画出，再同步切换两扇窗的可见性；不做双层阴影渐变。
        var renderTiming = _genieAnimationWindow?.FrameTimingSummary ?? "draw_frames=0";
        CloseGenieAnimationWindow(retainTexture: true, restoreInput: false);
        RootGrid.InvalidateVisual();
        var liveFrameDrawn = await WaitForRenderedBatchAsync(this);
        if (generation != _edgeAnimationGeneration || _isClosing) return;
        ResetLiveWindowPreparation();
        if (!hiding) CloseDockIcon();
        var report = FormattableString.Invariant(
            $"[dock] direction={(hiding ? "hide" : "reveal")} capture={_edgeCaptureSource} capture_ms={_edgeCaptureMs:F1} capture_scale={_captureScale:F2} dock_scale={_dockScaling:F2} ready_ms={_edgeReadyMs:F1} first_draw={_edgeFirstDrawConfirmed} prepare_ms={_edgePrepareMs:F1} live_prepare_ms={_edgeLivePrepareMs:F1} end_frame={endFrameDrawn} live_frame={liveFrameDrawn} playback_ms={_edgePlaybackClock.Elapsed.TotalMilliseconds:F1} handoff_ms={handoff.Elapsed.TotalMilliseconds:F1} ui_frames={_frameCount} ui_max_gap_ms={_maxFrameGapMs:F1} ui_gaps_over_25ms={_edgeLongIntervals} ui_update_max_ms={_edgeMaxUpdateMs:F2} {renderTiming}");
        if (DataContext is MainViewModel diagnosticModel) diagnosticModel.LogDockAnimation(report);
        _isSnapAnimating = false;
        _isEdgeFinishing = false;
        _animationViewModel?.SetWindowAnimationActive(false);
        var requestedHiding = _snapTargetPos == _dockHiddenPos;
        if (requestedHiding != hiding)
        {
            StartEdgeAnimation(_snapTargetPos, requestedHiding
                ? DockAnimationMotion.HideDurationMs : DockAnimationMotion.RevealDurationMs);
            return;
        }
        _isDockRevealed = !hiding;
        QueueForegroundDocumentProbe();
        UpdateStatusAnimationState(restartActiveClocks: false);
        if (_isDocked && _isDockRevealed) _edgeHoverTimer.Start();
        _ = SavePlacementAfterDockMotionAsync(generation);
    }
}
