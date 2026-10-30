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
        if (!_settings.Save(CreateAppSettings(useCurrentExportValues: false)))
        {
            (_windowPlacement, _windowWidth, _windowHeight) = previous;
            AppendLog("Error", "窗口位置与收纳状态保存失败，请检查数据目录写入权限。");
        }
    }

    public void LogPlacementRestoreFailure(Exception error)
        => AppendLog("Warn", "窗口状态恢复失败，已回到可见窗口：" + error.Message);
}
