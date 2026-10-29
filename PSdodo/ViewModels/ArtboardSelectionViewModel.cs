using CommunityToolkit.Mvvm.ComponentModel;

namespace PsExporter.ViewModels;

public partial class ArtboardSelectionViewModel : ViewModelBase
{
    public int Id { get; init; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;
}
