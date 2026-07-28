using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FocusTimer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private DispatcherTimer _timer;
    private int _remainingSeconds;

    [ObservableProperty]
    private string _timeDisplay = "25:00";

    [ObservableProperty]
    private int _customMinutes = 25;

    [ObservableProperty]
    private int _breakMinutes = 5;

    [ObservableProperty]
    private bool _isBreakMode = false;

    public string CurrentModeDisplay => IsBreakMode ? "BREAK" : "FOCUS";

    [ObservableProperty]
    private string _customMinutesString = "25";

    [ObservableProperty]
    private string _breakMinutesString = "5";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isSettingsVisible = false;

    [ObservableProperty]
    private bool _isStatsVisible = false;

    [ObservableProperty]
    private string _todayFocusDisplay = "0h 0m";

    [ObservableProperty]
    private int _currentStreak = 0;

    [ObservableProperty]
    private string _sessionGoal = "";

    [ObservableProperty]
    private bool _isMiniMode = false;

    [ObservableProperty]
    private double _windowWidth = 350;

    [ObservableProperty]
    private double _windowHeight = 500;

    [ObservableProperty]
    private IBrush _themeGradientBrush = new SolidColorBrush(Colors.Cyan);

    [ObservableProperty]
    private bool _isAlwaysOnTop = false;

    [ObservableProperty]
    private string _blockedAppsString = "";

    [ObservableProperty]
    private string _allowedTabString = "";

    [ObservableProperty]
    private bool _isIronWillEnabled = false;

    [ObservableProperty]
    private bool _isGhostModeEnabled = false;

    [ObservableProperty]
    private bool _isNotGhostMode = true;

    [ObservableProperty]
    private bool _showControls = true;

    [ObservableProperty]
    private bool _isAutoStartEnabled = false;

    [ObservableProperty]
    private bool _isSoundEnabled = true;

    public bool ShowMainMode => !IsMiniMode;
    public bool IsIntentLocked => IsRunning;

    [ObservableProperty]
    private double _backgroundOpacity = 0.88;

    [ObservableProperty]
    private double _ghostOpacity = 1.0;

    public bool ShowPauseButton => IsRunning && !IsIronWillEnabled;
    
    public double CurrentTimerOpacity 
    {
        get 
        {
            if (IsSettingsVisible) return 0.0;
            return IsGhostModeEnabled ? GhostOpacity : 1.0;
        }
    }

    public double CurrentBackgroundOpacity => IsGhostModeEnabled ? 0.0 : BackgroundOpacity;

    public bool ShowSettingsButton => ShowControls && !IsSettingsVisible && !IsStatsVisible;
    
    public bool ShowStatsButton => ShowControls && !IsSettingsVisible && !IsStatsVisible;

    partial void OnIsSettingsVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSettingsButton));
        OnPropertyChanged(nameof(ShowStatsButton));
        OnPropertyChanged(nameof(CurrentTimerOpacity));
    }

    partial void OnIsStatsVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSettingsButton));
        OnPropertyChanged(nameof(ShowStatsButton));
        OnPropertyChanged(nameof(CurrentTimerOpacity));
    }

    partial void OnIsIronWillEnabledChanged(bool value)
    {
        UpdateControlVisibility();
        SaveSettings();
    }

    partial void OnIsGhostModeEnabledChanged(bool value)
    {
        IsNotGhostMode = !value;
        OnPropertyChanged(nameof(CurrentTimerOpacity));
        OnPropertyChanged(nameof(CurrentBackgroundOpacity));
        SaveSettings();
    }

    partial void OnIsAutoStartEnabledChanged(bool value) => SaveSettings();
    partial void OnIsSoundEnabledChanged(bool value) => SaveSettings();

    partial void OnCustomMinutesChanged(int value)
    {
        if (!IsBreakMode) ResetTimer();
        SaveSettings();
    }

    partial void OnBreakMinutesChanged(int value)
    {
        if (BreakMinutesString != value.ToString())
        {
            BreakMinutesString = value.ToString();
        }
        if (IsBreakMode) ResetTimer();
        SaveSettings();
    }

    partial void OnCustomMinutesStringChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result >= 1 && result <= 1440)
            {
                CustomMinutes = result;
            }
        }
    }

    partial void OnBreakMinutesStringChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result >= 1 && result <= 1440)
            {
                BreakMinutes = result;
            }
        }
    }
    
    partial void OnBlockedAppsStringChanged(string value) => SaveSettings();
    partial void OnAllowedTabStringChanged(string value) => SaveSettings();
    partial void OnIsAlwaysOnTopChanged(bool value) => SaveSettings();
    partial void OnBackgroundOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(CurrentBackgroundOpacity));
        SaveSettings();
    }
    partial void OnGhostOpacityChanged(double value) 
    {
        OnPropertyChanged(nameof(CurrentTimerOpacity));
        SaveSettings();
    }

    public class AppSettings
    {
        public int CustomMinutes { get; set; } = 25;
        public int BreakMinutes { get; set; } = 5;
        public string BlockedApps { get; set; } = "";
        public string AllowedTab { get; set; } = "";
        public bool IsIronWill { get; set; } = false;
        public bool IsGhostMode { get; set; } = false;
        public bool IsAlwaysOnTop { get; set; } = false;
        public bool IsAutoStart { get; set; } = false;
        public bool IsSoundEnabled { get; set; } = true;
        public double BackgroundOpacity { get; set; } = 0.88;
        public double GhostOpacity { get; set; } = 1.0;
    }

    public class HistoryRecord
    {
        public string DateString { get; set; } = "";
        public int TotalMinutes { get; set; } = 0;
        public int SessionCount { get; set; } = 0;
    }

    public class HistoryData
    {
        public List<HistoryRecord> Records { get; set; } = new List<HistoryRecord>();
        public int CurrentStreak { get; set; } = 0;
        public string LastFocusedDate { get; set; } = "";
    }

    private HistoryData _history = new HistoryData();

    private void LoadSettings()
    {
        try
        {
            if (File.Exists("settings.json"))
            {
                var json = File.ReadAllText("settings.json");
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null)
                {
                    CustomMinutes = s.CustomMinutes;
                    if (s.BreakMinutes > 0) BreakMinutes = s.BreakMinutes;
                    BlockedAppsString = s.BlockedApps;
                    AllowedTabString = s.AllowedTab;
                    IsIronWillEnabled = s.IsIronWill;
                    IsGhostModeEnabled = s.IsGhostMode;
                    IsAlwaysOnTop = s.IsAlwaysOnTop;
                    IsAutoStartEnabled = s.IsAutoStart;
                    IsSoundEnabled = s.IsSoundEnabled;
                    BackgroundOpacity = s.BackgroundOpacity;
                    GhostOpacity = s.GhostOpacity;
                }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var s = new AppSettings
            {
                CustomMinutes = this.CustomMinutes,
                BreakMinutes = this.BreakMinutes,
                BlockedApps = this.BlockedAppsString,
                AllowedTab = this.AllowedTabString,
                IsIronWill = this.IsIronWillEnabled,
                IsGhostMode = this.IsGhostModeEnabled,
                IsAlwaysOnTop = this.IsAlwaysOnTop,
                IsAutoStart = this.IsAutoStartEnabled,
                IsSoundEnabled = this.IsSoundEnabled,
                BackgroundOpacity = this.BackgroundOpacity,
                GhostOpacity = this.GhostOpacity
            };
            File.WriteAllText("settings.json", JsonSerializer.Serialize(s));
        }
        catch { }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists("history.json"))
            {
                var json = File.ReadAllText("history.json");
                var h = JsonSerializer.Deserialize<HistoryData>(json);
                if (h != null)
                {
                    _history = h;
                }
            }
        }
        catch { }
        UpdateStreakLogic();
        UpdateStatsDisplay();
    }

    private void SaveHistory()
    {
        try
        {
            File.WriteAllText("history.json", JsonSerializer.Serialize(_history));
        }
        catch { }
    }

    private void UpdateStreakLogic()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        
        if (_history.LastFocusedDate != today && _history.LastFocusedDate != yesterday && !string.IsNullOrEmpty(_history.LastFocusedDate))
        {
            _history.CurrentStreak = 0;
        }
        CurrentStreak = _history.CurrentStreak;
    }

    private void UpdateStatsDisplay()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var todayRecord = _history.Records.FirstOrDefault(r => r.DateString == today);
        int totalMinutes = todayRecord != null ? todayRecord.TotalMinutes : 0;
        
        int hours = totalMinutes / 60;
        int mins = totalMinutes % 60;
        
        if (hours > 0)
            TodayFocusDisplay = $"{hours}h {mins}m";
        else
            TodayFocusDisplay = $"{mins}m";
            
        CurrentStreak = _history.CurrentStreak;
    }

    private void RecordCompletedSession(int minutes)
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        var record = _history.Records.FirstOrDefault(r => r.DateString == today);
        if (record == null)
        {
            record = new HistoryRecord { DateString = today };
            _history.Records.Add(record);
            
            if (_history.LastFocusedDate == yesterday || string.IsNullOrEmpty(_history.LastFocusedDate))
            {
                _history.CurrentStreak++;
            }
            else if (_history.LastFocusedDate != today)
            {
                _history.CurrentStreak = 1;
            }
        }
        
        record.TotalMinutes += minutes;
        record.SessionCount++;
        _history.LastFocusedDate = today;
        
        SaveHistory();
        UpdateStatsDisplay();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private const int SW_MINIMIZE = 6;

    public MainViewModel()
    {
        UpdateThemeBrush();

        LoadSettings();
        LoadHistory();
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        ResetTimer();
    }

    private void UpdateThemeBrush()
    {
        var colorStart = IsBreakMode ? "#00FF7F" : "#00E5FF";
        var colorEnd = IsBreakMode ? "#008080" : "#9D00FF";
        var stops = new GradientStops
        {
            new GradientStop(Color.Parse(colorStart), 0),
            new GradientStop(Color.Parse(colorEnd), 1)
        };
        ThemeGradientBrush = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative), GradientStops = stops };
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_remainingSeconds > 0)
        {
            _remainingSeconds--;
            UpdateTimeDisplay();
            CheckAndKillBlockedApps();
            EnforceTabWhitelist();
        }
        else
        {
            StartTimer();
        }
    }

    private void CheckAndKillBlockedApps()
    {
        if (string.IsNullOrWhiteSpace(BlockedAppsString)) return;

        var blockedNames = BlockedAppsString
            .Split(',')
            .Select(s => s.Trim().ToLower())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (blockedNames.Count == 0) return;

        var runningProcesses = Process.GetProcesses();
        foreach (var process in runningProcesses)
        {
            try
            {
                var processName = process.ProcessName.ToLower();
                if (blockedNames.Contains(processName))
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }
    }

    private void EnforceTabWhitelist()
    {
        if (string.IsNullOrWhiteSpace(AllowedTabString)) return;

        var allowedKeywords = AllowedTabString
            .Split(',')
            .Select(s => s.Trim().ToLower())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (allowedKeywords.Count == 0) return;

        IntPtr handle = GetForegroundWindow();
        if (handle == IntPtr.Zero) return;

        StringBuilder sb = new StringBuilder(256);
        if (GetWindowText(handle, sb, 256) > 0)
        {
            string title = sb.ToString().ToLower();

            if (title.Contains("google chrome"))
            {
                bool isAllowed = allowedKeywords.Any(k => title.Contains(k)) || title.Contains("new tab");
                
                if (!isAllowed)
                {
                    ShowWindow(handle, SW_MINIMIZE);
                }
            }
        }
    }

    private void UpdateTimeDisplay()
    {
        int minutes = _remainingSeconds / 60;
        int seconds = _remainingSeconds % 60;
        TimeDisplay = $"{minutes:D2}:{seconds:D2}";
    }

    private void UpdateControlVisibility()
    {
        ShowControls = !(IsRunning && IsIronWillEnabled);
        OnPropertyChanged(nameof(ShowPauseButton));
        OnPropertyChanged(nameof(ShowSettingsButton));
        OnPropertyChanged(nameof(ShowStatsButton));
        OnPropertyChanged(nameof(IsIntentLocked));
    }

    [RelayCommand]
    private void StartTimer()
    {
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            IsRunning = false;
            
            if (!IsBreakMode)
            {
                RecordCompletedSession(CustomMinutes);
                
                IsBreakMode = true;
                _remainingSeconds = BreakMinutes * 60;
                if (IsSoundEnabled) MessageBeep(0x00); // Standard ding
            }
            else
            {
                IsBreakMode = false;
                _remainingSeconds = CustomMinutes * 60;
                if (IsSoundEnabled) MessageBeep(0x00); // Standard ding
            }
            
            UpdateTimeDisplay();
            UpdateThemeBrush();
            OnPropertyChanged(nameof(CurrentModeDisplay));
            
            if (IsAutoStartEnabled)
            {
                IsRunning = true;
                _timer.Start();
            }
            
            UpdateControlVisibility();
        }
        else if (!_timer.IsEnabled)
        {
            IsRunning = true;
            UpdateControlVisibility();
            _timer.Start();
        }
    }

    [RelayCommand]
    private void StopTimer()
    {
        _timer.Stop();
        IsRunning = false;
        UpdateControlVisibility();
    }

    [RelayCommand]
    private void ResetTimer()
    {
        _timer.Stop();
        IsRunning = false;
        _remainingSeconds = (IsBreakMode ? BreakMinutes : CustomMinutes) * 60;
        UpdateTimeDisplay();
        UpdateControlVisibility();
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsVisible = !IsSettingsVisible;
        if (IsSettingsVisible) IsStatsVisible = false;
        
        if (!IsSettingsVisible)
        {
            ResetTimer();
        }
    }

    [RelayCommand]
    private void ToggleStats()
    {
        IsStatsVisible = !IsStatsVisible;
        if (IsStatsVisible) IsSettingsVisible = false;
    }

    [RelayCommand]
    private void ToggleGhostMode()
    {
        IsGhostModeEnabled = !IsGhostModeEnabled;
    }

    [RelayCommand]
    private void IncrementMinutes()
    {
        if (CustomMinutes < 1440) CustomMinutes++;
    }

    [RelayCommand]
    private void DecrementMinutes()
    {
        if (CustomMinutes > 1) CustomMinutes--;
    }

    [RelayCommand]
    private void IncrementBreakMinutes()
    {
        if (BreakMinutes < 60) BreakMinutes++;
    }

    [RelayCommand]
    private void DecrementBreakMinutes()
    {
        if (BreakMinutes > 1) BreakMinutes--;
    }

    [RelayCommand]
    private void ToggleMiniMode()
    {
        IsMiniMode = !IsMiniMode;
        if (IsMiniMode)
        {
            WindowWidth = 180;
            WindowHeight = 80;
        }
        else
        {
            WindowWidth = 350;
            WindowHeight = 500;
        }
        OnPropertyChanged(nameof(ShowMainMode));
    }
}
