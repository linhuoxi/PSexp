using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PsExporter.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private bool _closeToTray = true;
    public string WindowCloseHint => CloseToTray ? "收起到托盘，后台继续运行" : "退出 PSexp";
    partial void OnCloseToTrayChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowCloseHint));
        SaveBehaviorSettings();
    }
    [RelayCommand] private void SetCloseToTray(object? value) => CloseToTray = ParseBooleanCommandParameter(value);

    [ObservableProperty] private int _selectedSettingsSection;
    public bool IsSettingsGeneral => SelectedSettingsSection == 0;
    public bool IsSettingsExport => SelectedSettingsSection == 1;
    public bool IsSettingsLink => SelectedSettingsSection == 2;
    public bool IsSettingsUpdate => SelectedSettingsSection == 3;
    public string LogFilePath => _fileLogger.LogPath;
    partial void OnSelectedSettingsSectionChanged(int value)
    {
        OnPropertyChanged(nameof(IsSettingsGeneral));
        OnPropertyChanged(nameof(IsSettingsExport));
        OnPropertyChanged(nameof(IsSettingsLink));
        OnPropertyChanged(nameof(IsSettingsUpdate));
    }
    [RelayCommand] private void SelectSettingsSection(object? value)
    {
        if (int.TryParse(value?.ToString(), out var section) && section is >= 0 and <= 3)
            SelectedSettingsSection = section;
    }
    [RelayCommand] private void HideSettings() => IsViewSetting = false;
    [RelayCommand] private void OpenLogDirectory()
    {
        try
        {
            var directory = Path.GetDirectoryName(LogFilePath)!;
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        }
        catch (Exception error) { AppendLog("Error", "打开日志目录失败：" + error.Message); }
    }
}
