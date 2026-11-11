using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace AuraIsland;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _metricsTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _expanded;
    private bool _lightTheme;
    private ulong _lastIdle, _lastKernel, _lastUser;

    public MainWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => UpdateClock();
        _metricsTimer.Tick += (_, _) => UpdateMetrics();
        Loaded += (_, _) =>
        {
            GetSystemTimes(out _lastIdle, out _lastKernel, out _lastUser);
            UpdateClock();
            UpdateMetrics();
            _timer.Start();
            _metricsTimer.Start();
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Left = (SystemParameters.WorkArea.Width - Width) / 2;
        Top = 12;
    }

    private void UpdateClock()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm");
        DateText.Text = DateTime.Now.ToString("dddd, d MMMM");
    }

    private void UpdateMetrics()
    {
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            var idleDelta = idle - _lastIdle;
            var kernelDelta = kernel - _lastKernel;
            var userDelta = user - _lastUser;
            var total = kernelDelta + userDelta;
            var cpu = total == 0 ? 0 : Math.Clamp((double)(total - idleDelta) * 100 / total, 0, 100);
            _lastIdle = idle; _lastKernel = kernel; _lastUser = user;
            CpuBar.Value = cpu;
            CpuText.Text = $"{cpu:0}%";
        }

        var memory = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref memory))
        {
            var used = memory.TotalPhysical == 0 ? 0 :
                (double)(memory.TotalPhysical - memory.AvailablePhysical) * 100 / memory.TotalPhysical;
            RamBar.Value = used;
            RamText.Text = $"{used:0}%";
        }
    }

    private void Island_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleExpanded();
        else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton == MouseButton.Right) ToggleExpanded();
    }

    private void ToggleExpanded()
    {
        _expanded = !_expanded;
        var target = _expanded ? 260d : 76d;
        ExpandedPanel.Visibility = _expanded ? Visibility.Visible : Visibility.Collapsed;
        ExpandGlyph.Text = _expanded ? "⌃" : "⌄";
        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(HeightProperty, animation);
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _lightTheme = !_lightTheme;
        IslandBorder.Background = _lightTheme
            ? new SolidColorBrush(Color.FromArgb(245, 241, 244, 249))
            : new SolidColorBrush(Color.FromArgb(245, 20, 23, 29));
        IslandBorder.BorderBrush = _lightTheme
            ? new SolidColorBrush(Color.FromRgb(205, 212, 222))
            : new SolidColorBrush(Color.FromRgb(48, 58, 77));
        SetTextTheme(_lightTheme ? Brushes.Black : Brushes.White,
                     _lightTheme ? new SolidColorBrush(Color.FromRgb(75, 85, 99)) : new SolidColorBrush(Color.FromRgb(170, 178, 192)));
        ThemeButton.Content = _lightTheme ? "Switch to dark" : "Switch to light";
    }

    private void SetTextTheme(Brush primary, Brush secondary)
    {
        ClockText.Foreground = primary;
        DateText.Foreground = secondary;
        CpuText.Foreground = primary;
        RamText.Foreground = primary;
        foreach (var child in ExpandedPanel.Children)
            if (child is TextBlock block && block.Name != "none") block.Foreground = secondary;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length, MemoryLoad;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile,
                     TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
