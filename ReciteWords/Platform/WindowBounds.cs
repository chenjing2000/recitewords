using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using ReciteWords.Models;
namespace ReciteWords.Platform;

public static class WindowBounds
{
    public static Size CalculateInitialSize(int workWidth, int workHeight, uint dpi)
    {
        double scale = dpi / 96.0;
        return new Size(Math.Min(700, workWidth / scale), Math.Min(850, workHeight / scale));
    }
    public static void ApplySavedBounds(Window window, AppSettings settings)
    {
        if (settings.Left.HasValue && settings.Top.HasValue && double.IsFinite(settings.Left.Value) && double.IsFinite(settings.Top.Value))
        { window.Left = settings.Left.Value; window.Top = settings.Top.Value; }
        IntPtr handle = new WindowInteropHelper(window).Handle;
        var monitor = Info(handle);
        uint dpi = GetDpiForWindow(handle); if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;
        var initial = CalculateInitialSize(monitor.Work.Right - monitor.Work.Left, monitor.Work.Bottom - monitor.Work.Top, dpi);
        window.Width = Math.Max(window.MinWidth, settings.Width > 0 && double.IsFinite(settings.Width) ? settings.Width : initial.Width);
        window.Height = Math.Max(window.MinHeight, settings.Height > 0 && double.IsFinite(settings.Height) ? settings.Height : initial.Height);
        // 启动时将窗口移回可见工作区，不限制保存的尺寸或之后的拖动。
        window.Left = Math.Clamp(window.Left, monitor.Work.Left / scale, Math.Max(monitor.Work.Left / scale, monitor.Work.Right / scale - window.Width));
        window.Top = Math.Clamp(window.Top, monitor.Work.Top / scale, Math.Max(monitor.Work.Top / scale, monitor.Work.Bottom / scale - window.Height));
        if (!settings.Left.HasValue) window.Left = monitor.Work.Left / scale + Math.Max(0, (monitor.Work.Right - monitor.Work.Left) / scale - window.Width) / 2;
        if (!settings.Top.HasValue) window.Top = monitor.Work.Top / scale + Math.Max(0, (monitor.Work.Bottom - monitor.Work.Top) / scale - window.Height) / 2;
    }
    private static MonitorInfo Info(IntPtr handle)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) throw new System.ComponentModel.Win32Exception();
        return info;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rectangle { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rectangle Monitor; public Rectangle Work; public int Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
}
