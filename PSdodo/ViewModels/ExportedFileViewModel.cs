using CommunityToolkit.Mvvm.ComponentModel;

namespace PsExporter.ViewModels;

/// <summary>已导出文件项（用于实时进度列表展示）</summary>
public partial class ExportedFileViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Time { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileName { get; set; } = string.Empty;
}
