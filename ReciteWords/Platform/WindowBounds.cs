using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using ReciteWords.Models;
namespace ReciteWords.Platform;

public static class WindowBounds
{
    public static Size CalculateLimits(int pixelWidth, int workHeight, uint dpi)
    {
        double scale = dpi / 96.0;
        return new Size(pixelWidth * 0.7 / scale, workHeight / scale);
    }
    public static void Attach(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        HwndSource source = HwndSource.FromHwnd(handle);
        bool updating = false;
        void Update(bool clampPosition = false)
        {
            if (updating) return;
            updating = true;
            try { Limit(window, handle, clampPosition); }
            finally { updating = false; }
        }
        IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == 0x0024) // WM_GETMINMAXINFO also constrains title-bar maximization.
            {
                var monitor = Info(hwnd);
                var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                int width = Math.Min((int)((monitor.Monitor.Right - monitor.Monitor.Left) * 0.7), monitor.Work.Right - monitor.Work.Left);
                int height = monitor.Work.Bottom - monitor.Work.Top;
                limits.MaxPosition.X = monitor.Work.Left - monitor.Monitor.Left;
                limits.MaxPosition.Y = monitor.Work.Top - monitor.Monitor.Top;
                limits.MaxSize.X = width; limits.MaxSize.Y = height;
                limits.MaxTrackSize.X = width; limits.MaxTrackSize.Y = height;
                double scale = GetDpiForWindow(hwnd) / 96.0;
                limits.MinTrackSize.X = (int)Math.Ceiling(window.MinWidth * scale);
                limits.MinTrackSize.Y = (int)Math.Ceiling(window.MinHeight * scale);
                Marshal.StructureToPtr(limits, lParam, false); handled = true;
            }
            if (message == 0x02E0 || message == 0x001A || message == 0x0232)
                window.Dispatcher.BeginInvoke(new Action(() => Update(message != 0x02E0)));
            return IntPtr.Zero;
        }
        source.AddHook(Hook);
        window.LocationChanged += (_, _) => Update();
        window.Closed += (_, _) => source.RemoveHook(Hook);
        Update();
    }
    public static void ApplySavedBounds(Window window, AppSettings settings)
    {
        if (settings.Left.HasValue && settings.Top.HasValue && double.IsFinite(settings.Left.Value) && double.IsFinite(settings.Top.Value))
        { window.Left = settings.Left.Value; window.Top = settings.Top.Value; }
        IntPtr handle = new WindowInteropHelper(window).Handle;
        Limit(window, handle);
        window.Width = settings.Width > 0 && double.IsFinite(settings.Width) ? Math.Clamp(settings.Width, window.MinWidth, window.MaxWidth) : window.MaxWidth;
        window.Height = settings.Height > 0 && double.IsFinite(settings.Height) ? Math.Clamp(settings.Height, window.MinHeight, window.MaxHeight) : window.MaxHeight * 0.9;
        var monitor = Info(handle); double scale = GetDpiForWindow(handle) / 96.0;
        if (!settings.Left.HasValue) window.Left = monitor.Work.Left / scale + 12;
        if (!settings.Top.HasValue) window.Top = monitor.Work.Top / scale + (window.MaxHeight - window.Height) / 2;
        Limit(window, handle, true);
    }
    private static void Limit(Window window, IntPtr handle, bool clampPosition = false)
    {
        var monitor = Info(handle); uint dpi = GetDpiForWindow(handle); if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;
        var maximum = CalculateLimits(monitor.Monitor.Right - monitor.Monitor.Left, monitor.Work.Bottom - monitor.Work.Top, dpi);
        window.MaxWidth = maximum.Width; window.MaxHeight = maximum.Height;
        if (window.WindowState != WindowState.Normal) return;
        window.Width = Math.Clamp(window.Width, window.MinWidth, maximum.Width);
        window.Height = Math.Clamp(window.Height, window.MinHeight, maximum.Height);
        // Do not clamp position during a drag: that would prevent crossing a monitor seam.
        if (clampPosition)
        {
            window.Left = Math.Clamp(window.Left, monitor.Work.Left / scale, Math.Max(monitor.Work.Left / scale, monitor.Work.Right / scale - window.Width));
            window.Top = Math.Clamp(window.Top, monitor.Work.Top / scale, Math.Max(monitor.Work.Top / scale, monitor.Work.Bottom / scale - window.Height));
        }
    }
    private static MonitorInfo Info(IntPtr handle)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) throw new System.ComponentModel.Win32Exception();
        return info;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rectangle { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rectangle Monitor; public Rectangle Work; public int Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { public Point Reserved; public Point MaxSize; public Point MaxPosition; public Point MinTrackSize; public Point MaxTrackSize; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
}
