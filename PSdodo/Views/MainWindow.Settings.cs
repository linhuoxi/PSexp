using System;
using System.Diagnostics;
using Avalonia.Threading;

namespace PsExporter.Views;

public partial class MainWindow
{
    private readonly DispatcherTimer _settingsPageFadeTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _settingsPageFadeClock = new();
    private double _settingsPageFadeStart;

    private void BeginSettingsPageFade()
    {
        if (_isClosing) return;
        // Restart from the current opacity when categories are changed quickly.
        _settingsPageFadeStart = Math.Min(SettingsPageHost.Opacity, 0.72);
        SettingsPageHost.Opacity = _settingsPageFadeStart;
        _settingsPageFadeClock.Restart();
        _settingsPageFadeTimer.Start();
    }

    private void OnSettingsPageFadeTick(object? sender, EventArgs e)
    {
        var progress = Math.Clamp(_settingsPageFadeClock.Elapsed.TotalMilliseconds / 170, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        SettingsPageHost.Opacity = _settingsPageFadeStart + (1 - _settingsPageFadeStart) * eased;
        if (progress >= 1) StopSettingsPageFade();
    }

    private void StopSettingsPageFade()
    {
        _settingsPageFadeTimer.Stop();
        _settingsPageFadeClock.Reset();
        SettingsPageHost.Opacity = 1;
    }
}
