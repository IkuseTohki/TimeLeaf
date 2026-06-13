using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models;
using TimeLeaf.Repositories;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション設定のカテゴリ。
/// </summary>
public enum SettingsCategory
{
    Storage,
    Appearance,
    Calendar,
    Notifications,
    Behavior,
}

public partial class ApplicationSettingsViewModel : ObservableObject, IDialogViewModel
{
    private readonly IApplicationSettingsRepository _settingsRepository;
    private readonly ICalendarRepository _calendarRepository;
    private readonly IThemeService _themeService;
    private readonly WorkdayService _workdayService;
    private readonly ApplicationSettings _currentSettings;
    private CalendarSetting _calendarSetting = new();

    [ObservableProperty]
    private SettingsCategory _selectedCategory = SettingsCategory.Storage;

    [ObservableProperty]
    private string _storagePath = string.Empty;

    [ObservableProperty]
    private string _selectedTheme = "Forest";

    [ObservableProperty]
    private bool _enableOsNotification;

    [ObservableProperty]
    private bool _enableAppNotification;

    [ObservableProperty]
    private bool _minimizeOnClose;

    // Workdays
    [ObservableProperty]
    private bool _isMondayWorkday;

    [ObservableProperty]
    private bool _isTuesdayWorkday;

    [ObservableProperty]
    private bool _isWednesdayWorkday;

    [ObservableProperty]
    private bool _isThursdayWorkday;

    [ObservableProperty]
    private bool _isFridayWorkday;

    [ObservableProperty]
    private bool _isSaturdayWorkday;

    [ObservableProperty]
    private bool _isSundayWorkday;

    // Holidays
    public ObservableCollection<DateTime> Holidays { get; } = new();

    [ObservableProperty]
    private DateTime _newHolidayDate = DateTime.Today;

    /// <summary>
    /// ダイアログを閉じるよう要求するイベント。
    /// </summary>
    public event Action<bool>? RequestClose;

    public ApplicationSettingsViewModel(
        IApplicationSettingsRepository settingsRepository,
        ICalendarRepository calendarRepository,
        IThemeService themeService,
        WorkdayService workdayService,
        ApplicationSettings currentSettings
    )
    {
        _settingsRepository = settingsRepository;
        _calendarRepository = calendarRepository;
        _themeService = themeService;
        _workdayService = workdayService;
        _currentSettings = currentSettings;

        // 初期値ロード
        StoragePath = _currentSettings.StoragePath;
        SelectedTheme = _currentSettings.Theme;
        EnableOsNotification = _currentSettings.EnableOsNotification;
        EnableAppNotification = _currentSettings.EnableAppNotification;
        MinimizeOnClose = _currentSettings.MinimizeOnClose;

        _ = LoadCalendarSettingsAsync();
    }

    private async System.Threading.Tasks.Task LoadCalendarSettingsAsync()
    {
        _calendarSetting = await _calendarRepository.LoadAsync();

        IsMondayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Monday);
        IsTuesdayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Tuesday);
        IsWednesdayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Wednesday);
        IsThursdayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Thursday);
        IsFridayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Friday);
        IsSaturdayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Saturday);
        IsSundayWorkday = _calendarSetting.IsWorkdayOfWeek(DayOfWeek.Sunday);

        Holidays.Clear();
        foreach (var h in _calendarSetting.Holidays.OrderBy(d => d))
        {
            Holidays.Add(h);
        }
    }

    [RelayCommand]
    private void SelectCategory(SettingsCategory category)
    {
        SelectedCategory = category;
    }

    [RelayCommand]
    private void AddHoliday()
    {
        if (!Holidays.Contains(NewHolidayDate.Date))
        {
            var list = Holidays.ToList();
            list.Add(NewHolidayDate.Date);
            Holidays.Clear();
            foreach (var h in list.OrderBy(d => d))
            {
                Holidays.Add(h);
            }
        }
    }

    [RelayCommand]
    private void RemoveHoliday(DateTime date)
    {
        Holidays.Remove(date);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task Save()
    {
        // アプリケーション設定の更新
        _currentSettings.Theme = SelectedTheme;
        _currentSettings.EnableOsNotification = EnableOsNotification;
        _currentSettings.EnableAppNotification = EnableAppNotification;
        _currentSettings.MinimizeOnClose = MinimizeOnClose;
        _settingsRepository.Save(_currentSettings);

        // カレンダー設定の更新
        var workdays = new List<DayOfWeek>();
        if (IsMondayWorkday)
            workdays.Add(DayOfWeek.Monday);
        if (IsTuesdayWorkday)
            workdays.Add(DayOfWeek.Tuesday);
        if (IsWednesdayWorkday)
            workdays.Add(DayOfWeek.Wednesday);
        if (IsThursdayWorkday)
            workdays.Add(DayOfWeek.Thursday);
        if (IsFridayWorkday)
            workdays.Add(DayOfWeek.Friday);
        if (IsSaturdayWorkday)
            workdays.Add(DayOfWeek.Saturday);
        if (IsSundayWorkday)
            workdays.Add(DayOfWeek.Sunday);

        var newSetting = new CalendarSetting();
        newSetting.SetWorkdays(workdays);
        foreach (var h in Holidays)
        {
            newSetting.AddHoliday(h);
        }

        await _calendarRepository.SaveAsync(newSetting);

        // サービス側のキャッシュを更新
        _workdayService.UpdateSetting(newSetting);

        // テーマを即座に適用
        _themeService.ApplyTheme(SelectedTheme);

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void ResetStoragePath()
    {
        var result = MessageBox.Show(
            "保存場所を変更するにはアプリケーションの再起動が必要です。\n"
                + "設定をリセットして終了しますか？\n"
                + "次回起動時にセットアップ画面が表示されます。",
            "保存場所の変更",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            _currentSettings.StoragePath = string.Empty;
            _settingsRepository.Save(_currentSettings);

            // アプリケーション終了
            Application.Current.Shutdown();
        }
    }
}
