using PsExporter.Models;

namespace PsExporter.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    string DataDirectory { get; }
    AppSettings Load();
    bool Save(AppSettings settings);
}
