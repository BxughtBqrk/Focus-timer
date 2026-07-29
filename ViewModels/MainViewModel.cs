using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private int _sessionRemainingSeconds;
    private int _timeUntilBreakSeconds;
    private int _breakRemainingSeconds;

    [ObservableProperty]
    private string _timeDisplay = "25:00";

    [ObservableProperty]
    private int _customMinutes = 25;

    [ObservableProperty]
    private int _breakMinutes = 5;

    [ObservableProperty]
    private int _goalHours = 5;

    [ObservableProperty]
    private bool _isBreakMode = false;

    public string CurrentModeDisplay => IsBreakMode ? "BREAK" : "FOCUS";

    [ObservableProperty]
    private string _customMinutesString = "25";

    [ObservableProperty]
    private string _breakMinutesString = "5";

    [ObservableProperty]
    private string _goalHoursString = "5";

    [ObservableProperty]
    private bool _isGoalNotificationVisible = false;

    [ObservableProperty]
    private bool _goalReachedThisSession = false;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isSettingsVisible = false;

    [ObservableProperty]
    private bool _isStatsVisible = false;

    [ObservableProperty]
    private string _todayFocusDisplay = "0h 0m";

    [ObservableProperty]
    private string _todayBreakDisplay = "0h 0m";

    [ObservableProperty]
    private string _breakRatioDisplay = "0%";

    [ObservableProperty]
    private int _currentStreak = 0;

    [ObservableProperty]
    private List<Point> _focusChartPoints = new List<Point> { new Point(0, 80), new Point(240, 80) };

    [ObservableProperty]
    private List<Point> _breakChartPoints = new List<Point> { new Point(0, 80), new Point(240, 80) };

    [ObservableProperty]
    private string _chartMaxLabel = "60m";

    [ObservableProperty]
    private string _chartMidLabel = "30m";

    [ObservableProperty]
    private List<string> _dayLabels = new List<string> { "", "", "", "", "", "", "" };

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
        if (CustomMinutesString != value.ToString())
        {
            CustomMinutesString = value.ToString();
        }
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

    partial void OnGoalHoursChanged(int value)
    {
        if (GoalHoursString != value.ToString())
        {
            GoalHoursString = value.ToString();
        }
        GoalReachedThisSession = false;
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

    partial void OnGoalHoursStringChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            if (result >= 1 && result <= 24)
            {
                GoalHours = result;
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
        public int GoalHours { get; set; } = 5;
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
        public int TotalBreakMinutes { get; set; } = 0;
        public int SessionCount { get; set; } = 0;
    }

    public class HistoryData
    {
        public List<HistoryRecord> Records { get; set; } = new List<HistoryRecord>();
        public int CurrentStreak { get; set; } = 0;
        public string LastFocusedDate { get; set; } = "";
    }

    [JsonSerializable(typeof(AppSettings))]
    [JsonSerializable(typeof(HistoryData))]
    [JsonSerializable(typeof(HistoryRecord))]
    [JsonSerializable(typeof(List<HistoryRecord>))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }

    private HistoryData _history = new HistoryData();
    private bool _isLoadingSettings = false;

    private string GetConfigFilePath(string filename)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "FocusTimer");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return Path.Combine(folder, filename);
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var path = GetConfigFilePath("settings.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var s = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings);
                if (s != null)
                {
                    CustomMinutes = s.CustomMinutes;
                    if (s.BreakMinutes > 0) BreakMinutes = s.BreakMinutes;
                    if (s.GoalHours > 0) GoalHours = s.GoalHours;
                    BlockedAppsString = s.BlockedApps ?? "";
                    AllowedTabString = s.AllowedTab ?? "";
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
        finally { _isLoadingSettings = false; }
    }

    private void SaveSettings()
    {
        if (_isLoadingSettings) return;
        try
        {
            var s = new AppSettings
            {
                CustomMinutes = this.CustomMinutes,
                BreakMinutes = this.BreakMinutes,
                GoalHours = this.GoalHours,
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
            var path = GetConfigFilePath("settings.json");
            var json = JsonSerializer.Serialize(s, AppJsonContext.Default.AppSettings);
            File.WriteAllText(path, json);
        }
        catch (Exception)
        { 
        }
    }

    private void LoadHistory()
    {
        try
        {
            var path = GetConfigFilePath("history.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var h = JsonSerializer.Deserialize(json, AppJsonContext.Default.HistoryData);
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
            var path = GetConfigFilePath("history.json");
            File.WriteAllText(path, JsonSerializer.Serialize(_history, AppJsonContext.Default.HistoryData));
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
        int totalFocus = todayRecord != null ? todayRecord.TotalMinutes : 0;
        int totalBreak = todayRecord != null ? todayRecord.TotalBreakMinutes : 0;
        
        int fHours = totalFocus / 60;
        int fMins = totalFocus % 60;
        TodayFocusDisplay = fHours > 0 ? $"{fHours}h {fMins}m" : $"{fMins}m";

        int bHours = totalBreak / 60;
        int bMins = totalBreak % 60;
        TodayBreakDisplay = bHours > 0 ? $"{bHours}h {bMins}m" : $"{bMins}m";

        if (totalFocus + totalBreak > 0)
        {
            double ratio = (double)totalBreak / (totalFocus + totalBreak) * 100.0;
            BreakRatioDisplay = $"{Math.Round(ratio)}%";
        }
        else
        {
            BreakRatioDisplay = "0%";
        }
            
        CurrentStreak = _history.CurrentStreak;
        UpdateChartData();
    }

    private void UpdateChartData()
    {
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.Now.AddDays(-6 + i))
            .ToList();

        var focusData = new List<int>();
        var breakData = new List<int>();
        var newLabels = new List<string>();

        foreach (var day in last7Days)
        {
            var dayStr = day.ToString("yyyy-MM-dd");
            var record = _history.Records.FirstOrDefault(r => r.DateString == dayStr);
            focusData.Add(record?.TotalMinutes ?? 0);
            breakData.Add(record?.TotalBreakMinutes ?? 0);
            newLabels.Add(day.ToString("ddd")); // Mon, Tue, etc.
        }

        DayLabels = newLabels;

        double width = 240;
        double height = 80;

        int maxVal = Math.Max(1, Math.Max(focusData.Max(), breakData.Max()));
        maxVal = (int)Math.Ceiling(maxVal / 10.0) * 10; // Round up to nearest 10
        if (maxVal == 0) maxVal = 60;

        ChartMaxLabel = $"{maxVal}m";
        ChartMidLabel = $"{maxVal / 2}m";

        var focusPointsList = new List<Point>();
        var breakPointsList = new List<Point>();

        for (int i = 0; i < 7; i++)
        {
            double x = i * (width / 6);
            double focusY = height - ((double)focusData[i] / maxVal * height);
            double breakY = height - ((double)breakData[i] / maxVal * height);

            focusPointsList.Add(new Point(x, focusY));
            breakPointsList.Add(new Point(x, breakY));
        }

        FocusChartPoints = focusPointsList;
        BreakChartPoints = breakPointsList;
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
        
        if (record.TotalMinutes >= GoalHours * 60 && !GoalReachedThisSession)
        {
            GoalReachedThisSession = true;
            IsGoalNotificationVisible = true;
            if (IsSoundEnabled) MessageBeep(0x00);
        }
        
        SaveHistory();
        UpdateStatsDisplay();
    }

    private void RecordCompletedBreak(int minutes)
    {
        if (minutes <= 0) return;
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var record = _history.Records.FirstOrDefault(r => r.DateString == today);
        if (record == null)
        {
            record = new HistoryRecord { DateString = today };
            _history.Records.Add(record);
        }
        record.TotalBreakMinutes += minutes;
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

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;

        LoadSettings();
        LoadHistory();
        
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
        if (!IsBreakMode)
        {
            if (_sessionRemainingSeconds > 0)
            {
                _sessionRemainingSeconds--;
                _timeUntilBreakSeconds--;
                UpdateTimeDisplay();
                CheckAndKillBlockedApps();
                EnforceTabWhitelist();

                if (_timeUntilBreakSeconds <= 0)
                {
                    RecordCompletedSession(CustomMinutes);

                    if (_sessionRemainingSeconds > 0)
                    {
                        IsBreakMode = true;
                        _breakRemainingSeconds = BreakMinutes * 60;
                        if (IsSoundEnabled) MessageBeep(0x00);
                        UpdateTimeDisplay();
                        UpdateThemeBrush();
                        OnPropertyChanged(nameof(CurrentModeDisplay));

                        if (!IsAutoStartEnabled)
                        {
                            IsRunning = false;
                            _timer.Stop();
                            UpdateControlVisibility();
                        }
                    }
                    else
                    {
                        _timer.Stop();
                        IsRunning = false;
                        UpdateTimeDisplay();
                        UpdateControlVisibility();
                    }
                }
                else if (_sessionRemainingSeconds <= 0)
                {
                    int partialMinutes = (CustomMinutes * 60 - _timeUntilBreakSeconds) / 60;
                    if (partialMinutes > 0) RecordCompletedSession(partialMinutes);
                    
                    _timer.Stop();
                    IsRunning = false;
                    UpdateTimeDisplay();
                    UpdateControlVisibility();
                }
            }
        }
        else
        {
            if (_breakRemainingSeconds > 0)
            {
                _breakRemainingSeconds--;
                UpdateTimeDisplay();
            }
            else
            {
                RecordCompletedBreak(BreakMinutes);
                
                IsBreakMode = false;
                _timeUntilBreakSeconds = CustomMinutes * 60;
                if (IsSoundEnabled) MessageBeep(0x00);
                UpdateTimeDisplay();
                UpdateThemeBrush();
                OnPropertyChanged(nameof(CurrentModeDisplay));

                if (!IsAutoStartEnabled)
                {
                    IsRunning = false;
                    _timer.Stop();
                    UpdateControlVisibility();
                }
            }
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
        int displaySeconds = IsBreakMode ? _breakRemainingSeconds : _sessionRemainingSeconds;
        int hours = displaySeconds / 3600;
        int minutes = (displaySeconds % 3600) / 60;
        int seconds = displaySeconds % 60;
        
        if (hours > 0)
            TimeDisplay = $"{hours}:{minutes:D2}:{seconds:D2}";
        else
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
        if (_sessionRemainingSeconds <= 0)
        {
            ResetTimer();
            IsRunning = true;
            _timer.Start();
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
        IsBreakMode = false;
        
        _sessionRemainingSeconds = GoalHours * 3600;
        _timeUntilBreakSeconds = CustomMinutes * 60;
        _breakRemainingSeconds = BreakMinutes * 60;
        
        UpdateTimeDisplay();
        UpdateThemeBrush();
        OnPropertyChanged(nameof(CurrentModeDisplay));
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
    private void IncrementGoalHours()
    {
        if (GoalHours < 24) GoalHours++;
    }

    [RelayCommand]
    private void DecrementGoalHours()
    {
        if (GoalHours > 1) GoalHours--;
    }

    [RelayCommand]
    private void DismissGoalNotification()
    {
        IsGoalNotificationVisible = false;
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
