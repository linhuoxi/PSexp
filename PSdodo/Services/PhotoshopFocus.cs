using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PsExporter.Services;

internal static class PhotoshopFocus
{
    // Mouse buttons, modifiers and Space only; do not scan ordinary character keys.
    private static readonly int[] GestureKeys = { 0x01, 0x02, 0x04, 0x05, 0x06, 0x10, 0x11, 0x12, 0x20, 0x5B, 0x5C };
    internal static bool IsForeground()
    {
        if (!OperatingSystem.IsWindows()) return false;
        GetWindowThreadProcessId(GetForegroundWindow(), out var pid);
        try { using var process = Process.GetProcessById((int)pid); return process.ProcessName.Equals("Photoshop", StringComparison.OrdinalIgnoreCase); }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }

    // Read-only state checks: no hooks, hotkey registration, input injection or key-content logging.
    internal static bool TryGetIdleForegroundSnapshot(out string windowKey, out uint lastInputTick)
    {
        windowKey = string.Empty;
        lastInputTick = 0;
        if (!OperatingSystem.IsWindows() || !IsForeground()) return false;
        var input = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref input) || unchecked((uint)Environment.TickCount - input.Time) < 1200) return false;
        // Includes mouse buttons and held modifiers (Space/Ctrl/Alt/Shift/Win). Never probe during a held gesture.
        foreach (var key in GestureKeys)
            if ((GetAsyncKeyState(key) & 0x8000) != 0) return false;
        var foreground = GetForegroundWindow();
        GetWindowThreadProcessId(foreground, out var pid);
        try
        {
            using var process = Process.GetProcessById((int)pid);
            if (!process.ProcessName.Equals("Photoshop", StringComparison.OrdinalIgnoreCase)) return false;
            windowKey = pid + "|" + process.MainWindowTitle;
            lastInputTick = input.Time;
            return GetForegroundWindow() == foreground;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo { public uint Size; public uint Time; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetLastInputInfo(ref LastInputInfo info);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
}
