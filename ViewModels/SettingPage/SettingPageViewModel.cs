// Copyright (c) 2026 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using AI_Interviewer.Attributes;
using AI_Interviewer.Helpers;
using AI_Interviewer.Models;
using AI_Interviewer.Services.IServices;
using AI_Interviewer.Views.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

// ReSharper disable NotAccessedField.Local

namespace AI_Interviewer.ViewModels.SettingPage;

public partial class SettingPageViewModel : ObservableObject {
    // 选中的启动Page
    [ObservableProperty] private Type _startPageType = typeof(Views.Pages.HomePage.HomePage);

    // 启动Page列表
    public ObservableCollection<PageInfo> AvailblePages { get; }

    // 默认退出选项
    [ObservableProperty] private ExitMode _exitMode = ExitMode.Ask;

    // 星火AI配置
    [ObservableProperty] private bool _isEditingSparkConfig;

    // AppId
    [ObservableProperty] private string _appId = string.Empty;

    // ApiKey
    [ObservableProperty] private string _apiKey = string.Empty;

    // ApiSecret
    [ObservableProperty] private string _apiSecret = string.Empty;

    // 目标摄像头帧率
    [ObservableProperty] private double _cameraFps = 30;

    // 提示信息服务
    private readonly SnackbarServiceHelper _snackbarService;

    // Logger
    private readonly ILogger _logger;

    // 配置本地化服务
    private readonly IPreferencesService _preferencesService;

    public SettingPageViewModel(SnackbarServiceHelper snackbarService, ILogger logger,
        IPreferencesService preferencesService) {
        _snackbarService = snackbarService;
        _logger = logger;
        _preferencesService = preferencesService;
        AvailblePages = GetAvailblePages();
        Init();
    }

    private void Init() {
        StartPageType = _preferencesService.Get("StartPage", typeof(Views.Pages.HomePage.HomePage))!;
        ExitMode = _preferencesService.Get("ExitMode", ExitMode.Ask);
        AppId = _preferencesService.Get("SparkAI/AppId", string.Empty)!;
        ApiKey = _preferencesService.Get("SparkAI/ApiKey", string.Empty)!;
        ApiSecret = _preferencesService.Get("SparkAI/ApiSecret", string.Empty)!;
        CameraFps = _preferencesService.Get("CameraFps", 30);
    }

    public async Task Dispose() {
        await _preferencesService.Set("StartPage", StartPageType);
        await _preferencesService.Set("ExitMode", ExitMode);
        await _preferencesService.Set("SparkAI/AppId", AppId);
        await _preferencesService.Set("SparkAI/ApiKey", ApiKey);
        await _preferencesService.Set("SparkAI/ApiSecret", ApiSecret);
        await _preferencesService.Set("CameraFps", CameraFps);
    }

    partial void OnStartPageTypeChanged(Type value) {
        _preferencesService.Set("StartPage", value);
    }

    partial void OnExitModeChanged(ExitMode value) {
        _preferencesService.Set("ExitMode", value);
    }

    [RelayCommand]
    private void StartEdit() {
        IsEditingSparkConfig = true;
    }

    [RelayCommand]
    private void EndEdit() {
        IsEditingSparkConfig = false;
        _preferencesService.Set("SparkAI/AppId", AppId);
        _preferencesService.Set("SparkAI/ApiKey", ApiKey);
        _preferencesService.Set("SparkAI/ApiSecret", ApiSecret);
    }

    partial void OnAppIdChanged(string value) {
        _preferencesService.Set("SparkAI/AppId", value);
    }

    partial void OnApiKeyChanged(string value) {
        _preferencesService.Set("SparkAI/ApiKey", value);
    }

    partial void OnApiSecretChanged(string value) {
        _preferencesService.Set("SparkAI/ApiSecret", value);
    }

    partial void OnCameraFpsChanged(double value) {
        _preferencesService.Set("CameraFps", value);
    }

    private ObservableCollection<PageInfo> GetAvailblePages() {
        var availblePages = new ObservableCollection<PageInfo>();

        var assembly = Assembly.GetExecutingAssembly();

        var pageTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Page)))
            .Where(t => t.GetCustomAttribute<AvailbleStartPageAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<AvailbleStartPageAttribute>()!.SortWeight)
            .ThenBy(t => t.Name);

        foreach (var pageType in pageTypes) {
            var displayName = GetDisplayNameByDescriptionAttribute(pageType);
            availblePages.Add(new PageInfo {
                DisplayName = displayName,
                PageType = pageType
            });
        }

        return availblePages;
    }

    private string GetDisplayNameByDescriptionAttribute(Type pageType) {
        var descriptionAttribute = pageType.GetCustomAttribute<DescriptionAttribute>();
        return descriptionAttribute?.Description ?? pageType.Name;
    }

    [RelayCommand]
    private void OpenAboutWindow() {
        var aboutWindow = App.GetService<AboutWindow>()!;
        aboutWindow.Owner = App.Current.MainWindow;
        aboutWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenLogFolder() {
        Process.Start(new ProcessStartInfo {
            FileName = GlobalSettings.LogDirectory,
            UseShellExecute = true
        });
    }
}