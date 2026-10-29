using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PsExporter.Services;
internal static class PhotoshopFocus
{
    internal static bool IsForeground()
    {
        if (!OperatingSystem.IsWindows()) return false;
        GetWindowThreadProcessId(GetForegroundWindow(), out var pid);
        try { using var process=Process.GetProcessById((int)pid); return process.ProcessName.Equals("Photoshop",StringComparison.OrdinalIgnoreCase); }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }
    [DllImport("user32.dll", EntryPoint="GetForegroundWindow")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", EntryPoint="GetWindowThreadProcessId")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
}
