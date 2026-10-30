using PsExporter.Models;
using PsExporter.Services;

namespace PsExporter.ViewModels;

public partial class MainViewModel
{
    private WindowPlacementState? _windowPlacement;
    public WindowPlacementState? GetSavedWindowPlacement() => _windowPlacement;

    public void SaveWindowPlacement(WindowPlacementState placement, double width, double height)
    {
        var (w, h) = SettingsService.NormalizeWindowSize(width, height);
        if (_windowPlacement == placement && Math.Abs(_windowWidth - w) < 0.5 && Math.Abs(_windowHeight - h) < 0.5) return;
        var previous = (_windowPlacement, _windowWidth, _windowHeight);
        _windowPlacement = placement;
        _windowWidth = w;
        _windowHeight = h;
        if (!_settings.Save(CreateAppSettings()))
        {
            (_windowPlacement, _windowWidth, _windowHeight) = previous;
            AppendLog("Error", "窗口位置与收纳状态保存失败，请检查数据目录写入权限。");
        }
    }

    // 每次动效结束只写一条汇总，避免逐帧磁盘 I/O 本身拖慢 UI。
    public void LogDockAnimation(string message, bool failure = false)
        => _ = System.Threading.Tasks.Task.Run(() =>
        {
            if (failure) _fileLogger.Warn(message);
            else _fileLogger.Debug(message);
        });

    public void LogPlacementRestoreFailure(Exception error)
        => AppendLog("Warn", "窗口状态恢复失败，已回到可见窗口：" + error.Message);
}
