using CommunityToolkit.Mvvm.ComponentModel;

namespace PsExporter.ViewModels;

public partial class LogLineViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Time { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Level { get; set; } = "Info";

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;
}
