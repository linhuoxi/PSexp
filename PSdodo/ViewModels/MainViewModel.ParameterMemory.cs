using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PsExporter.Models;
using PsExporter.Services;

namespace PsExporter.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private string _sliceFolderName = string.Empty;
    private string _quickImageName = string.Empty;
    private string _layerImagePrefix = string.Empty;
    private DispatcherTimer? _parameterMemoryTimer;

    private void QueueParameterMemory()
    {
        if (!_settingsInitialized) return;
        if (_parameterMemoryTimer is null)
        {
            _parameterMemoryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _parameterMemoryTimer.Tick += (_, _) => FlushParameterMemory();
        }
        _parameterMemoryTimer.Stop();
        _parameterMemoryTimer.Start();
    }

    public void FlushParameterMemory()
    {
        if (_parameterMemoryTimer?.IsEnabled != true) return;
        _parameterMemoryTimer.Stop();
        SaveBehaviorSettings();
    }

    public string SliceFolderNameError
    {
        get
        {
            ExportSettingsValidator.TryValidateSliceFolder((SliceFolderName ?? string.Empty).Trim(), out var error);
            return error;
        }
    }
    public bool HasSliceFolderNameError => !string.IsNullOrEmpty(SliceFolderNameError);
    partial void OnSliceFolderNameChanged(string value)
    {
        OnPropertyChanged(nameof(SliceFolderNameError));
        OnPropertyChanged(nameof(HasSliceFolderNameError));
        QueueParameterMemory();
    }
    partial void OnExportPathOptionChanged(ExportPathOption value) => QueueParameterMemory();
    partial void OnQualityChanged(int value) => QueueParameterMemory();
    partial void OnIsViewSettingChanged(bool value) => QueueParameterMemory();
    partial void OnImageNameChanged(string value)
    {
        if (SelectedMode == ExportMode.QuickExport) _quickImageName = value;
        else if (SelectedMode == ExportMode.Layers) _layerImagePrefix = value;
        QueueParameterMemory();
    }
}
