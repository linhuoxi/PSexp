using System;
using System.IO;
using PsExporter.Models;

namespace PsExporter.Services;

public static class ExportSettingsValidator
{

    public static bool TryValidateSliceFolder(string name, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrEmpty(name)) return true;
        var stem = name.Split('.')[0];
        var reserved = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        if (name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.Contains('/') || name.Contains('\\') || name.EndsWith('.') || name.EndsWith(' ')
            || Array.Exists(reserved, item => item.Equals(stem, StringComparison.OrdinalIgnoreCase)))
        {
            error = "切片文件夹名称无效：请填写单个文件夹名，不含路径、非法字符或系统保留名称";
            return false;
        }
        return true;
    }

    public static bool TryPrepare(ExportSettings settings, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(settings.ExportPath))
        {
            error = "请选择导出文件夹";
            return false;
        }

        if (settings.Quality < ExportSettings.MinimumQuality ||
            settings.Quality > ExportSettings.MaximumQuality)
        {
            error = $"图片质量必须在 {ExportSettings.MinimumQuality:0} 到 {ExportSettings.MaximumQuality:0} 之间";
            return false;
        }

        settings.SliceFolderName = (settings.SliceFolderName ?? string.Empty).Trim();
        if (!TryValidateSliceFolder(settings.SliceFolderName, out error)) return false;

        try
        {
            if (!Path.IsPathFullyQualified(settings.ExportPath))
            {
                error = "导出路径必须是完整路径";
                return false;
            }

            settings.ExportPath = Path.GetFullPath(settings.ExportPath.Trim());
            Directory.CreateDirectory(settings.ExportPath);

            var probePath = Path.Combine(settings.ExportPath, $".psx-write-{Guid.NewGuid():N}.tmp");
            using var probe = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
            probe.WriteByte(0);
        }
        catch (Exception ex) when (ex is ArgumentException
                                   or IOException
                                   or NotSupportedException
                                   or UnauthorizedAccessException)
        {
            error = "导出文件夹不可用或没有写入权限：" + ex.Message;
            return false;
        }

        settings.FileName = settings.FileName.Trim();
        settings.ImageName = settings.ImageName.Trim();
        return true;
    }
}
