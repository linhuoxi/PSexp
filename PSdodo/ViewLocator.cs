using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace PsExporter;

/// <summary>
/// 标准的 Avalonia ViewLocator：把 ViewModel 自动映射到同名的 View。
/// 命名规则：PsExporter.ViewModels.MainViewModel -> PsExporter.Views.MainView
/// </summary>
public class ViewLocator : IDataTemplate
{
    public bool SupportsRecycling => false;

    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModels.ViewModelBase;
    }
}
