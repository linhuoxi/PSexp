using System.Diagnostics;
using Avalonia;

namespace PsExporter.Controls;

/// <summary>普通滚轮一格一步；触控板小增量累计，反向或离开后丢弃余量。</summary>
internal sealed class WheelStepAccumulator
{
    private double _remainder;
    private long _lastInput;

    public int Add(Vector delta, bool increaseUp = false)
    {
        var amount = Math.Abs(delta.Y) >= Math.Abs(delta.X)
            ? (increaseUp ? delta.Y : -delta.Y)
            : delta.X;
        if (!double.IsFinite(amount) || Math.Abs(amount) < 0.0001) return 0;
        var now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(_lastInput, now).TotalMilliseconds > 400
            || Math.Sign(amount) != Math.Sign(_remainder))
            _remainder = 0;
        _lastInput = now;
        _remainder += amount;
        var wholeSteps = Math.Truncate(_remainder);
        _remainder -= wholeSteps;
        // 单次合并事件只执行一个最终选择，不排队重放积压的滚动。
        return (int)Math.Clamp(wholeSteps, -4, 4);
    }

    public void Reset() => _remainder = 0;
}
