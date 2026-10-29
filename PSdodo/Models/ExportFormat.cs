namespace PsExporter.Models;

public enum ExportFormat
{
    Jpg,
    Png
}

public static class ExportFormatExtensions
{
    public static string ToExtension(this ExportFormat f) => f == ExportFormat.Png ? "png" : "jpg";

    public static string ToScriptString(this ExportFormat f) => f == ExportFormat.Png ? "png" : "jpg";
}
