using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Forms;
using DrawingIcon = System.Drawing.SystemIcons;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using AuraIsland.Services;

namespace AuraIsland;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _metricsTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly NotifyIcon _trayIcon = new() { Icon = DrawingIcon.Application, Visible = true, Text = "Aura Island" };
    private bool _expanded;
    private ulong _lastIdle, _lastKernel, _lastUser;

    public MainWindow()
    {
        InitializeComponent();
        CityBox.Text = _settings.City;
        Closed += (_, _) => { _trayIcon.Visible = false; _trayIcon.Dispose(); };
        Loaded += (_, _) =>
        {
            GetSystemTimes(out _lastIdle, out _lastKernel, out _lastUser);
            UpdateClock();
            UpdateMetrics();
            ApplyTheme();
            UpdateStartupLabel();
            _timer.Tick += (_, _) => UpdateClock();
            _metricsTimer.Tick += (_, _) => UpdateMetrics();
            _timer.Start(); _metricsTimer.Start();
            _ = RefreshWifiAsync();
            _ = RefreshBluetoothAsync();
            _ = RefreshWeatherAsync();
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
            var total = (kernel - _lastKernel) + (user - _lastUser);
            var cpu = total == 0 ? 0 : Math.Clamp((double)(total - idleDelta) * 100 / total, 0, 100);
            _lastIdle = idle; _lastKernel = kernel; _lastUser = user;
            CpuBar.Value = cpu; CpuText.Text = $"{cpu:0}%";
        }
        var memory = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref memory))
        {
            var used = memory.TotalPhysical == 0 ? 0 : (double)(memory.TotalPhysical - memory.AvailablePhysical) * 100 / memory.TotalPhysical;
            RamBar.Value = used; RamText.Text = $"{used:0}%";
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
        var target = _expanded ? 550d : 82d;
        ExpandedPanel.Visibility = _expanded ? Visibility.Visible : Visibility.Collapsed;
        ExpandGlyph.Text = _expanded ? "⌃" : "⌄";
        BeginAnimation(HeightProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(230))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private void Notify(string title, string message)
    {
        try { _trayIcon.BalloonTipTitle = title; _trayIcon.BalloonTipText = message; _trayIcon.ShowBalloonTip(2500); } catch { }
    }

    private void Previous_Click(object s, RoutedEventArgs e) { MediaService.Previous(); SetStatus("Предыдущий трек"); }
    private void PlayPause_Click(object s, RoutedEventArgs e) { MediaService.PlayPause(); SetStatus("Команда воспроизведения/паузы отправлена активному плееру"); }
    private void Next_Click(object s, RoutedEventArgs e) { MediaService.Next(); SetStatus("Следующий трек"); }
    private void VolumeDown_Click(object s, RoutedEventArgs e) { MediaService.VolumeDown(); SetStatus("Громкость уменьшена"); }
    private void VolumeUp_Click(object s, RoutedEventArgs e) { MediaService.VolumeUp(); SetStatus("Громкость увеличена"); }

    private async void Wifi_Click(object sender, RoutedEventArgs e)
    {
        var state = await NetworkService.GetWifiStateAsync();
        var currentlyEnabled = state.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
        var result = await NetworkService.SetWifiAsync(!currentlyEnabled);
        SetStatus($"Wi-Fi: {result}");
        Notify("Aura Island", "Состояние Wi-Fi изменено.");
        await RefreshWifiAsync();
    }

    private async Task RefreshWifiAsync()
    {
        var state = await NetworkService.GetWifiStateAsync();
        var enabled = state.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
        WifiButton.Content = state.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
            ? "Wi-Fi адаптер не найден"
            : enabled ? "Wi-Fi: включён (выкл.)" : "Wi-Fi: выключен (вкл.)";
    }

    private async void Bluetooth_Click(object sender, RoutedEventArgs e)
    {
        var state = await NetworkService.GetBluetoothStateAsync();
        var currentlyEnabled = state.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
        var result = await NetworkService.SetBluetoothAsync(!currentlyEnabled);
        SetStatus($"Bluetooth: {result}");
        Notify("Aura Island", "Команда изменения Bluetooth отправлена Windows.");
        await RefreshBluetoothAsync();
    }

    private async Task RefreshBluetoothAsync()
    {
        var state = await NetworkService.GetBluetoothStateAsync();
        var enabled = state.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
        BluetoothButton.Content = enabled ? "Bluetooth: включён (выкл.)" : "Bluetooth: выключен (вкл.)";
    }

    private async void Weather_Click(object sender, RoutedEventArgs e)
    {
        _settings.City = CityBox.Text.Trim(); _settings.Save();
        await RefreshWeatherAsync();
    }

    private async Task RefreshWeatherAsync()
    {
        try
        {
            WeatherText.Text = "Загружаю погоду…";
            var info = await WeatherService.GetAsync(_settings.City);
            WeatherText.Text = info is null ? "Город не найден" : $"{info.City}: {info.Temperature:0}°C • {info.Description}";
        }
        catch (Exception ex) { WeatherText.Text = "Погода недоступна: " + ex.Message; }
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.LightTheme = !_settings.LightTheme;
        _settings.Save(); ApplyTheme();
    }

    private void ApplyTheme()
    {
        var light = _settings.LightTheme;
        IslandBorder.Background = new SolidColorBrush(light ? MediaColor.FromArgb(248, 241, 244, 249) : MediaColor.FromArgb(245, 20, 23, 29));
        IslandBorder.BorderBrush = new SolidColorBrush(light ? MediaColor.FromRgb(205, 212, 222) : MediaColor.FromRgb(48, 58, 77));
        var primary = light ? MediaBrushes.Black : MediaBrushes.White;
        var secondary = light ? new SolidColorBrush(MediaColor.FromRgb(75, 85, 99)) : new SolidColorBrush(MediaColor.FromRgb(170, 178, 192));
        ClockText.Foreground = primary; DateText.Foreground = secondary;
        CpuText.Foreground = primary; RamText.Foreground = primary; WeatherText.Foreground = primary;
        ThemeButton.Content = light ? "Светлая тема ✓" : "Тёмная тема ✓";
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        _settings.StartWithWindows = !_settings.StartWithWindows;
        try { _settings.Save(); UpdateStartupLabel(); SetStatus("Настройка автозапуска сохранена."); }
        catch (Exception ex) { SetStatus("Не удалось изменить автозапуск: " + ex.Message); }
    }

    private void UpdateStartupLabel() => StartupButton.Content = _settings.StartWithWindows ? "Автозапуск: вкл." : "Автозапуск: выкл.";

    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length, MemoryLoad;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError=true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
